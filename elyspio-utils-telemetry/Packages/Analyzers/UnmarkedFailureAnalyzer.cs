using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Elyspio.Utils.Telemetry.Analyzers;

/// <summary>
///     ELY0001: in a method holding a TraceScope, <c>return error;</c> converts the error to a failed Result without
///     marking the span. <c>return trace.Fail(error);</c> marks it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnmarkedFailureAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "ELY0001";

	internal const string ScopeProperty = "Scope";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Failure returned without marking the trace scope",
		"Return '{0}.Fail(...)' so the span of '{0}' is marked as failed",
		"Telemetry",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Returning an error converts it to a failed Result, but the span of the enclosing TraceScope stays successful. Wrap the error in trace.Fail(...).");

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(start =>
		{
			var types = KnownTypes.From(start.Compilation);
			if (types is null) return;

			start.RegisterOperationAction(operation => AnalyzeReturn(operation, types), OperationKind.Return);
		});
	}

	private static void AnalyzeReturn(OperationAnalysisContext context, KnownTypes types)
	{
		var returned = ((IReturnOperation)context.Operation).ReturnedValue;
		if (returned is not IConversionOperation { IsImplicit: true } conversion) return;
		if (conversion.Type is not INamedTypeSymbol { IsGenericType: true } target) return;

		var definition = target.OriginalDefinition;
		ITypeSymbol errorType;
		if (SymbolEqualityComparer.Default.Equals(definition, types.Result1)) errorType = types.Error;
		else if (SymbolEqualityComparer.Default.Equals(definition, types.Result2)) errorType = target.TypeArguments[1];
		else return;

		if (!IsError(conversion.Operand.Type, errorType, types)) return;

		var scope = FindScope(context.Operation.Syntax, context.Operation.SemanticModel!, types.TraceScope, context.CancellationToken);
		if (scope is null) return;

		var properties = ImmutableDictionary<string, string?>.Empty.Add(ScopeProperty, scope.Name);
		context.ReportDiagnostic(Diagnostic.Create(Rule, conversion.Operand.Syntax.GetLocation(), properties, scope.Name));
	}

	/// <summary>The returned value is an error: an Error subtype, the error type of the Result, or an Err case.</summary>
	private static bool IsError(ITypeSymbol? type, ITypeSymbol errorType, KnownTypes types)
	{
		if (type is null) return false;
		if (type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, types.Err)) return true;
		if (SymbolEqualityComparer.Default.Equals(type, errorType)) return true;

		for (var current = type; current is not null; current = current.BaseType)
		{
			if (SymbolEqualityComparer.Default.Equals(current, types.Error)) return true;
		}

		return false;
	}

	/// <summary>The TraceScope local declared before the return, in the same function (not in an enclosing one).</summary>
	private static ILocalSymbol? FindScope(SyntaxNode returnSyntax, SemanticModel model, INamedTypeSymbol traceScope, CancellationToken cancellationToken)
	{
		var function = returnSyntax.Ancestors().FirstOrDefault(IsFunction);
		if (function is null) return null;

		foreach (var declarator in function.DescendantNodes(node => node == function || !IsFunction(node)).OfType<VariableDeclaratorSyntax>())
		{
			if (declarator.SpanStart > returnSyntax.SpanStart) break;

			if (model.GetDeclaredSymbol(declarator, cancellationToken) is ILocalSymbol local && SymbolEqualityComparer.Default.Equals(local.Type, traceScope))
			{
				return local;
			}
		}

		return null;
	}

	private static bool IsFunction(SyntaxNode node) => node is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax
		or AnonymousFunctionExpressionSyntax;

	private sealed class KnownTypes(INamedTypeSymbol traceScope, INamedTypeSymbol result1, INamedTypeSymbol result2, INamedTypeSymbol err, INamedTypeSymbol error)
	{
		public INamedTypeSymbol TraceScope { get; } = traceScope;
		public INamedTypeSymbol Result1 { get; } = result1;
		public INamedTypeSymbol Result2 { get; } = result2;
		public INamedTypeSymbol Err { get; } = err;
		public INamedTypeSymbol Error { get; } = error;

		public static KnownTypes? From(Compilation compilation)
		{
			var traceScope = compilation.GetTypeByMetadataName("Elyspio.Utils.Telemetry.TraceScope");
			var result1 = compilation.GetTypeByMetadataName("Elyspio.Utils.Results.Result`1");
			var result2 = compilation.GetTypeByMetadataName("Elyspio.Utils.Results.Result`2");
			var err = compilation.GetTypeByMetadataName("Elyspio.Utils.Results.Err`1");
			var error = compilation.GetTypeByMetadataName("Elyspio.Utils.Results.Error");

			return traceScope is null || result1 is null || result2 is null || err is null || error is null
				? null
				: new KnownTypes(traceScope, result1, result2, err, error);
		}
	}
}
