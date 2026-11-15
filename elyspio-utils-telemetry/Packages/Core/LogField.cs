using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Elyspio.Utils.Telemetry;

public static partial class Log
{
	/// <summary>
	///     <c>$"{Log.F(id)}"</c> gives <c>id=42</c>, <c>$"{Log.F(request)}"</c> gives <c>request={"A":1}</c>. Nothing is
	///     computed here: the value is formatted by the handler, only when the log is enabled.
	/// </summary>
	public static LogField<T> F<T>(T? value, [CallerArgumentExpression(nameof(value))] string name = "") => new(name, value);
}

/// <summary>
///     A <c>name=value</c> pair. Our handlers format it without intermediate strings (dedicated <c>AppendFormatted</c>
///     overload); anywhere else (<c>$"..."</c>, another handler) it is an <see cref="ISpanFormattable" /> struct, so it is
///     still not boxed and writes into the destination span.
/// </summary>
public readonly struct LogField<T> : ISpanFormattable
{
	internal LogField(string name, T? value)
	{
		Name = name;
		Value = value;
	}

	public string Name { get; }

	public T? Value { get; }

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
		=> LogFieldWriter.TryFormat(Name, Value, destination, out charsWritten);

	public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

	public override string ToString()
	{
		var handler = new DefaultInterpolatedStringHandler(0, 1);
		LogFieldWriter.Append(ref handler, this);
		return handler.ToStringAndClear();
	}
}

/// <summary>Formatting of <see cref="LogField{T}" />, shared by the handlers.</summary>
public static class LogFieldWriter
{
	private const string Ellipsis = "…";

	/// <summary>
	///     Appends <c>name=value</c> to <paramref name="handler" />: scalars through <see cref="ISpanFormattable" /> (a retry
	///     only formats again a number), everything else serialized once into a pooled UTF-8 buffer then transcoded into the
	///     handler buffer.
	/// </summary>
	public static void Append<T>(ref DefaultInterpolatedStringHandler handler, LogField<T> field)
	{
		if (field.Value is null || LogScalar.Is<T>())
		{
			handler.AppendFormatted(field);
			return;
		}

		var settings = LogFieldOptions.Current;
		var scratch = JsonScratch.Rent();
		try
		{
			scratch.Serialize(field.Value, settings);
			var json = scratch.Visible(settings.MaxLength, out var truncated);

			handler.AppendLiteral(field.Name);
			handler.AppendLiteral("=");
			handler.AppendFormatted(new Utf8Text(json));
			if (truncated) handler.AppendLiteral(Ellipsis);
		}
		finally
		{
			JsonScratch.Return(scratch);
		}
	}

	/// <summary>Fallback when the field is not given to one of our handlers: a complex value is serialized again on each retry.</summary>
	internal static bool TryFormat<T>(string name, T? value, Span<char> destination, out int charsWritten)
	{
		charsWritten = 0;
		if (destination.Length <= name.Length) return false;

		name.CopyTo(destination);
		destination[name.Length] = '=';
		var position = name.Length + 1;
		var rest = destination[position..];
		var settings = LogFieldOptions.Current;

		int written;
		if (value is null)
		{
			if (!"null".TryCopyTo(rest)) return false;
			written = 4;
		}
		else if (LogScalar.Is<T>())
		{
			if (!LogScalar.TryFormat(value, rest, out written, settings)) return false;
		}
		else
		{
			var scratch = JsonScratch.Rent();
			try
			{
				scratch.Serialize(value, settings);
				var json = scratch.Visible(settings.MaxLength, out var truncated);
				if (Utf8.ToUtf16(json.Span, rest, out _, out written) != OperationStatus.Done) return false;
				if (truncated)
				{
					if (!Ellipsis.TryCopyTo(rest[written..])) return false;
					written += Ellipsis.Length;
				}
			}
			finally
			{
				JsonScratch.Return(scratch);
			}
		}

		charsWritten = position + written;
		return true;
	}
}

/// <summary>UTF-8 bytes transcoded into the handler buffer: a retry only transcodes again, it never serializes again.</summary>
internal readonly struct Utf8Text(ReadOnlyMemory<byte> utf8) : ISpanFormattable
{
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
		=> Utf8.ToUtf16(utf8.Span, destination, out _, out charsWritten) == OperationStatus.Done;

	public string ToString(string? format, IFormatProvider? formatProvider) => ToString(utf8.Span);

	public static string ToString(ReadOnlySpan<byte> utf8) => System.Text.Encoding.UTF8.GetString(utf8);
}

/// <summary>Types written without System.Text.Json, with the exact same output.</summary>
internal static class LogScalar
{
	private static readonly object True = true;
	private static readonly object False = false;

	// typeof(T) == typeof(X) is a JIT constant: each instantiation keeps a single branch.
	public static bool Is<T>() =>
		typeof(T) == typeof(string) || typeof(T) == typeof(char) || typeof(T) == typeof(bool)
		|| typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(short) || typeof(T) == typeof(byte)
		|| typeof(T) == typeof(uint) || typeof(T) == typeof(ulong) || typeof(T) == typeof(ushort) || typeof(T) == typeof(sbyte)
		|| typeof(T) == typeof(double) || typeof(T) == typeof(float) || typeof(T) == typeof(decimal)
		|| typeof(T) == typeof(Guid) || typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTimeOffset)
		|| typeof(T) == typeof(TimeSpan) || typeof(T) == typeof(DateOnly)
		|| typeof(T) == typeof(int?) || typeof(T) == typeof(long?) || typeof(T) == typeof(bool?) || typeof(T) == typeof(double?)
		|| typeof(T) == typeof(decimal?) || typeof(T) == typeof(Guid?) || typeof(T) == typeof(DateTime?) || typeof(T) == typeof(DateTimeOffset?);

	/// <summary>Value of a span/log attribute (boxes value types, bool cached).</summary>
	public static object? Box<T>(T? value)
	{
		if (value is null) return null;
		if (typeof(T) == typeof(bool)) return Unsafe.As<T, bool>(ref value) ? True : False;
		return value;
	}

	/// <summary>Non-null value of a type accepted by <see cref="Is{T}" />.</summary>
	public static bool TryFormat<T>(T value, Span<char> d, out int w, LogFieldOptions.Snapshot settings)
	{
		var inv = CultureInfo.InvariantCulture;

		if (typeof(T) == typeof(string)) return TryFormatString(((string)(object)value!).AsSpan(), d, out w, settings);
		if (typeof(T) == typeof(char)) return TryFormatString(new ReadOnlySpan<char>(in Unsafe.As<T, char>(ref value)), d, out w, settings);
		if (typeof(T) == typeof(bool)) return TryCopy(Unsafe.As<T, bool>(ref value) ? "true" : "false", d, out w);
		if (typeof(T) == typeof(int)) return Unsafe.As<T, int>(ref value).TryFormat(d, out w, default, inv);
		if (typeof(T) == typeof(long)) return Unsafe.As<T, long>(ref value).TryFormat(d, out w, default, inv);
		if (typeof(T) == typeof(short)) return Unsafe.As<T, short>(ref value).TryFormat(d, out w, default, inv);
		if (typeof(T) == typeof(byte)) return Unsafe.As<T, byte>(ref value).TryFormat(d, out w, default, inv);
		if (typeof(T) == typeof(uint)) return Unsafe.As<T, uint>(ref value).TryFormat(d, out w, default, inv);
		if (typeof(T) == typeof(ulong)) return Unsafe.As<T, ulong>(ref value).TryFormat(d, out w, default, inv);
		if (typeof(T) == typeof(ushort)) return Unsafe.As<T, ushort>(ref value).TryFormat(d, out w, default, inv);
		if (typeof(T) == typeof(sbyte)) return Unsafe.As<T, sbyte>(ref value).TryFormat(d, out w, default, inv);
		if (typeof(T) == typeof(double)) return TryFormatFloating(Unsafe.As<T, double>(ref value), d, out w);
		if (typeof(T) == typeof(float)) return TryFormatFloating(Unsafe.As<T, float>(ref value), d, out w);
		if (typeof(T) == typeof(decimal)) return Unsafe.As<T, decimal>(ref value).TryFormat(d, out w, default, inv);
		if (typeof(T) == typeof(Guid)) return TryQuoted(Unsafe.As<T, Guid>(ref value), d, out w, "D");
		// System.Text.Json writes ISO 8601 with the trailing zeros of the fraction trimmed.
		if (typeof(T) == typeof(DateTime)) return TryQuoted(Unsafe.As<T, DateTime>(ref value), d, out w, "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK");
		if (typeof(T) == typeof(DateTimeOffset)) return TryQuoted(Unsafe.As<T, DateTimeOffset>(ref value), d, out w, "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFzzz");
		if (typeof(T) == typeof(TimeSpan)) return TryQuoted(Unsafe.As<T, TimeSpan>(ref value), d, out w, "c");
		if (typeof(T) == typeof(DateOnly)) return TryQuoted(Unsafe.As<T, DateOnly>(ref value), d, out w, "yyyy'-'MM'-'dd");

		// Nullable<X>: the caller already handled null.
		if (typeof(T) == typeof(int?)) return TryFormat(Unsafe.As<T, int?>(ref value).GetValueOrDefault(), d, out w, settings);
		if (typeof(T) == typeof(long?)) return TryFormat(Unsafe.As<T, long?>(ref value).GetValueOrDefault(), d, out w, settings);
		if (typeof(T) == typeof(bool?)) return TryFormat(Unsafe.As<T, bool?>(ref value).GetValueOrDefault(), d, out w, settings);
		if (typeof(T) == typeof(double?)) return TryFormat(Unsafe.As<T, double?>(ref value).GetValueOrDefault(), d, out w, settings);
		if (typeof(T) == typeof(decimal?)) return TryFormat(Unsafe.As<T, decimal?>(ref value).GetValueOrDefault(), d, out w, settings);
		if (typeof(T) == typeof(Guid?)) return TryFormat(Unsafe.As<T, Guid?>(ref value).GetValueOrDefault(), d, out w, settings);
		if (typeof(T) == typeof(DateTime?)) return TryFormat(Unsafe.As<T, DateTime?>(ref value).GetValueOrDefault(), d, out w, settings);
		if (typeof(T) == typeof(DateTimeOffset?)) return TryFormat(Unsafe.As<T, DateTimeOffset?>(ref value).GetValueOrDefault(), d, out w, settings);

		throw new UnreachableException(typeof(T).FullName);
	}

	/// <summary>JSON string escaped by the same encoder as the serializer; longer than MaxLength: cut, then <c>…</c>.</summary>
	private static bool TryFormatString(ReadOnlySpan<char> value, Span<char> d, out int w, LogFieldOptions.Snapshot settings)
	{
		w = 0;
		var truncated = value.Length > settings.MaxLength;
		if (truncated) value = value[..settings.MaxLength];
		if (d.Length < 2) return false;

		d[0] = '"';
		if (settings.Encoder.Encode(value, d[1..], out _, out var written) != OperationStatus.Done) return false;
		var end = written + 1;
		if (end >= d.Length) return false;

		d[end++] = '"';
		if (truncated)
		{
			if (end >= d.Length) return false;
			d[end++] = '…';
		}

		w = end;
		return true;
	}

	private static bool TryFormatFloating<TFloat>(TFloat value, Span<char> d, out int w) where TFloat : IFloatingPoint<TFloat>
	{
		// Shortest round-trippable, like Utf8JsonWriter; NaN/Infinity quoted (AllowNamedFloatingPointLiterals) instead of throwing.
		if (TFloat.IsFinite(value)) return value.TryFormat(d, out w, default, CultureInfo.InvariantCulture);
		return TryQuoted(value, d, out w, null);
	}

	private static bool TryQuoted<TValue>(TValue value, Span<char> d, out int w, string? format) where TValue : ISpanFormattable
	{
		w = 0;
		if (d.Length < 2) return false;

		d[0] = '"';
		if (!value.TryFormat(d[1..], out var written, format, CultureInfo.InvariantCulture) || written + 2 > d.Length) return false;

		d[written + 1] = '"';
		w = written + 2;
		return true;
	}

	private static bool TryCopy(string text, Span<char> d, out int w)
	{
		w = text.TryCopyTo(d) ? text.Length : 0;
		return w != 0;
	}
}
