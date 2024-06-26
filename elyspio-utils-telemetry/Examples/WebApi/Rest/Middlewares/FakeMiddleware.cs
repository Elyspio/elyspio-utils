using Elyspio.Utils.Telemetry.Tracing.Elements;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Rest.Middlewares;

/// <summary>
///     Middleware d'exemple
/// </summary>
public class FakeMiddleware : TracingMiddleware, IMiddleware
{
	private static int I;

    /// <summary>
    ///     Constructeur du middleware
    /// </summary>
    public FakeMiddleware(ILogger<FakeMiddleware> logger) : base(logger)
	{
	}


	/// <inheritdoc />
	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		using var logger = LogMiddleware($"{context.Request.Method} {context.Request.Path.ToString()}");

		logger.Debug("Do something");

		if (I % 5 == 0) logger.Info("This is informative");
		if (I % 10 == 0) logger.Warn("This is problematic");
		if (I % 50 == 0) logger.Error("Oops without exception");
		if (I % 50 == 1) logger.Error(new Exception("An error occurred"), "Oops with exception");

		Interlocked.Add(ref I, 1);

		await next.Invoke(context);
	}
}