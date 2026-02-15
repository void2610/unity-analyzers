using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Void2610.Unity.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EventSystemAnalyzer : DiagnosticAnalyzer
    {
        // C#標準のeventやAction/Funcフィールドの代わりにR3のSubjectを使用するよう警告
        public static readonly DiagnosticDescriptor VUA0003 = new DiagnosticDescriptor(
            "VUA0003",
            "イベントにはR3のSubjectを使用してください",
            "'{0}' はC#標準のイベント/デリゲートです。R3のSubject<T>を使用してください",
            "Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(VUA0003);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
            context.RegisterSymbolAction(AnalyzeEvent, SymbolKind.Event);
        }

        private static void AnalyzeEvent(SymbolAnalysisContext context)
        {
            if (GeneratedCodeHelper.IsGenerated(context.Symbol)) return;
            var eventSymbol = (IEventSymbol)context.Symbol;

            // コンパイラ生成は除外
            if (eventSymbol.IsImplicitlyDeclared)
                return;

            context.ReportDiagnostic(
                Diagnostic.Create(VUA0003, eventSymbol.Locations[0], eventSymbol.Name));
        }

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            if (GeneratedCodeHelper.IsGenerated(context.Symbol)) return;
            var field = (IFieldSymbol)context.Symbol;

            // コンパイラ生成フィールドは除外（eventのバッキングフィールドなど）
            if (field.IsImplicitlyDeclared)
                return;

            if (IsActionOrFuncType(field.Type))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(VUA0003, field.Locations[0], field.Name));
            }
        }

        private static void AnalyzeProperty(SymbolAnalysisContext context)
        {
            if (GeneratedCodeHelper.IsGenerated(context.Symbol)) return;
            var property = (IPropertySymbol)context.Symbol;

            if (IsActionOrFuncType(property.Type))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(VUA0003, property.Locations[0], property.Name));
            }
        }

        private static bool IsActionOrFuncType(ITypeSymbol type)
        {
            if (type == null)
                return false;

            var name = type.Name;
            var ns = type.ContainingNamespace?.ToDisplayString();

            // System.Action, System.Action<T>, System.Func<T> を検出
            return ns == "System" && (name == "Action" || name == "Func");
        }
    }
}
