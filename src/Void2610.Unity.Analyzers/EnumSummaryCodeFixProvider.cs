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
    public sealed class EnumSummaryCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("VUA0006");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];
            var node = root.FindNode(diagnostic.Location.SourceSpan);

            var enumMember = node.FirstAncestorOrSelf<EnumMemberDeclarationSyntax>();
            if (enumMember == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    "/// <summary> コメントを追加",
                    ct => AddSummaryCommentAsync(context.Document, enumMember, ct),
                    nameof(EnumSummaryCodeFixProvider)),
                diagnostic);
        }

        private static async Task<Document> AddSummaryCommentAsync(
            Document document, EnumMemberDeclarationSyntax enumMember, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var existingTrivia = enumMember.GetLeadingTrivia();

            // 末尾のWhitespaceTrivia（インデント）を分離
            var indentation = "    ";
            var triviaBeforeIndent = new SyntaxTriviaList();

            if (existingTrivia.Count > 0 && existingTrivia.Last().IsKind(SyntaxKind.WhitespaceTrivia))
            {
                indentation = existingTrivia.Last().ToString();
                for (var i = 0; i < existingTrivia.Count - 1; i++)
                    triviaBeforeIndent = triviaBeforeIndent.Add(existingTrivia[i]);
            }
            else
            {
                triviaBeforeIndent = existingTrivia;
            }

            // 1行形式のsummaryコメント + メンバーのインデントを構築
            var summaryTrivia = SyntaxFactory.ParseLeadingTrivia(
                indentation + "/// <summary>  </summary>\n" +
                indentation);

            // 元のtrivia（インデント除外） + summaryコメント を結合
            var newTrivia = triviaBeforeIndent.AddRange(summaryTrivia);
            var newEnumMember = enumMember.WithLeadingTrivia(newTrivia);

            var newRoot = root.ReplaceNode(enumMember, newEnumMember);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
