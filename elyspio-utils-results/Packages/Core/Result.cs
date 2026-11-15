using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elyspio.Utils.Results;

/// <summary>Success case of a result. A distinct type, so success and failure never overlap (even for Result&lt;string, string&gt;).</summary>
public readonly record struct Ok<T>(T Value);

/// <summary>Failure case of a result.</summary>
public readonly record struct Err<TError>(TError Error);

/// <summary>
///     A failure not yet typed: <c>return trace.Fail(error);</c> converts to the method's Result&lt;T&gt; without naming T.
/// </summary>
public readonly record struct Failed(Error Error);

/// <summary>Same as <see cref="Failed" /> for a custom error type: converts to any Result&lt;T, TError&gt;.</summary>
public readonly record struct Failed<TError>(TError Error);

/// <summary>Value of a success that carries nothing (e.g. an update): <c>Result&lt;Unit&gt;</c>.</summary>
public readonly record struct Unit
{
	public static Unit Value => default;
}

/// <summary>
///     Either <see cref="Ok{T}" /> or <see cref="Err{TError}" />, as a C# 15 custom union: typed storage (no boxing),
///     exhaustive <c>switch</c> over the two cases, implicit conversions from a value or an error.
///     <para>A default (uninitialized) result is neither: a switch on it throws, it never passes for a success.</para>
/// </summary>
[Union]
public readonly struct Result<T, TError> : IUnion where TError : notnull
{
	private readonly T _value;
	private readonly TError _error;
	private readonly ResultState _state;

	// The two public single-parameter constructors define the union case types.
	public Result(Ok<T> ok)
	{
		_value = ok.Value;
		_error = default!;
		_state = ResultState.Ok;
	}

	public Result(Err<TError> err)
	{
		_value = default!;
		_error = err.Error;
		_state = ResultState.Err;
	}

	public static implicit operator Result<T, TError>(T value) => new(new Ok<T>(value));

	public static implicit operator Result<T, TError>(TError error) => new(new Err<TError>(error));

	public static implicit operator Result<T, TError>(Failed<TError> failed) => new(new Err<TError>(failed.Error));

	public bool IsSuccess => _state == ResultState.Ok;

	public bool IsFailure => _state == ResultState.Err;

	/// <summary>Union contract: boxes. Pattern matching uses <see cref="TryGetValue(out Ok{T})" /> instead.</summary>
	public object? Value => _state switch
	{
		ResultState.Ok => new Ok<T>(_value),
		ResultState.Err => new Err<TError>(_error),
		_ => null,
	};

	public bool HasValue => _state != ResultState.None;

	public bool TryGetValue(out Ok<T> value)
	{
		value = new Ok<T>(_value);
		return _state == ResultState.Ok;
	}

	public bool TryGetValue(out Err<TError> value)
	{
		value = new Err<TError>(_error);
		return _state == ResultState.Err;
	}

	/// <summary>
	///     Value or error in one test: <c>if (!read.TryGet(out var value, out var error)) return trace.Fail(error);</c>
	///     A default (uninitialized) result throws.
	/// </summary>
	public bool TryGet([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out TError error)
	{
		if (_state == ResultState.None) throw ResultGuard.Uninitialized();

		value = _value;
		error = _error;
		return _state == ResultState.Ok;
	}

	public Result<TOut, TError> Map<TOut>(Func<T, TOut> map) => _state switch
	{
		ResultState.Ok => new Ok<TOut>(map(_value)),
		ResultState.Err => new Err<TError>(_error),
		_ => throw ResultGuard.Uninitialized(),
	};

	public Result<TOut, TError> Bind<TOut>(Func<T, Result<TOut, TError>> bind) => _state switch
	{
		ResultState.Ok => bind(_value),
		ResultState.Err => new Err<TError>(_error),
		_ => throw ResultGuard.Uninitialized(),
	};
}

/// <summary>
///     The common case: a value or an <see cref="Error" /> (whose category gives the HTTP status). C# has no generic
///     alias, so this is its own union over <see cref="Result{T, TError}" />.
/// </summary>
[Union]
public readonly struct Result<T> : IUnion
{
	private readonly Result<T, Error> _inner;

	public Result(Ok<T> ok) => _inner = new Result<T, Error>(ok);

	public Result(Err<Error> err) => _inner = new Result<T, Error>(err);

	private Result(Result<T, Error> inner) => _inner = inner;

	public static implicit operator Result<T>(T value) => new(new Ok<T>(value));

	public static implicit operator Result<T>(Error error) => new(new Err<Error>(error));

	public static implicit operator Result<T>(Failed failed) => new(new Err<Error>(failed.Error));

	public static implicit operator Result<T, Error>(Result<T> result) => result._inner;

	public static implicit operator Result<T>(Result<T, Error> result) => new(result);

	public bool IsSuccess => _inner.IsSuccess;

	public bool IsFailure => _inner.IsFailure;

	public object? Value => _inner.Value;

	public bool HasValue => _inner.HasValue;

	public bool TryGetValue(out Ok<T> value) => _inner.TryGetValue(out value);

	public bool TryGetValue(out Err<Error> value) => _inner.TryGetValue(out value);

	/// <inheritdoc cref="Result{T, TError}.TryGet" />
	public bool TryGet([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out Error error) => _inner.TryGet(out value, out error);

	public Result<TOut> Map<TOut>(Func<T, TOut> map) => new(_inner.Map(map));

	public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind) => new(_inner.Bind<TOut>(value => bind(value)));
}

internal enum ResultState : byte
{
	None,
	Ok,
	Err,
}

internal static class ResultGuard
{
	public static InvalidOperationException Uninitialized() => new("Uninitialized result (default value): neither a success nor a failure.");
}
