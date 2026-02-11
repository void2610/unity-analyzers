using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Void2610.Unity.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MotionHandleTryCancelAnalyzer : DiagnosticAnalyzer
    {
        // if(handle.IsActive()) handle.Cancel() パターンを検出し、TryCancelの使用を推奨
        public static readonly DiagnosticDescriptor VUA0007 = new DiagnosticDescriptor(
            "VUA0007",
            "MotionHandleにはTryCancel()を使用してください",
            "'{0}' に対して if(IsActive()) Cancel() ではなく TryCancel() を使用してください",
            "Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(VUA0007);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        }

        private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            // elseがある場合は対象外
            if (ifStatement.Else != null)
                return;

            // 条件部: xxx.IsActive() の形式かチェック
            if (!TryGetMemberAccessTarget(ifStatement.Condition, "IsActive", out var conditionTarget))
                return;

            // 本体: xxx.Cancel() の形式かチェック
            var bodyStatement = GetSingleStatement(ifStatement.Statement);
            if (bodyStatement == null)
                return;

            if (!(bodyStatement is ExpressionStatementSyntax exprStatement))
                return;

            if (!TryGetMemberAccessTarget(exprStatement.Expression, "Cancel", out var bodyTarget))
                return;

            // 同じオブジェクトに対する呼び出しかチェック
            if (conditionTarget.ToString() != bodyTarget.ToString())
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                VUA0007,
                ifStatement.GetLocation(),
                conditionTarget.ToString()));
        }

        // メンバーアクセス呼び出し（target.methodName()）からtargetを取得
        private static bool TryGetMemberAccessTarget(
            ExpressionSyntax expression, string methodName, out ExpressionSyntax target)
        {
            target = null;

            // 呼び出し式かチェック
            if (!(expression is InvocationExpressionSyntax invocation))
                return false;

            // メンバーアクセスかチェック
            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
                return false;

            // メソッド名が一致するかチェック
            if (memberAccess.Name.Identifier.Text != methodName)
                return false;

            // 引数なしかチェック
            if (invocation.ArgumentList.Arguments.Count != 0)
                return false;

            target = memberAccess.Expression;
            return true;
        }

        // if本体から単一のステートメントを取得
        private static StatementSyntax GetSingleStatement(StatementSyntax statement)
        {
            if (statement is BlockSyntax block)
            {
                return block.Statements.Count == 1 ? block.Statements[0] : null;
            }
            return statement;
        }
    }
}
