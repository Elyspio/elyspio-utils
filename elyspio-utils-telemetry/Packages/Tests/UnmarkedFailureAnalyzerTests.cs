using System.Collections.Immutable;
using Elyspio.Utils.Results;
using Elyspio.Utils.Telemetry.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Elyspio.Utils.Telemetry.Tests;

public sealed class UnmarkedFailureAnalyzerTests
{
	private const string Header = """
		using Elyspio.Utils.Results;
		using Elyspio.Utils.Telemetry;
		using Microsoft.Extensions.Logging;

		public sealed record Missing() : NotFound("missing", "Missing.");

		public sealed class Service(ILogger<Service> logger)
		{
		""";

	private static async Task<ImmutableArray<Diagnostic>> Analyze(string members)
	{
		var source = $"{Header}\n{members}\n}}";
		var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
			.Append(typeof(Result<>).Assembly.Location)
			.Append(typeof(TraceScope).Assembly.Location)
			.Append(typeof(ILogger<>).Assembly.Location)
			.Distinct()
			.Select(path => MetadataReference.CreateFromFile(path));

		var compilation = CSharpCompilation.Create("Sample", [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))], references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
		return await compilation.WithAnalyzers([new UnmarkedFailureAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task Error_returned_in_a_traced_method_is_reported()
	{
		var diagnostics = await Analyze("""
			public Result<int> Find()
			{
				using var trace = logger.Trace();
				return new Missing();
			}
			""");

		diagnostics.Single().Id.ShouldBe(UnmarkedFailureAnalyzer.DiagnosticId);
		diagnostics.Single().GetMessage().ShouldContain("trace.Fail");
	}

	[Fact]
	public async Task Error_returned_by_an_async_traced_method_is_reported()
	{
		var diagnostics = await Analyze("""
			public async System.Threading.Tasks.Task<Result<int>> Find()
			{
				using var trace = logger.Trace();
				await System.Threading.Tasks.Task.Yield();
				Error error = new Missing();
				return error;
			}
			""");

		diagnostics.Length.ShouldBe(1);
	}

	[Fact]
	public async Task Custom_error_type_is_reported()
	{
		var diagnostics = await Analyze("""
			public Result<int, string> Parse()
			{
				using var trace = logger.Trace();
				return "not a number";
			}
			""");

		diagnostics.Length.ShouldBe(1);
	}

	[Fact]
	public async Task Marked_failures_values_and_untraced_methods_are_not_reported()
	{
		var diagnostics = await Analyze("""
			public Result<int> Marked()
			{
				using var trace = logger.Trace();
				return trace.Fail(new Missing());
			}

			public Result<int> Value()
			{
				using var trace = logger.Trace();
				return 1;
			}

			public Result<int> Relayed(Result<int> inner)
			{
				using var trace = logger.Trace();
				return trace.Return(inner);
			}

			public Result<int> Untraced() => new Missing();

			public Result<int> Lambda()
			{
				using var trace = logger.Trace();
				System.Func<Result<int>> nested = () => new Missing();
				return nested().IsSuccess ? 1 : trace.Fail(new Missing());
			}
			""");

		diagnostics.ShouldBeEmpty();
	}

	[Theory]
	[InlineData("new Missing()", "trace.Fail(new Missing())")]
	[InlineData("new Err<Error>(error)", "trace.Fail(error)")]
	public void Code_fix_wraps_the_error(string expression, string expected)
	{
		var wrapped = UnmarkedFailureCodeFix.Wrap(SyntaxFactory.ParseExpression(expression), "trace");

		wrapped.NormalizeWhitespace().ToFullString().ShouldBe(expected);
	}
}
