using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Void2610.Unity.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SerializeFieldNamingAnalyzer : DiagnosticAnalyzer
    {
        // 通常のprivateフィールドに_プレフィックスがない場合の警告
        public static readonly DiagnosticDescriptor VUA0008 = new DiagnosticDescriptor(
            "VUA0008",
            "privateフィールドには'_'プレフィックスが必要です",
            "privateフィールド '{0}' には '_' プレフィックスを付けてください",
            "Naming",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // [SerializeField]付きフィールドに_プレフィックスがある場合の警告
        public static readonly DiagnosticDescriptor VUA0002 = new DiagnosticDescriptor(
            "VUA0002",
            "[SerializeField]フィールドには'_'プレフィックスを付けないでください",
            "[SerializeField]フィールド '{0}' から '_' プレフィックスを除去してください",
            "Naming",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(VUA0008, VUA0002);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        }

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            if (GeneratedCodeHelper.IsGenerated(context.Symbol)) return;
            var field = (IFieldSymbol)context.Symbol;

            // privateフィールドのみ対象
            if (field.DeclaredAccessibility != Accessibility.Private)
                return;

            // const, static, コンパイラ生成フィールドは除外
            if (field.IsConst || field.IsStatic || field.IsImplicitlyDeclared)
                return;

            var hasSerializeField = field.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "SerializeField" ||
                a.AttributeClass?.Name == "SerializeFieldAttribute");

            var startsWithUnderscore = field.Name.StartsWith("_");

            if (hasSerializeField)
            {
                // [SerializeField]付き → _プレフィックス不要
                if (startsWithUnderscore)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(VUA0002, field.Locations[0], field.Name));
                }
            }
            else
            {
                // 通常のprivateフィールド → _プレフィックス必須
                if (!startsWithUnderscore)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(VUA0008, field.Locations[0], field.Name));
                }
            }
        }
    }
}
