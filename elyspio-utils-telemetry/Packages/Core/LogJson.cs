using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Elyspio.Utils.Telemetry;

/// <summary>Never written by <c>Log.F</c>: on a property/field, or on a type (every property of that type).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class LogIgnoreAttribute : Attribute;

/// <summary>Written by <c>Log.F</c> as <c>"***"</c> (secrets, personal data).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class LogMaskAttribute : Attribute;

public sealed class LogFieldOptions
{
	private static Snapshot _current = new(new LogFieldOptions());

	internal static Snapshot Current => Volatile.Read(ref _current);

	/// <summary>
	///     Max UTF-8 bytes of one value (beyond: cut, then <c>…</c>). Bounds the memory too: the serializer keeps writing
	///     into a small reused overflow buffer that is thrown away.
	/// </summary>
	public int MaxLength { get; set; } = 8 * 1024;

	/// <summary>Max object nesting written (deeper: the value is cut with an error marker).</summary>
	public int MaxDepth { get; set; } = 16;

	/// <summary>Readable logs by default (<c>é</c> stays <c>é</c>): a log is never rendered as HTML.</summary>
	public JavaScriptEncoder Encoder { get; set; } = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

	/// <summary>Source-generated contexts (AOT); tried before reflection, which is the fallback only when enabled.</summary>
	public IList<IJsonTypeInfoResolver> TypeInfoResolvers { get; } = [];

	/// <summary>byte[] / Memory&lt;byte&gt; written as <c>"&lt;1234 bytes&gt;"</c>, Stream properties skipped.</summary>
	public bool SummarizeBinary { get; set; } = true;

	/// <summary>Last-chance customization (converters, naming policy…) of the log-only serializer options.</summary>
	public Action<JsonSerializerOptions>? Json { get; set; }

	internal HashSet<(Type Type, string Property)> IgnoredProperties { get; } = [];

	/// <summary>Properties of a type we do not own: <c>o.Ignore&lt;UploadRequest&gt;(nameof(UploadRequest.Content))</c>.</summary>
	public LogFieldOptions Ignore<T>(params ReadOnlySpan<string> properties)
	{
		foreach (var property in properties) IgnoredProperties.Add((typeof(T), property));
		return this;
	}

	/// <summary>Once at startup, before the first log.</summary>
	public static void Configure(Action<LogFieldOptions> configure)
	{
		var options = new LogFieldOptions();
		configure(options);
		Volatile.Write(ref _current, new Snapshot(options));
	}

	/// <summary>Immutable settings read on each log.</summary>
	internal sealed class Snapshot
	{
		public Snapshot(LogFieldOptions options)
		{
			MaxLength = options.MaxLength;
			Encoder = options.Encoder;

			var resolvers = new List<IJsonTypeInfoResolver>(options.TypeInfoResolvers);
			if (JsonSerializer.IsReflectionEnabledByDefault) resolvers.Add(ReflectionResolver());

			var rules = new LogContractRules(options.IgnoredProperties, options.SummarizeBinary);
			Json = new JsonSerializerOptions
			{
				TypeInfoResolver = JsonTypeInfoResolver.Combine([.. resolvers]).WithAddedModifier(rules.Apply),
				Encoder = options.Encoder,
				MaxDepth = options.MaxDepth,
				NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
			};
			options.Json?.Invoke(Json);
			Json.MakeReadOnly();

			WriterOptions = new JsonWriterOptions { Encoder = options.Encoder, MaxDepth = options.MaxDepth, SkipValidation = true };
		}

		public int MaxLength { get; }
		public JavaScriptEncoder Encoder { get; }
		public JsonSerializerOptions Json { get; }
		public JsonWriterOptions WriterOptions { get; }

		// Only reached when reflection-based serialization is enabled (never in a trimmed/AOT app).
		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
		[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
		private static IJsonTypeInfoResolver ReflectionResolver() => new DefaultJsonTypeInfoResolver();
	}
}

/// <summary>Contract modifier of the log serializer: [LogIgnore], [LogMask], binary summaries.</summary>
internal sealed class LogContractRules(HashSet<(Type Type, string Property)> ignored, bool summarizeBinary)
{
	private const string MsLogPropertyIgnore = "Microsoft.Extensions.Logging.LogPropertyIgnoreAttribute";

	public void Apply(JsonTypeInfo type)
	{
		if (type.Kind != JsonTypeInfoKind.Object) return;

		// A [LogIgnore] type logged directly (not as a property) is written as an empty object.
		if (type.Type.IsDefined(typeof(LogIgnoreAttribute), false))
		{
			type.Properties.Clear();
			return;
		}

		for (var i = type.Properties.Count - 1; i >= 0; i--)
		{
			var property = type.Properties[i];

			if (IsIgnored(type.Type, property))
			{
				type.Properties.RemoveAt(i);
				continue;
			}

			if (Has<LogMaskAttribute>(property.AttributeProvider))
			{
				// AOT-safe: a converter must match the property type, so only strings are masked; anything else is dropped.
				if (property.PropertyType == typeof(string)) property.CustomConverter = MaskConverter.Instance;
				else type.Properties.RemoveAt(i);
				continue;
			}

			if (!summarizeBinary) continue;
			if (property.PropertyType == typeof(byte[])) property.CustomConverter = BinarySummary.Array;
			else if (property.PropertyType == typeof(ReadOnlyMemory<byte>)) property.CustomConverter = BinarySummary.ReadOnlyMemory;
			else if (property.PropertyType == typeof(Memory<byte>)) property.CustomConverter = BinarySummary.Memory;
			else if (typeof(Stream).IsAssignableFrom(property.PropertyType)) type.Properties.RemoveAt(i);
		}
	}

	private bool IsIgnored(Type owner, JsonPropertyInfo property)
	{
		if (ignored.Contains((owner, property.Name))) return true;
		if (property.AttributeProvider is MemberInfo member && ignored.Contains((owner, member.Name))) return true;
		if (Has<LogIgnoreAttribute>(property.AttributeProvider)) return true;
		if (property.AttributeProvider?.GetCustomAttributes(false).Any(a => a.GetType().FullName == MsLogPropertyIgnore) == true) return true;
		return property.PropertyType.IsDefined(typeof(LogIgnoreAttribute), false);
	}

	private static bool Has<TAttribute>(ICustomAttributeProvider? provider) where TAttribute : Attribute
		=> provider?.IsDefined(typeof(TAttribute), true) == true;
}

internal sealed class MaskConverter : JsonConverter<string>
{
	public static readonly MaskConverter Instance = new();

	public override bool HandleNull => true;

	public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

	public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue("***");
}

/// <summary><c>"&lt;1234 bytes&gt;"</c> without allocating.</summary>
internal static class BinarySummary
{
	public static readonly JsonConverter<byte[]> Array = new Converter<byte[]>(static b => b.Length);
	public static readonly JsonConverter<ReadOnlyMemory<byte>> ReadOnlyMemory = new Converter<ReadOnlyMemory<byte>>(static b => b.Length);
	public static readonly JsonConverter<Memory<byte>> Memory = new Converter<Memory<byte>>(static b => b.Length);

	private sealed class Converter<T>(Func<T, int> length) : JsonConverter<T>
	{
		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			Span<char> text = stackalloc char[32];
			text.TryWrite($"({length(value)} bytes)", out var written);
			writer.WriteStringValue(text[..written]);
		}
	}
}

/// <summary>Per-thread serialization buffers (a nested Log.F, e.g. from a getter, gets its own).</summary>
internal sealed class JsonScratch
{
	[ThreadStatic] private static JsonScratch? _cached;

	private readonly CappedBufferWriter _buffer = new();
	private Utf8JsonWriter? _writer;
	private JsonWriterOptions _writerOptions;

	public static JsonScratch Rent()
	{
		var scratch = _cached ?? new JsonScratch();
		_cached = null;
		return scratch;
	}

	public static void Return(JsonScratch scratch)
	{
		scratch._buffer.Clear();
		_cached = scratch;
	}

	/// <summary>False when the value could not be serialized: the buffer then holds an error marker.</summary>
	public bool Serialize<T>(T value, LogFieldOptions.Snapshot settings)
	{
		_buffer.Reset(settings.MaxLength);
		var writer = Writer(settings.WriterOptions);
		try
		{
			// No [RequiresUnreferencedCode]: GetTypeInfo only uses the configured resolvers (throws if none knows T).
			JsonSerializer.Serialize(writer, value, (JsonTypeInfo<T>)settings.Json.GetTypeInfo(typeof(T)));
			writer.Flush();
			return true;
		}
		catch (Exception exception)
		{
			// Logging never throws. Too deep (a cycle) or a value too big for the cap: keep what was written, shown cut ("…").
			if (exception is JsonException or CappedBufferWriter.OversizedValueException && TryKeepPartial(writer)) return false;

			// Unsupported type, AOT without metadata, a getter that throws…: an error marker.
			_buffer.Reset(settings.MaxLength);
			writer.Reset(_buffer);
			writer.WriteStringValue($"(unserializable {typeof(T).Name}: {exception.GetType().Name})");
			writer.Flush();
			return false;
		}
	}

	private bool TryKeepPartial(Utf8JsonWriter writer)
	{
		try
		{
			writer.Flush();
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		return _buffer.MarkCut();
	}

	/// <summary>The serialized bytes, cut at the cap on a UTF-8 character boundary.</summary>
	public ReadOnlyMemory<byte> Visible(int maxLength, out bool truncated) => _buffer.Visible(maxLength, out truncated);

	private Utf8JsonWriter Writer(JsonWriterOptions options)
	{
		if (_writer is null || !SameOptions(_writerOptions, options))
		{
			_writer = new Utf8JsonWriter(_buffer, options);
			_writerOptions = options;
		}
		else
		{
			_writer.Reset(_buffer);
		}

		return _writer;
	}

	private static bool SameOptions(JsonWriterOptions a, JsonWriterOptions b) => a.Encoder == b.Encoder && a.MaxDepth == b.MaxDepth;
}

/// <summary>
///     Pooled <see cref="IBufferWriter{T}" /> that keeps at most ~cap bytes: past the cap, the serializer gets a small
///     reused overflow buffer whose content is dropped (memory stays bounded even for a huge object).
/// </summary>
internal sealed class CappedBufferWriter : IBufferWriter<byte>
{
	private const int MinChunk = 256;
	private const int RetainedMax = 64 * 1024;

	private byte[] _buffer = [];
	private byte[]? _overflow;
	private int _written;
	private int _cap;
	private bool _inOverflow;
	private bool _cut;

	public long Dropped { get; private set; }

	/// <summary>The content is incomplete (serialization failed midway); false when there is none.</summary>
	public bool MarkCut()
	{
		_cut = _written > 0;
		return _cut;
	}

	public void Reset(int cap)
	{
		_cut = false;
		_written = 0;
		_cap = cap;
		_inOverflow = false;
		Dropped = 0;
	}

	public void Advance(int count)
	{
		if (_inOverflow) Dropped += count;
		else _written += count;
	}

	public Memory<byte> GetMemory(int sizeHint = 0) => Chunk(sizeHint);

	public Span<byte> GetSpan(int sizeHint = 0) => Chunk(sizeHint).Span;

	public ReadOnlyMemory<byte> Visible(int maxLength, out bool truncated)
	{
		truncated = _cut || Dropped > 0 || _written > maxLength;
		var length = Math.Min(_written, maxLength);

		// Do not cut a multi-byte character in half.
		if (_written > maxLength)
			while (length > 0 && (_buffer[length] & 0xC0) == 0x80) length--;

		return _buffer.AsMemory(0, length);
	}

	/// <summary>Gives big buffers back to the pool so an occasional huge log does not pin memory on the thread.</summary>
	public void Clear()
	{
		if (_buffer.Length > RetainedMax) Replace(ref _buffer, []);
		if (_overflow is { Length: > RetainedMax }) Swap(ref _overflow, null);
	}

	private Memory<byte> Chunk(int sizeHint)
	{
		// The writer asks for a whole value at once (a 4 M characters string asks ~12 MB): past the cap plus a margin,
		// the value is dropped instead of buffered.
		if (sizeHint > _cap + RetainedMax) throw new OversizedValueException();

		sizeHint = Math.Max(sizeHint, MinChunk);

		_inOverflow = _written >= _cap;
		if (_inOverflow)
		{
			if (_overflow is null || _overflow.Length < sizeHint) Swap(ref _overflow, ArrayPool<byte>.Shared.Rent(sizeHint));
			return _overflow;
		}

		if (_buffer.Length - _written < sizeHint)
		{
			var grown = ArrayPool<byte>.Shared.Rent(Math.Max(_written + sizeHint, _buffer.Length * 2));
			_buffer.AsSpan(0, _written).CopyTo(grown);
			Replace(ref _buffer, grown);
		}

		return _buffer.AsMemory(_written);
	}

	/// <summary>Stops the serialization of a value bigger than the cap; caught by <see cref="JsonScratch.Serialize{T}" />.</summary>
	internal sealed class OversizedValueException() : Exception("Value too big for the log field cap.");

	private static void Swap(ref byte[]? field, byte[]? value)
	{
		if (field is { Length: > 0 }) ArrayPool<byte>.Shared.Return(field);
		field = value;
	}

	private static void Replace(ref byte[] field, byte[] value)
	{
		if (field.Length > 0) ArrayPool<byte>.Shared.Return(field);
		field = value;
	}
}
