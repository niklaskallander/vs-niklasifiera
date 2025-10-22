namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Service for analyzing method and constructor signature formatting.
/// </summary>
public class SignatureAnalyzerService
    : IAnalyzerService
{
    public const string DiagnosticId =
        "NIKL001";

    private const string Category =
        "Formatting";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.SignatureAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.SignatureAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.SignatureAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

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
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ConstructorDeclaration);
        context
            .RegisterSyntaxNodeAction
            (
                Analyze,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.RecordStructDeclaration
            );
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        var (parameterList, parentDeclaration, identifierText) = context.Node switch
        {
            MethodDeclarationSyntax method
                => (method.ParameterList, method, method.Identifier.Text),

            ConstructorDeclarationSyntax constructor
                => (constructor.ParameterList, constructor, constructor.Identifier.Text),

            TypeDeclarationSyntax type when type.ParameterList is not null
                => (type.ParameterList, (SyntaxNode)type, type.Identifier.Text),

            _ => (null, null, null)
        };

        if (parameterList is null || parentDeclaration is null)
        {
            return;
        }

        if (!ShouldReportDiagnostic(parameterList, parentDeclaration, context.Options.AnalyzerConfigOptionsProvider))
        {
            return;
        }

        var diagnostic =
            Diagnostic
                .Create
                (
                    Rule,
                    parameterList.GetLocation(),
                    identifierText
                );

        context
            .ReportDiagnostic(diagnostic);
    }

    private bool ShouldReportDiagnostic
        (
        ParameterListSyntax parameterList,
        SyntaxNode parentDeclaration,
        AnalyzerConfigOptionsProvider optionsProvider
        )
    {
        if (parameterList == null)
        {
            return false;
        }

        var parameters =
            parameterList.Parameters;

        var parameterCount =
            parameters.Count;

        // Rule: 0-1 parameters can be on a single line, 2+ must be split
        if (parameterCount is 0 or 1)
        {
            // For 0-1 parameters, check if it's incorrectly split
            return IsIncorrectlySplit(parameterList);
        }

        // 2 or more parameters
        // Must be split across multiple lines
        return !IsCorrectlyFormatted(parameterList, parentDeclaration, optionsProvider);
    }

    private bool IsIncorrectlySplit(ParameterListSyntax _)
        // For 0-1 parameters, they can be on a single line, but if split, that's also ok
        // We'll be lenient here and not flag 0-1 parameter splits as errors
        => false;

    private bool IsCorrectlyFormatted
        (
        ParameterListSyntax parameterList,
        SyntaxNode parentDeclaration,
        AnalyzerConfigOptionsProvider optionsProvider
        )
    {
        var parameters =
            parameterList.Parameters;

        if (parameters.Count < 2)
        {
            return true;
        }

        var sourceText =
            parameterList.SyntaxTree
                .GetText();

        var declarationStartLine =
            sourceText
                .GetDeclarationStartLine(parentDeclaration);

        var declarationIndentation =
            sourceText
                .GetLineIndentationFor(declarationStartLine);

        var indentationUnit =
            ConfigurationReader
                .GetIndentationUnit(parentDeclaration.SyntaxTree, optionsProvider);

        var expectedParameterIndentation =
            declarationIndentation + indentationUnit;

        if (!IsOpeningParenthesisCorrectlyFormatted(parameterList, sourceText, declarationStartLine, expectedParameterIndentation))
        {
            return false;
        }

        if (!AreParametersCorrectlyFormatted(parameterList, sourceText, expectedParameterIndentation))
        {
            return false;
        }

        if (!IsClosingParenthesisCorrectlyFormatted(parameterList, sourceText, expectedParameterIndentation))
        {
            return false;
        }

        return AreBracesCorrectlyFormatted(parentDeclaration, parameterList, declarationIndentation, sourceText);
    }

    private bool IsOpeningParenthesisCorrectlyFormatted
        (
        ParameterListSyntax parameterList,
        SourceText sourceText,
        int declarationStartLine,
        string expectedIndentation
        )
    {
        var openParenLine =
            sourceText
                .GetLineNumberFor(parameterList.OpenParenToken);

        // For correctly formatted signatures, the opening paren should be on the line after the declaration
        if (openParenLine <= declarationStartLine)
        {
            return false;
        }

        var openParenIndentation =
            sourceText
                .GetLineIndentationFor(openParenLine);

        return openParenIndentation == expectedIndentation;
    }

    private bool AreParametersCorrectlyFormatted
        (
        ParameterListSyntax parameterList,
        SourceText sourceText,
        string expectedIndentation
        )
    {
        var parameters =
            parameterList.Parameters;

        var openParenLine =
            sourceText
                .GetLineNumberFor(parameterList.OpenParenToken);

        var previousLine =
            openParenLine;

        foreach (var parameter in parameters)
        {
            if (!IsParameterCorrectlyFormatted(parameter, sourceText, previousLine, expectedIndentation))
            {
                return false;
            }

            previousLine =
                sourceText
                    .GetLineNumberFor(parameter);
        }

        return true;
    }

    private bool IsParameterCorrectlyFormatted
        (
        ParameterSyntax parameter,
        SourceText sourceText,
        int previousLine,
        string expectedIndentation
        )
    {
        var parameterLine =
            sourceText
                .GetLineNumberFor(parameter);

        // Each parameter should be on a different line from the previous one
        if (parameterLine <= previousLine)
        {
            return false;
        }

        // Check parameter indentation - should align with opening parenthesis
        var parameterIndentation =
            sourceText
                .GetLineIndentationFor(parameterLine);

        return parameterIndentation == expectedIndentation;
    }

    private bool IsClosingParenthesisCorrectlyFormatted
        (
        ParameterListSyntax parameterList,
        SourceText sourceText,
        string expectedIndentation
        )
    {
        var parameters =
            parameterList.Parameters;

        var closeParenLine =
            sourceText
                .GetLineNumberFor(parameterList.CloseParenToken);

        var lastParameterLine =
            sourceText
                .GetLineNumberFor(parameters.Last().Span.End);

        // Closing paren must be on its own line after the last parameter
        if (closeParenLine <= lastParameterLine)
        {
            return false;
        }

        // Check closing parenthesis indentation - should align with opening parenthesis
        var closeParenIndentation =
            sourceText
                .GetLineIndentationFor(closeParenLine);

        return closeParenIndentation == expectedIndentation;
    }

    private bool AreBracesCorrectlyFormatted
        (
        SyntaxNode parentDeclaration,
        ParameterListSyntax parameterList,
        string declarationIndentation,
        SourceText sourceText
        )
    {
        var (openBraceToken, closeBraceToken) =
            GetBraceTokens(parentDeclaration);

        if (!openBraceToken.HasValue ||
            openBraceToken.Value.IsKind(SyntaxKind.None))
        {
            // No body brace found, this is valid (e.g., abstract method, expression body)
            return true;
        }

        if (!IsOpeningBraceCorrectlyFormatted(openBraceToken.Value, parameterList, declarationIndentation, sourceText))
        {
            return false;
        }

        if (!closeBraceToken.HasValue ||
            closeBraceToken.Value.IsKind(SyntaxKind.None))
        {
            return true;
        }

        return IsClosingBraceCorrectlyFormatted(closeBraceToken.Value, openBraceToken.Value, declarationIndentation, sourceText);
    }

    private (SyntaxToken? openBrace, SyntaxToken? closeBrace) GetBraceTokens(SyntaxNode parentDeclaration)
        => parentDeclaration switch
        {
            MethodDeclarationSyntax method
                => (method.Body?.OpenBraceToken, method.Body?.CloseBraceToken),

            ConstructorDeclarationSyntax constructor
                => (constructor.Body?.OpenBraceToken, constructor.Body?.CloseBraceToken),

            TypeDeclarationSyntax type
                => (type.OpenBraceToken, type.CloseBraceToken),

            _ => (null, null)
        };

    private bool IsOpeningBraceCorrectlyFormatted
        (
        SyntaxToken openBraceToken,
        ParameterListSyntax parameterList,
        string declarationIndentation,
        SourceText sourceText
        )
        => IsBraceCorrectlyFormatted
        (
            openBraceToken,
            parameterList.CloseParenToken,
            declarationIndentation,
            sourceText
        );

    private bool IsClosingBraceCorrectlyFormatted
        (
        SyntaxToken closeBraceToken,
        SyntaxToken openBraceToken,
        string declarationIndentation,
        SourceText sourceText
        )
        => IsBraceCorrectlyFormatted
        (
            closeBraceToken,
            openBraceToken,
            declarationIndentation,
            sourceText
        );

    private bool IsBraceCorrectlyFormatted
        (
        SyntaxToken braceToken,
        SyntaxToken precedingToken,
        string expectedIndentation,
        SourceText sourceText
        )
    {
        var braceLine =
            sourceText
                .GetLineNumberFor(braceToken);

        var precedingLine =
            sourceText
                .GetLineNumberFor(precedingToken);

        // Rule: Brace must be on its own line after the preceding token
        if (braceLine <= precedingLine)
        {
            return false;
        }

        // Rule: Brace must be indented at the expected level
        var braceIndentation =
            sourceText
                .GetLineIndentationFor(braceLine);

        return braceIndentation == expectedIndentation;
    }
}
