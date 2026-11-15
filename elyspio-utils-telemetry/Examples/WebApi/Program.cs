using Elyspio.Utils.Results;
using Elyspio.Utils.Results.AspNetCore;
using Elyspio.Utils.Telemetry;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddElyspioTelemetry(); // gtnh-app would add: o => o.Sources.Add("Yarp.ReverseProxy")

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<BridgeAdapter>();
builder.Services.AddSingleton<OresService>();
builder.Services.AddHealthChecks().AddCheck<BridgeHealthCheck>("bridge");

var app = builder.Build();

app.UseExceptionHandler(); // unexpected exceptions -> 500 ProblemDetails, marked on the server span

// Endpoints: pass-through, the HTTP mapping comes from the error category.
app.MapGet("/api/ores/stock/{grid}", async (string grid, OresService ores, CancellationToken ct) => (await ores.GetStock(grid, ct)).ToHttpResult());
app.MapPut("/api/ores/assignments/{resourceId}", (string resourceId, OresService ores) => ores.Assign(resourceId).ToHttpResult());
app.MapPost("/api/ores/refresh/{grid}", (string grid, OresService ores) =>
	ores.Refresh(new RefreshRequest(grid, "s3cr3t-token", new byte[1_000_000], "Fer étiré")).ToHttpResult(run => TypedResults.Accepted($"/api/ores/refresh/{run.Id}", run)));
app.MapHealthChecks("/health"); // never traced, nor anything it calls (lib default)

app.Run();

// ---- Errors of the app: specialisations of the library categories ----

public sealed record InvalidResourceId(string ResourceId) : Invalid("ores.invalid-resource-id", "Identifiant de ressource invalide.")
{
	public override IEnumerable<KeyValuePair<string, object?>> Extensions => [new("resourceId", ResourceId)];
}

public sealed record ModUnreachable(string Grid) : Unavailable("ores.mod-unreachable", "Le serveur Minecraft ne répond pas.");

public sealed record Stock(string Grid, IReadOnlyDictionary<string, long> Amounts);

public sealed record RefreshRun(Guid Id, string Grid);

/// <summary>Log.F: the token is masked, the payload summarized ("(1000000 bytes)"), the API JSON is unaffected.</summary>
public sealed record RefreshRequest(string Grid, [property: LogMask] string Token, byte[] Payload, string Label);

// ---- Services ----

public sealed partial class OresService(BridgeAdapter bridge, ILogger<OresService> logger)
{
	public async Task<Result<Stock>> GetStock(string grid, CancellationToken cancellationToken)
	{
		using var trace = logger.Trace($"{Log.F(grid)}");
		trace.Activity?.SetTag("ores.grid", grid);

		var read = await bridge.ReadMetrics(grid, cancellationToken);

		// Relay the callee's failure, marking this span too.
		if (!read.TryGet(out var amounts, out var error)) return trace.Fail(error);

		if (amounts.Count == 0) trace.LogWarning($"grid {grid} is empty");
		LogTotal(trace, amounts.Values.Sum());

		trace.Exit($"{amounts.Count} entries");
		return new Stock(grid, amounts);
	}

	public Result<Unit> Assign(string resourceId)
	{
		using var trace = logger.Trace($"{Log.F(resourceId)}");

		if (!resourceId.All(char.IsAsciiLetterOrDigit)) return trace.Fail(new InvalidResourceId(resourceId));

		return Unit.Value;
	}

	public Result<RefreshRun> Refresh(RefreshRequest request)
	{
		using var trace = logger.Trace($"{Log.F(request)}");

		return new RefreshRun(Guid.NewGuid(), request.Grid);
	}

	/// <summary>Exhaustive switch on the two cases, then on the categories (no default arm).</summary>
	public static string Describe(Result<Stock> stock) => stock switch
	{
		Ok<Stock> ok => $"{ok.Value.Amounts.Count} entries",
		Err<Error> { Error: Unavailable } => "mod down",
		Err<Error> err => err.Error.Code,
	};

	[LoggerMessage(LogLevel.Information, "Total amount {Total}")]
	private static partial void LogTotal(ILogger logger, long total);
}

public sealed class BridgeAdapter(ILogger<BridgeAdapter> logger)
{
	public async Task<Result<Dictionary<string, long>>> ReadMetrics(string grid, CancellationToken cancellationToken)
	{
		using var trace = logger.Trace($"{Log.F(grid)}");

		await Task.Delay(30, cancellationToken); // stands for the HTTP call to the mod (HttpClient span would nest here)

		if (grid == "boom") return trace.Fail(new ModUnreachable(grid));
		if (grid == "crash") throw new InvalidOperationException("bug"); // the unexpected: an exception, not a Result

		return grid == "empty" ? [] : new Dictionary<string, long> { ["minecraft:iron_ingot"] = 1234, ["gregtech:copper"] = 56 };
	}
}

/// <summary>Readiness check calling the bridge: its spans must not appear (instrumentation suppressed for /health).</summary>
public sealed class BridgeHealthCheck(BridgeAdapter bridge) : IHealthCheck
{
	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
	{
		var read = await bridge.ReadMetrics("main", cancellationToken);
		return read.IsSuccess ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
	}
}
