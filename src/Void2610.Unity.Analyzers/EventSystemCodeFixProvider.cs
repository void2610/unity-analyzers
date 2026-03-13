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
using Microsoft.CodeAnalysis.Text;

namespace Void2610.Unity.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class EventSystemCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("VUA1002");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root?.FindNode(context.Diagnostics[0].Location.SourceSpan);
            if (node == null)
                return;

            if (node.FirstAncestorOrSelf<EventFieldDeclarationSyntax>() is { } eventField)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "R3.Subject に置き換える",
                        ct => ReplaceEventFieldAsync(context.Document, eventField.Span, ct),
                        nameof(EventSystemCodeFixProvider) + ".Event"),
                    context.Diagnostics);
                return;
            }

            if (node.FirstAncestorOrSelf<FieldDeclarationSyntax>() is { } fieldDeclaration)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "R3.Subject に置き換える",
                        ct => ReplaceFieldAsync(context.Document, fieldDeclaration.Span, ct),
                        nameof(EventSystemCodeFixProvider) + ".Field"),
                    context.Diagnostics);
                return;
            }

            if (node.FirstAncestorOrSelf<PropertyDeclarationSyntax>() is { } propertyDeclaration)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "R3.Subject に置き換える",
                        ct => ReplacePropertyAsync(context.Document, propertyDeclaration.Span, ct),
                        nameof(EventSystemCodeFixProvider) + ".Property"),
                    context.Diagnostics);
            }
        }

        private static async Task<Document> ReplaceEventFieldAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var eventField = root?.FindNode(span) as EventFieldDeclarationSyntax;
            if (eventField == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var subjectType = CreateSubjectTypeSyntax(eventField.Declaration.Type, semanticModel, preferUnit: true);

            var variableDeclaration = eventField.Declaration.WithType(subjectType);
            variableDeclaration = variableDeclaration.WithVariables(
                SyntaxFactory.SeparatedList(variableDeclaration.Variables.Select(EnsureNewInitializer)));

            var fieldDeclaration = SyntaxFactory.FieldDeclaration(variableDeclaration)
                .WithAttributeLists(eventField.AttributeLists)
                .WithModifiers(eventField.Modifiers)
                .WithLeadingTrivia(eventField.GetLeadingTrivia())
                .WithTrailingTrivia(eventField.GetTrailingTrivia());

            return document.WithSyntaxRoot(root.ReplaceNode(eventField, fieldDeclaration));
        }

        private static async Task<Document> ReplaceFieldAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var fieldDeclaration = root?.FindNode(span) as FieldDeclarationSyntax;
            if (fieldDeclaration == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var subjectType = CreateSubjectTypeSyntax(fieldDeclaration.Declaration.Type, semanticModel, preferUnit: false);

            var variableDeclaration = fieldDeclaration.Declaration.WithType(subjectType);
            variableDeclaration = variableDeclaration.WithVariables(
                SyntaxFactory.SeparatedList(variableDeclaration.Variables.Select(EnsureNewInitializer)));

            var newFieldDeclaration = fieldDeclaration.WithDeclaration(variableDeclaration);
            return document.WithSyntaxRoot(root.ReplaceNode(fieldDeclaration, newFieldDeclaration));
        }

        private static async Task<Document> ReplacePropertyAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var propertyDeclaration = root?.FindNode(span) as PropertyDeclarationSyntax;
            if (propertyDeclaration == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var subjectType = CreateSubjectTypeSyntax(propertyDeclaration.Type, semanticModel, preferUnit: false);

            var accessorList = SyntaxFactory.AccessorList(
                SyntaxFactory.SingletonList(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));

            var initializer = propertyDeclaration.Initializer ?? SyntaxFactory.EqualsValueClause(
                SyntaxFactory.ImplicitObjectCreationExpression());

            var newPropertyDeclaration = propertyDeclaration
                .WithType(subjectType)
                .WithAccessorList(accessorList)
                .WithExpressionBody(null)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .WithInitializer(initializer);

            return document.WithSyntaxRoot(root.ReplaceNode(propertyDeclaration, newPropertyDeclaration));
        }

        private static VariableDeclaratorSyntax EnsureNewInitializer(VariableDeclaratorSyntax variable)
        {
            return variable.Initializer != null
                ? variable.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ImplicitObjectCreationExpression()))
                : variable.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ImplicitObjectCreationExpression()));
        }

        private static TypeSyntax CreateSubjectTypeSyntax(TypeSyntax originalType, SemanticModel semanticModel, bool preferUnit)
        {
            var typeSymbol = semanticModel.GetTypeInfo(originalType).Type;
            var payloadType = GetPayloadType(typeSymbol, preferUnit);
            return SyntaxFactory.ParseTypeName($"R3.Subject<{payloadType}>")
                .WithTrailingTrivia(SyntaxFactory.Space);
        }

        private static string GetPayloadType(ITypeSymbol typeSymbol, bool preferUnit)
        {
            if (preferUnit)
                return "R3.Unit";

            if (typeSymbol is INamedTypeSymbol namedType &&
                namedType.ContainingNamespace?.ToDisplayString() == "System" &&
                namedType.Name == "Action")
            {
                if (namedType.TypeArguments.Length == 0)
                    return "R3.Unit";

                if (namedType.TypeArguments.Length == 1)
                    return ToTypeName(namedType.TypeArguments[0]);

                var tupleElements = string.Join(", ", namedType.TypeArguments.Select(ToTypeName));
                return $"({tupleElements})";
            }

            return "R3.Unit";
        }

        private static string ToTypeName(ITypeSymbol typeSymbol)
        {
            return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", string.Empty);
        }
    }
}
