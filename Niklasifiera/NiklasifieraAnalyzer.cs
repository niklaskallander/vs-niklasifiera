namespace Niklasifiera;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Immutable;
using System.Threading;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NiklasifieraAnalyzer
    : DiagnosticAnalyzer
{
    public const string SignatureDiagnosticId =
        "NIKL001";

    public const string InheritanceDiagnosticId =
        "NIKL002";

    private static readonly LocalizableString SignatureTitle = new LocalizableResourceString(nameof(Resources.SignatureAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString SignatureMessageFormat = new LocalizableResourceString(nameof(Resources.SignatureAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString SignatureDescription = new LocalizableResourceString(nameof(Resources.SignatureAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString InheritanceTitle = new LocalizableResourceString(nameof(Resources.InheritanceAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString InheritanceMessageFormat = new LocalizableResourceString(nameof(Resources.InheritanceAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString InheritanceDescription = new LocalizableResourceString(nameof(Resources.InheritanceAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category =
        "Formatting";

    private static readonly DiagnosticDescriptor SignatureRule =
        new
        (
            SignatureDiagnosticId,
            SignatureTitle,
            SignatureMessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: SignatureDescription
        );

    private static readonly DiagnosticDescriptor InheritanceRule =
        new
        (
            InheritanceDiagnosticId,
            InheritanceTitle,
            InheritanceMessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: InheritanceDescription
        );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [SignatureRule, InheritanceRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeConstructorDeclaration, SyntaxKind.ConstructorDeclaration);

        // Handle primary constructors by analyzing all type declarations
        context
            .RegisterSyntaxNodeAction
            (
                AnalyzeTypeDeclaration,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.RecordStructDeclaration,
                SyntaxKind.InterfaceDeclaration
            );
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration =
            (MethodDeclarationSyntax)context.Node;

        var parameterList =
            methodDeclaration.ParameterList;

        if (!ShouldReportDiagnostic(parameterList, methodDeclaration, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        var diagnostic =
            Diagnostic
                .Create
                (
                    SignatureRule,
                    parameterList.GetLocation(),
                    methodDeclaration.Identifier.Text
                );

        context
            .ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context)
    {
        var constructorDeclaration =
            (ConstructorDeclarationSyntax)context.Node;

        var parameterList =
            constructorDeclaration.ParameterList;

        if (!ShouldReportDiagnostic(parameterList, constructorDeclaration, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        var diagnostic =
            Diagnostic
                .Create
                (
                    SignatureRule,
                    parameterList.GetLocation(),
                    constructorDeclaration.Identifier.Text
                );

        context
            .ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration =
            (TypeDeclarationSyntax)context.Node;

        // Check for primary constructor (C# 12+) - now with direct property access
        if (typeDeclaration.ParameterList is not null)
        {
            var parameterList =
                typeDeclaration.ParameterList;

            if (ShouldReportDiagnostic(parameterList, typeDeclaration, context.SemanticModel, context.CancellationToken))
            {
                var diagnostic =
                    Diagnostic
                        .Create
                        (
                            SignatureRule,
                            parameterList.GetLocation(),
                            typeDeclaration.Identifier.Text
                        );

                context
                    .ReportDiagnostic(diagnostic);
            }
        }

        // Check for inheritance/interface implementation formatting
        if (typeDeclaration.BaseList is not null)
        {
            var baseList =
                typeDeclaration.BaseList;

            if (ShouldReportInheritanceDiagnostic(baseList, typeDeclaration))
            {
                var diagnostic =
                    Diagnostic
                        .Create
                        (
                            InheritanceRule,
                            baseList.GetLocation(),
                            typeDeclaration.Identifier.Text
                        );

                context
                    .ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool ShouldReportDiagnostic
        (
        ParameterListSyntax parameterList,
        SyntaxNode parentDeclaration,
        SemanticModel _,
        CancellationToken __
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
        return !IsCorrectlyFormatted(parameterList, parentDeclaration);
    }

    private static bool IsIncorrectlySplit(ParameterListSyntax _)
        // For 0-1 parameters, they can be on a single line, but if split, that's also ok
        // We'll be lenient here and not flag 0-1 parameter splits as errors
        => false;

    private static bool IsCorrectlyFormatted
        (
        ParameterListSyntax parameterList,
        SyntaxNode parentDeclaration
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

        // Find the line where the declaration starts (could be method name, constructor name, or class name for primary constructor)
        var declarationStartLine =
            GetDeclarationStartLine(parentDeclaration, sourceText);

        // Check if opening paren is on a different line from the declaration start
        var openParenLine =
            sourceText.Lines
                .GetLineFromPosition(parameterList.OpenParenToken.SpanStart)
                .LineNumber;

        // For correctly formatted signatures, the opening paren should be on the line after the declaration
        if (openParenLine <= declarationStartLine)
        {
            return false;
        }

        // Get the indentation of the declaration and calculate expected parameter indentation
        var declarationIndentation =
            GetLineIndentation(sourceText.Lines[declarationStartLine].ToString());

        var expectedParameterIndentation =
            declarationIndentation + GetIndentationUnit(sourceText, parentDeclaration);

        // Check opening parenthesis indentation
        var openParenIndentation =
            GetLineIndentation(sourceText.Lines[openParenLine].ToString());

        if (openParenIndentation != expectedParameterIndentation)
        {
            return false;
        }

        // Check if all parameters are on separate lines and properly indented
        var previousLine =
            openParenLine;

        foreach (var parameter in parameters)
        {
            var parameterLine =
                sourceText.Lines
                    .GetLineFromPosition(parameter.SpanStart)
                    .LineNumber;

            // Each parameter should be on a different line from the previous one
            if (parameterLine <= previousLine)
            {
                return false;
            }

            // Check parameter indentation - should align with opening parenthesis
            var parameterIndentation =
                GetLineIndentation(sourceText.Lines[parameterLine].ToString());

            if (parameterIndentation != expectedParameterIndentation)
            {
                return false;
            }

            previousLine =
                parameterLine;
        }

        // Check if closing paren is on its own line after the last parameter
        var closeParenLine =
            sourceText.Lines
                .GetLineFromPosition(parameterList.CloseParenToken.SpanStart)
                .LineNumber;

        var lastParameterLine =
            sourceText.Lines
                .GetLineFromPosition(parameters.Last().Span.End)
                .LineNumber;

        if (closeParenLine <= lastParameterLine)
        {
            return false;
        }

        // Check closing parenthesis indentation - should align with opening parenthesis
        var closeParenIndentation =
            GetLineIndentation(sourceText.Lines[closeParenLine].ToString());

        if (closeParenIndentation != expectedParameterIndentation)
        {
            return false;
        }

        // Check opening and closing brace placement and indentation
        return AreBracesCorrectlyFormatted(parentDeclaration, parameterList, declarationIndentation, sourceText);
    }

    private static bool AreBracesCorrectlyFormatted
        (
        SyntaxNode parentDeclaration,
        ParameterListSyntax parameterList,
        string declarationIndentation,
        SourceText sourceText
        )
    {
        // Find the opening and closing brace tokens
        SyntaxToken? openBraceToken = null;
        SyntaxToken? closeBraceToken = null;

        switch (parentDeclaration)
        {
            case MethodDeclarationSyntax method:
                openBraceToken = method.Body?.OpenBraceToken;
                closeBraceToken = method.Body?.CloseBraceToken;
                break;

            case ConstructorDeclarationSyntax constructor:
                openBraceToken = constructor.Body?.OpenBraceToken;
                closeBraceToken = constructor.Body?.CloseBraceToken;
                break;

            case TypeDeclarationSyntax type:
                // For primary constructors, the braces are part of the type declaration
                openBraceToken = type.OpenBraceToken;
                closeBraceToken = type.CloseBraceToken;
                break;
        }

        if (!openBraceToken.HasValue ||
            openBraceToken.Value.IsKind(SyntaxKind.None))
        {
            // No body brace found, this is valid (e.g., abstract method, expression body)
            return true;
        }

        var openBraceLine =
            sourceText.Lines
                .GetLineFromPosition(openBraceToken.Value.SpanStart)
                .LineNumber;

        var closeParenLine =
            sourceText.Lines
                .GetLineFromPosition(parameterList.CloseParenToken.SpanStart)
                .LineNumber;

        // Rule: Opening brace must be on its own line after the closing parenthesis
        if (openBraceLine <= closeParenLine)
        {
            return false;
        }

        // Rule: Opening brace must be indented at the same level as the declaration (not the parameters)
        var openBraceIndentation =
            GetLineIndentation(sourceText.Lines[openBraceLine].ToString());

        if (openBraceIndentation != declarationIndentation)
        {
            return false;
        }

        // Rule: Closing brace must also be indented at the same level as the declaration
        if (closeBraceToken.HasValue &&
            !closeBraceToken.Value.IsKind(SyntaxKind.None))
        {
            var closeBraceLine =
                sourceText.Lines
                    .GetLineFromPosition(closeBraceToken.Value.SpanStart)
                    .LineNumber;

            // Rule: Closing brace must be on its own line after the opening brace
            if (closeBraceLine <= openBraceLine)
            {
                return false;
            }

            // Rule: Closing brace must be indented at the same level as the declaration (same as opening brace)
            var closeBraceIndentation =
                GetLineIndentation(sourceText.Lines[closeBraceLine].ToString());

            if (closeBraceIndentation != declarationIndentation)
            {
                return false;
            }
        }

        return true;
    }

    private static int GetDeclarationStartLine
        (
        SyntaxNode parentDeclaration,
        SourceText sourceText
        )
        => parentDeclaration switch
        {
            MethodDeclarationSyntax method
                // For methods, the declaration starts at the method identifier (after return type, modifiers, generics)
                => sourceText.Lines
                    .GetLineFromPosition(method.Identifier.SpanStart)
                    .LineNumber,

            ConstructorDeclarationSyntax constructor
                // For constructors, the declaration starts at the constructor identifier
                => sourceText.Lines
                    .GetLineFromPosition(constructor.Identifier.SpanStart)
                    .LineNumber,

            TypeDeclarationSyntax type
                // For primary constructors, the declaration starts at the type identifier
                => sourceText.Lines
                    .GetLineFromPosition(type.Identifier.SpanStart)
                    .LineNumber,
            _
                // Fallback: use the start of the parent declaration
                => sourceText.Lines
                    .GetLineFromPosition(parentDeclaration.SpanStart)
                    .LineNumber,
        };

    private static bool ShouldReportInheritanceDiagnostic
        (
        BaseListSyntax baseList,
        TypeDeclarationSyntax _
        )
    {
        var sourceText =
            baseList.SyntaxTree
                .GetText();

        // Check formatting for both single and multiple inheritance/interfaces
        return !IsInheritanceCorrectlyFormatted(baseList, sourceText);
    }

    private static bool IsInheritanceCorrectlyFormatted
        (
        BaseListSyntax baseList,
        SourceText sourceText
        )
    {
        var baseTypes =
            baseList.Types;

        // Get the line of the colon and the class declaration
        var colonLine =
            sourceText.Lines
                .GetLineFromPosition(baseList.ColonToken.SpanStart)
                .LineNumber;

        // Get the line of the class identifier (to check if colon is on same line as class)
        var parentType =
            baseList.Parent as TypeDeclarationSyntax;

        var classLine =
            sourceText.Lines
                .GetLineFromPosition(parentType.Identifier.SpanStart)
                .LineNumber;

        // RULE: Colon should be on its own line (not on same line as class)
        if (colonLine == classLine)
        {
            return false;
        }

        // RULE: If there's a primary constructor, colon should not be on same line as closing parenthesis
        if (parentType is ClassDeclarationSyntax classDecl &&
            classDecl.ParameterList is not null)
        {
            var primaryConstructorCloseLine =
                sourceText.Lines
                    .GetLineFromPosition(classDecl.ParameterList.CloseParenToken.SpanStart)
                    .LineNumber;

            if (colonLine == primaryConstructorCloseLine)
            {
                return false;
            }
        }

        // Get the line of the first base type
        var firstBaseTypeLine =
            sourceText.Lines
                .GetLineFromPosition(baseTypes[0].SpanStart)
                .LineNumber;

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

        if (textBetween != " ")
        {
            return false;
        }

        // RULE: Check colon indentation - should be exactly one level deeper than class
        var classIndentation =
            GetLineIndentation(sourceText.Lines[classLine].ToString());

        var colonIndentation =
            GetLineIndentation(sourceText.Lines[colonLine].ToString());

        var expectedColonIndentation =
            classIndentation + GetIndentationUnit(sourceText, parentType);

        if (colonIndentation != expectedColonIndentation)
        {
            return false;
        }

        // For single inheritance/interface, check formatting rules
        if (baseTypes.Count == 1)
        {
            // Check all the same rules as multiple inheritance for the first base type
            // The logic after this handles the first base type validation already
            // So just continue to the shared validation logic
        }

        // RULE: Each subsequent base type should be on its own line with leading comma
        for (var i = 1; i < baseTypes.Count; i++)
        {
            var currentBaseTypeLine =
                sourceText.Lines
                    .GetLineFromPosition(baseTypes[i].SpanStart)
                    .LineNumber;

            var previousBaseTypeLine =
                sourceText.Lines
                    .GetLineFromPosition(baseTypes[i - 1].SpanStart)
                    .LineNumber;

            // Each base type should be on its own line
            if (currentBaseTypeLine <= previousBaseTypeLine)
            {
                return false;
            }

            // Check if the separator comma is at the beginning of the line (leading comma style)
            var separator =
                baseList.Types
                    .GetSeparator(i - 1);

            var separatorLine =
                sourceText.Lines
                    .GetLineFromPosition(separator.SpanStart)
                    .LineNumber;

            // The comma should be on the same line as the current base type (leading comma)
            if (separatorLine != currentBaseTypeLine)
            {
                return false;
            }

            // RULE: Check spacing after comma - should have exactly one space
            var commaEndPosition =
                separator.Span.End;

            var currentBaseTypeStartPosition =
                baseTypes[i].SpanStart;

            var textAfterComma =
                sourceText
                    .ToString(TextSpan.FromBounds(commaEndPosition, currentBaseTypeStartPosition));

            if (textAfterComma != " ")
            {
                return false;
            }

            // RULE: Check comma indentation - should align with colon
            var commaIndentation =
                GetLineIndentation(sourceText.Lines[currentBaseTypeLine].ToString());

            if (commaIndentation != expectedColonIndentation)
            {
                return false;
            }
        }

        return true;
    }

    private static string GetLineIndentation(string line)
    {
        var indentation = "";

        foreach (var c in line)
        {
            if (c is ' ' or '\t')
            {
                indentation += c;
            }
            else
            {
                break;
            }
        }

        return indentation;
    }

    private static string GetIndentationUnit
        (
        SourceText sourceText,
        SyntaxNode parentType
        )
    {
        // Try to detect the indentation style from the file
        // Look for existing indentation patterns in the syntax tree

        // First, try to find indentation from the parent type's members
        if (parentType is TypeDeclarationSyntax typeDecl)
        {
            var members =
                typeDecl.Members;

            if (members.Count > 0)
            {
                var firstMember =
                    members[0];

                var memberLine =
                    sourceText.Lines
                        .GetLineFromPosition(firstMember.SpanStart);

                var typeLine =
                    sourceText.Lines
                        .GetLineFromPosition(typeDecl.Identifier.SpanStart);

                var memberIndentation =
                    GetLineIndentation(memberLine.ToString());

                var typeIndentation =
                    GetLineIndentation(typeLine.ToString());

                if (memberIndentation.Length > typeIndentation.Length)
                {
                    return memberIndentation
                        .Substring(typeIndentation.Length);
                }
            }
        }

        // Fallback: detect tabs vs spaces from the source
        var sourceString =
            sourceText
                .ToString();

        if (sourceString.Contains("\t"))
        {
            return "\t"; // Use tab if file contains tabs
        }

        // Default to 4 spaces if we can't detect
        return "    ";
    }
}
