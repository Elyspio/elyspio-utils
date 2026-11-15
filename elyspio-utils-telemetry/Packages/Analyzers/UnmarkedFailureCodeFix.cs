using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Elyspio.Utils.Telemetry.Analyzers;

/// <summary>ELY0001 fix: <c>return error;</c> becomes <c>return trace.Fail(error);</c>.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnmarkedFailureCodeFix))]
[Shared]
public sealed class UnmarkedFailureCodeFix : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds => [UnmarkedFailureAnalyzer.DiagnosticId];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null || model is null) return;

		foreach (var diagnostic in context.Diagnostics)
		{
			if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not ExpressionSyntax expression) continue;
			if (!diagnostic.Properties.TryGetValue(UnmarkedFailureAnalyzer.ScopeProperty, out var scope) || scope is null) continue;

			var errCase = model.GetTypeInfo(expression, context.CancellationToken).Type is INamedTypeSymbol { Name: "Err", IsGenericType: true } type
						  && type.ContainingNamespace.ToDisplayString() == "Elyspio.Utils.Results";
			var fixedRoot = root.ReplaceNode(expression, Wrap(expression, scope, errCase));
			context.RegisterCodeFix(CodeAction.Create($"Return {scope}.Fail(...)", _ => Task.FromResult(context.Document.WithSyntaxRoot(fixedRoot)), nameof(UnmarkedFailureCodeFix)),
				diagnostic);
		}
	}

	/// <param name="errCase">The expression is an <c>Err&lt;TError&gt;</c> value: its error is passed, not the case itself.</param>
	internal static ExpressionSyntax Wrap(ExpressionSyntax expression, string scope, bool errCase = false)
	{
		// An Err<TError> case carries the error as its only argument.
		var argument = expression switch
		{
			ObjectCreationExpressionSyntax { Type: GenericNameSyntax { Identifier.Text: "Err" } or QualifiedNameSyntax { Right: GenericNameSyntax { Identifier.Text: "Err" } }, ArgumentList.Arguments.Count: 1 } creation
				=> creation.ArgumentList!.Arguments[0].Expression,
			_ when errCase => SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
				expression is IdentifierNameSyntax or MemberAccessExpressionSyntax ? expression.WithoutTrivia() : SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia()),
				SyntaxFactory.IdentifierName("Error")),
			_ => expression.WithoutTrivia()
		};

		var fail = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(scope), SyntaxFactory.IdentifierName("Fail"));

		return SyntaxFactory.InvocationExpression(fail, SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(argument))))
			.WithTriviaFrom(expression);
	}
}
