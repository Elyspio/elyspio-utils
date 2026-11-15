namespace Elyspio.Utils.Results;

/// <summary>
///     Root of every error. <c>closed</c>: only the categories below derive from it directly, so a switch over the
///     categories is exhaustive (the HTTP mapping has no default arm). The categories are open: an app derives its
///     own errors from them (<c>record InvalidResourceId(...) : Invalid(...)</c>).
/// </summary>
/// <param name="Code">Stable identifier (ProblemDetails <c>type</c>, span <c>error.type</c>), e.g. <c>ores.invalid-resource-id</c>.</param>
/// <param name="Message">Human-readable message, shown to the caller (except for <see cref="Unexpected" />).</param>
public closed record Error(string Code, string Message)
{
	/// <summary>Extra data published to the caller (ProblemDetails extensions). Nothing by default: opt-in per error.</summary>
	public virtual IEnumerable<KeyValuePair<string, object?>> Extensions => [];
}

/// <summary>400. <see cref="Fields" /> gives per-field messages (ValidationProblemDetails).</summary>
public record Invalid(string Code, string Message) : Error(Code, Message)
{
	public IReadOnlyDictionary<string, string[]>? Fields { get; init; }
}

/// <summary>401.</summary>
public record Unauthorized(string Code, string Message) : Error(Code, Message);

/// <summary>403.</summary>
public record Forbidden(string Code, string Message) : Error(Code, Message);

/// <summary>404.</summary>
public record NotFound(string Code, string Message) : Error(Code, Message);

/// <summary>409.</summary>
public record Conflict(string Code, string Message) : Error(Code, Message);

/// <summary>429.</summary>
public record TooManyRequests(string Code, string Message) : Error(Code, Message);

/// <summary>500. The message stays server-side: the caller gets a neutral message and the trace id.</summary>
public record Unexpected(string Code, string Message) : Error(Code, Message);

/// <summary>502: an upstream service answered with an error.</summary>
public record BadGateway(string Code, string Message) : Error(Code, Message);

/// <summary>503: a dependency is down or not configured.</summary>
public record Unavailable(string Code, string Message) : Error(Code, Message);

/// <summary>504: an upstream call took too long.</summary>
public record Timeout(string Code, string Message) : Error(Code, Message);

/// <summary>Escape hatch: any other status, without waiting for a new category in the library.</summary>
public record Failure(int Status, string Code, string Message) : Error(Code, Message);
