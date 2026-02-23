using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Void2610.Unity.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class StartCoroutineAnalyzer : DiagnosticAnalyzer
    {
        // コルーチン(StartCoroutine)の使用を禁止し、UniTaskなどの代替を推奨
        public static readonly DiagnosticDescriptor VUA0009 = new DiagnosticDescriptor(
            "VUA0009",
            "コルーチンの使用は禁止されています",
            "StartCoroutine の呼び出しを削除し、UniTask などの代替手段を使用してください",
            "Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(VUA0009);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeHelper.IsGenerated(context.Node.SyntaxTree)) return;
            var invocation = (InvocationExpressionSyntax)context.Node;

            string methodName = null;

            // target.StartCoroutine(...) の形式
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                methodName = memberAccess.Name.Identifier.Text;
            }
            // StartCoroutine(...) の形式（thisを省略した呼び出し）
            else if (invocation.Expression is IdentifierNameSyntax identifier)
            {
                methodName = identifier.Identifier.Text;
            }

            if (methodName == "StartCoroutine")
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    VUA0009,
                    invocation.GetLocation()));
            }
        }
    }
}
