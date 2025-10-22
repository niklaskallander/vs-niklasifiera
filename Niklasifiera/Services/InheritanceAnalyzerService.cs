namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Service for analyzing type inheritance and interface implementation formatting.
/// </summary>
public class InheritanceAnalyzerService
    : IAnalyzerService
{
    public const string DiagnosticId =
        "NIKL002";

    private const string Category =
        "Formatting";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.InheritanceAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.InheritanceAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.InheritanceAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    public DiagnosticDescriptor Rule { get; } =
        new
        (
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description
        );

    public void InitializeAnalyzer(AnalysisContext context)
        => context
            .RegisterSyntaxNodeAction
            (
                Analyze,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.RecordStructDeclaration,
                SyntaxKind.InterfaceDeclaration
            );

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not TypeDeclarationSyntax typeDeclaration)
        {
            return;
        }

        if (typeDeclaration.BaseList is null)
        {
            return;
        }

        var baseList =
            typeDeclaration.BaseList;

        if (!ShouldReportDiagnostic(baseList, context.Options.AnalyzerConfigOptionsProvider))
        {
            return;
        }

        var diagnostic =
            Diagnostic
                .Create
                (
                    Rule,
                    baseList.GetLocation(),
                    typeDeclaration.Identifier.Text
                );

        context
            .ReportDiagnostic(diagnostic);
    }

    private bool ShouldReportDiagnostic
        (
        BaseListSyntax baseList,
        AnalyzerConfigOptionsProvider optionsProvider
        )
    {
        var sourceText =
            baseList.SyntaxTree
                .GetText();

        // Check formatting for both single and multiple inheritance/interfaces
        return !IsCorrectlyFormatted(baseList, sourceText, optionsProvider);
    }

    private bool IsCorrectlyFormatted
        (
        BaseListSyntax baseList,
        SourceText sourceText,
        AnalyzerConfigOptionsProvider optionsProvider
        )
    {
        if (baseList.Parent is not TypeDeclarationSyntax parentType)
        {
            return true; // Cannot validate if parent is not a type declaration
        }

        var colonLine =
            sourceText
                .GetLineNumberFor(baseList.ColonToken);

        var classLine =
            sourceText
                .GetLineNumberFor(parentType.Identifier);

        if (!IsColonPlacementCorrect(colonLine, classLine, parentType, sourceText))
        {
            return false;
        }

        if (!IsFirstBaseTypeFormattedCorrectly(baseList, sourceText, colonLine))
        {
            return false;
        }

        var classIndentation =
            sourceText.GetLineIndentationFor(classLine);

        var expectedColonIndentation =
            classIndentation + ConfigurationReader.GetIndentationUnit(baseList.SyntaxTree, optionsProvider);

        if (!IsColonIndentationCorrect(sourceText, colonLine, expectedColonIndentation))
        {
            return false;
        }

        return AreSubsequentBaseTypesFormattedCorrectly(baseList, sourceText, expectedColonIndentation);
    }

    private bool IsColonPlacementCorrect
        (
        int colonLine,
        int classLine,
        TypeDeclarationSyntax parentType,
        SourceText sourceText
        )
    {
        // RULE: Colon should be on its own line (not on same line as class)
        if (colonLine == classLine)
        {
            return false;
        }

        // RULE: If there's a primary constructor, colon should not be on same line as closing parenthesis
        if (parentType is ClassDeclarationSyntax classDeclaration &&
            classDeclaration.ParameterList is not null)
        {
            var primaryConstructorCloseLine =
                sourceText
                    .GetLineNumberFor(classDeclaration.ParameterList.CloseParenToken);

            if (colonLine == primaryConstructorCloseLine)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsFirstBaseTypeFormattedCorrectly
        (
        BaseListSyntax baseList,
        SourceText sourceText,
        int colonLine
        )
    {
        var baseTypes =
            baseList.Types;

        var firstBaseTypeLine =
            sourceText
                .GetLineNumberFor(baseTypes[0]);

        // RULE: First base type should be on the same line as the colon (with space after colon)
        if (firstBaseTypeLine != colonLine)
        {
            return false;
        }

        // RULE: Check spacing after colon - should have exactly one space
        var colonEndPosition =
            baseList.ColonToken.Span.End;

        var firstBaseTypeStartPosition =
            baseTypes[0].SpanStart;

        var textBetween =
            sourceText
                .ToString(TextSpan.FromBounds(colonEndPosition, firstBaseTypeStartPosition));

        return textBetween == " ";
    }

    private bool AreSubsequentBaseTypesFormattedCorrectly
        (
        BaseListSyntax baseList,
        SourceText sourceText,
        string expectedIndentation
        )
    {
        var baseTypes =
            baseList.Types;

        if (baseTypes.Count == 1)
        {
            return true; // Only one base type, no subsequent types to check
        }

        // RULE: Each subsequent base type should be on its own line with leading comma
        for (var i = 1; i < baseTypes.Count; i++)
        {
            if (!IsSubsequentBaseTypeFormattedCorrectly(baseList, sourceText, i, expectedIndentation))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsSubsequentBaseTypeFormattedCorrectly
        (
        BaseListSyntax baseList,
        SourceText sourceText,
        int index,
        string expectedIndentation
        )
    {
        var baseTypes =
            baseList.Types;

        var currentBaseTypeLine =
            sourceText
                .GetLineNumberFor(baseTypes[index]);

        var previousBaseTypeLine =
            sourceText
                .GetLineNumberFor(baseTypes[index - 1]);

        // Each base type should be on its own line
        if (currentBaseTypeLine <= previousBaseTypeLine)
        {
            return false;
        }

        var separator =
            baseList.Types
                .GetSeparator(index - 1);

        var separatorLine =
            sourceText
                .GetLineNumberFor(separator);

        // The comma should be on the same line as the current base type (leading comma)
        if (separatorLine != currentBaseTypeLine)
        {
            return false;
        }

        if (!IsCommaSpacingCorrect(separator, baseTypes[index], sourceText))
        {
            return false;
        }

        return IsCommaIndentationCorrect(sourceText, currentBaseTypeLine, expectedIndentation);
    }

    private bool IsCommaSpacingCorrect
        (
        SyntaxToken comma,
        BaseTypeSyntax baseType,
        SourceText sourceText
        )
    {
        // RULE: Check spacing after comma - should have exactly one space
        var commaEndPosition =
            comma.Span.End;

        var baseTypeStartPosition =
            baseType.SpanStart;

        var textAfterComma =
            sourceText
                .ToString(TextSpan.FromBounds(commaEndPosition, baseTypeStartPosition));

        return textAfterComma == " ";
    }

    private bool IsIndentationCorrect
        (
        SourceText sourceText,
        int lineNumber,
        string expectedIndentation
        )
    {
        var actualIndentation =
            sourceText
                .GetLineIndentationFor(lineNumber);

        return actualIndentation == expectedIndentation;
    }

    private bool IsColonIndentationCorrect
        (
        SourceText sourceText,
        int colonLine,
        string expectedIndentation
        )
        => IsIndentationCorrect(sourceText, colonLine, expectedIndentation);

    private bool IsCommaIndentationCorrect
        (
        SourceText sourceText,
        int commaLine,
        string expectedIndentation
        )
        => IsIndentationCorrect(sourceText, commaLine, expectedIndentation);
}
