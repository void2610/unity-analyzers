using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Void2610.Unity.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EnumSummaryAnalyzer : DiagnosticAnalyzer
    {
        // トップレベルenumメンバーにはXMLドキュメントコメントを必須とする
        public static readonly DiagnosticDescriptor VUA4001 = new DiagnosticDescriptor(
            "VUA4001",
            "トップレベルenumメンバーには/// <summary>コメントが必要です",
            "enumメンバー '{0}' に/// <summary>コメントを追加してください",
            "Documentation",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(VUA4001);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeEnumMember, SyntaxKind.EnumMemberDeclaration);
        }

        private static void AnalyzeEnumMember(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeHelper.IsGenerated(context.Node.SyntaxTree)) return;
            var enumMember = (EnumMemberDeclarationSyntax)context.Node;

            // 親がトップレベルenumかチェック（クラス内ネストは除外）
            if (enumMember.Parent is not EnumDeclarationSyntax enumDecl)
                return;
            if (enumDecl.Parent is TypeDeclarationSyntax)
                return;

            // XMLドキュメントコメントの有無をチェック
            // DocumentationModeによっては///がSingleLineCommentTriviaとして解析されるため両方チェック
            var hasDocComment = enumMember.GetLeadingTrivia()
                .Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                    || (t.IsKind(SyntaxKind.SingleLineCommentTrivia)
                        && t.ToString().StartsWith("///")
                        && t.ToString().Contains("<summary>")));

            if (!hasDocComment)
            {
                var diagnostic = Diagnostic.Create(
                    VUA4001,
                    enumMember.Identifier.GetLocation(),
                    enumMember.Identifier.Text);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
