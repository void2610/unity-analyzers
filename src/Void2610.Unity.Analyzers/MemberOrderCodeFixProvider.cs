using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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
    public sealed class MemberOrderCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("VUA0005");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];
            var node = root.FindNode(diagnostic.Location.SourceSpan);

            var typeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (typeDeclaration == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    "メンバーの宣言順序を修正",
                    ct => ReorderMembersAsync(context.Document, typeDeclaration, ct),
                    nameof(MemberOrderCodeFixProvider)),
                diagnostic);
        }

        private static async Task<Document> ReorderMembersAsync(
            Document document, TypeDeclarationSyntax typeDeclaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var members = typeDeclaration.Members;

            // 各メンバーをカテゴリで分類
            var categorized = new List<(MemberDeclarationSyntax Member, MemberOrderAnalyzer.MemberCategory Category, int OriginalIndex)>();
            var excluded = new List<(MemberDeclarationSyntax Member, int OriginalIndex)>();

            for (var i = 0; i < members.Count; i++)
            {
                var category = MemberOrderAnalyzer.ClassifyMember(members[i]);
                if (category == MemberOrderAnalyzer.MemberCategory.Excluded)
                {
                    excluded.Add((members[i], i));
                }
                else
                {
                    categorized.Add((members[i], category, i));
                }
            }

            // カテゴリ順でソート（同一カテゴリ内は元の順序を維持）
            var sorted = categorized
                .OrderBy(x => (int)x.Category)
                .ThenBy(x => x.OriginalIndex)
                .Select(x => x.Member)
                .ToList();

            // Excludedメンバーを元の相対位置に再挿入
            // Excludedメンバーは末尾に追加する
            sorted.AddRange(excluded.OrderBy(x => x.OriginalIndex).Select(x => x.Member));

            var newMembers = SyntaxFactory.List(sorted);
            var newTypeDeclaration = typeDeclaration.WithMembers(newMembers);

            var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
