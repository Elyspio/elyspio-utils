# Elyspio.Utils.Results

A `Result` type for expected failures, built on C# 15 unions (.NET 11). Exceptions stay for the unexpected.

```bash
dotnet add package Elyspio.Utils.Results
dotnet add package Elyspio.Utils.Results.AspNetCore   # ToHttpResult / ProblemDetails
```

## Result

```csharp
public Result<Stock> GetStock(string grid)
{
	if (!IsValid(grid)) return new InvalidGrid(grid); // error => failure
	return new Stock(grid, amounts);                  // value => success
}
```

- `Result<T, TError>` holds either `Ok<T>` or `Err<TError>`. Values are stored typed, with no boxing. A `switch` over the two cases is exhaustive:

```csharp
var text = result switch
{
	Ok<Stock> ok => $"{ok.Value.Amounts.Count} entries",
	Err<Error> { Error: Unavailable } => "mod down",
	Err<Error> err => err.Error.Code,
};
```

- `Result<T>` is `Result<T, Error>`, the common case.
- `TryGet(out value, out error)` tests and unpacks in one step: `if (!read.TryGet(out var amounts, out var error)) return trace.Fail(error);`
- `Map` and `Bind` are synchronous. `Unit.Value` is the success of an operation that returns nothing (`Result<Unit>`).
- `default(Result<T>)` is neither a success nor a failure: `TryGet`, `Map` and `Bind` throw on it.
- `Failed` / `Failed<TError>`, returned by `trace.Fail(...)` in Elyspio.Utils.Telemetry, convert to any `Result` without naming `T`.
- Name clash: `Ok<T>` also exists in `Microsoft.AspNetCore.Http.HttpResults`. Do not import both namespaces in the same file.

## Errors

`Error(Code, Message)` is a `closed` record. Only the categories below derive from it directly, so a switch over the categories needs no default arm. The categories themselves are open, so an app derives its own errors from them:

| Category | HTTP |
| --- | --- |
| `Invalid` (+ `Fields` for per-field messages) | 400 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `TooManyRequests` | 429 |
| `Unexpected` (message never shown to the caller) | 500 |
| `BadGateway` | 502 |
| `Unavailable` | 503 |
| `Timeout` | 504 |
| `Failure(Status, ...)` | any |

```csharp
public sealed record InvalidResourceId(string ResourceId) : Invalid("ores.invalid-resource-id", "Identifiant de ressource invalide.")
{
	public override IEnumerable<KeyValuePair<string, object?>> Extensions => [new("resourceId", ResourceId)];
}
```

`Code` is stable: it becomes the ProblemDetails `type` (`urn:problem:{Code}`) and the span's `error.type`. `Extensions` are published to the caller only when the error overrides them (opt-in).

Add `<WarningsAsErrors>$(WarningsAsErrors);CS8509</WarningsAsErrors>` to the app so that a non-exhaustive switch breaks the build.

## ASP.NET Core

```csharp
builder.Services.AddProblemDetails();
app.UseExceptionHandler(); // unexpected exceptions: 500 ProblemDetails

app.MapGet("/api/ores/stock/{grid}", async (string grid, OresService ores, CancellationToken ct) => (await ores.GetStock(grid, ct)).ToHttpResult());
app.MapPost("/api/ores/refresh/{grid}", (string grid, OresService ores) => ores.Refresh(grid).ToHttpResult(run => TypedResults.Accepted($"/api/ores/refresh/{run.Id}", run)));
```

- `ToHttpResult()` maps a success to 200 with the value (204 for `Result<Unit>`). The overload `ToHttpResult(onSuccess)` builds any other success response (201, 202, …).
- A failure becomes a ProblemDetails whose status comes from the category, with `type`, `detail` and the `code` extension. `Invalid` with `Fields` gives a ValidationProblemDetails. ASP.NET adds `traceId` (`AddProblemDetails`).
- MVC controllers can return the `IResult` directly from an action: `public async Task<IResult> Get(...) => (await service.Get(...)).ToHttpResult();`. `error.ToProblem()` is also available on its own.
