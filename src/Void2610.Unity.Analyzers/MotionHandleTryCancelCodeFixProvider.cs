using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Void2610.Unity.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class MotionHandleTryCancelCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("VUA1003");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];
            var node = root.FindNode(diagnostic.Location.SourceSpan);

            var ifStatement = node.FirstAncestorOrSelf<IfStatementSyntax>();
            if (ifStatement == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    "TryCancel() に置換",
                    ct => ReplaceWithTryCancelAsync(context.Document, ifStatement, ct),
                    nameof(MotionHandleTryCancelCodeFixProvider)),
                diagnostic);
        }

        private static async Task<Document> ReplaceWithTryCancelAsync(
            Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // 条件部から対象オブジェクトを取得: target.IsActive()
            if (!(ifStatement.Condition is InvocationExpressionSyntax invocation))
                return document;
            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
                return document;

            var target = memberAccess.Expression;

            var tryCancelExpression = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        target,
                        SyntaxFactory.IdentifierName("TryCancel"))))
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(ifStatement, tryCancelExpression);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
