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
		if (root is null) return;

		foreach (var diagnostic in context.Diagnostics)
		{
			if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not ExpressionSyntax expression) continue;
			if (!diagnostic.Properties.TryGetValue(UnmarkedFailureAnalyzer.ScopeProperty, out var scope) || scope is null) continue;

			var fixedRoot = root.ReplaceNode(expression, Wrap(expression, scope));
			context.RegisterCodeFix(CodeAction.Create($"Return {scope}.Fail(...)", _ => Task.FromResult(context.Document.WithSyntaxRoot(fixedRoot)), nameof(UnmarkedFailureCodeFix)),
				diagnostic);
		}
	}

	internal static ExpressionSyntax Wrap(ExpressionSyntax expression, string scope)
	{
		// An Err<TError> case carries the error as its only argument.
		var argument = expression is ObjectCreationExpressionSyntax { Type: GenericNameSyntax { Identifier.Text: "Err" }, ArgumentList.Arguments.Count: 1 } creation
			? creation.ArgumentList!.Arguments[0].Expression
			: expression.WithoutTrivia();

		var fail = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(scope), SyntaxFactory.IdentifierName("Fail"));

		return SyntaxFactory.InvocationExpression(fail, SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(argument))))
			.WithTriviaFrom(expression);
	}
}
