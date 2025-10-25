namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Service for analyzing conditional operator (ternary if) formatting.
/// </summary>
public class ConditionalOperatorAnalyzerService
    : IAnalyzerService
{
    public const string DiagnosticId =
        "NIKL003";

    private const string Category =
        "Formatting";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ConditionalOperatorAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ConditionalOperatorAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ConditionalOperatorAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

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
                SyntaxKind.ConditionalExpression
            );

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        var conditionalExpression =
            (ConditionalExpressionSyntax)context.Node;

        if (!ShouldReportDiagnostic(conditionalExpression, context.Options.AnalyzerConfigOptionsProvider))
        {
            return;
        }

        var diagnostic =
            Diagnostic
                .Create
                (
                    Rule,
                    conditionalExpression.GetLocation(),
                    "conditional operator"
                );

        context
            .ReportDiagnostic(diagnostic);
    }

    private bool ShouldReportDiagnostic
        (
        ConditionalExpressionSyntax conditionalExpression,
        AnalyzerConfigOptionsProvider optionsProvider
        )
    {
        var sourceText =
            conditionalExpression.SyntaxTree
                .GetText();

        var firstLine =
            sourceText
                .GetLineNumberFor(conditionalExpression.GetFirstToken());

        var lastLine =
            sourceText
                .GetLineNumberFor(conditionalExpression.GetLastToken());

        var isSingleLine =
            firstLine == lastLine;

        if (isSingleLine)
        {
            return true; // Single-line is always a violation.
        }

        return !IsCorrectlyFormatted(conditionalExpression, sourceText, optionsProvider);
    }

    private bool IsCorrectlyFormatted
        (
        ConditionalExpressionSyntax conditionalExpression,
        SourceText sourceText,
        AnalyzerConfigOptionsProvider optionsProvider
        )
    {
        var context =
            new ConditionalFormattingContext
            (
                conditionalExpression,
                sourceText,
                ConfigurationReader.GetIndentationUnit(conditionalExpression.SyntaxTree, optionsProvider)
            );

        return ValidateFormatting(context);
    }

    private bool ValidateFormatting(ConditionalFormattingContext context)
    {
        // Rule: ? and : operators must be on separate lines after the condition
        // and indented one level from the line where the condition ends
        return context.IsProperlyFormatted();
    }

    /// <summary>
    /// Encapsulates conditional expression formatting context and validation logic.
    /// </summary>
    private sealed class ConditionalFormattingContext
        (
        ConditionalExpressionSyntax conditionalExpression,
        SourceText sourceText,
        string indentationUnit
        )
    {
        public SourceText SourceText { get; } = sourceText;

        public int ConditionEndLine { get; } =
            sourceText
                .GetLineNumberFor(conditionalExpression.Condition.GetLastToken());

        public int QuestionLine { get; } =
            sourceText
                .GetLineNumberFor(conditionalExpression.QuestionToken);

        public int WhenTrueLine { get; } =
            sourceText
                .GetLineNumberFor(conditionalExpression.WhenTrue.GetFirstToken());

        public int ColonLine { get; } =
            sourceText
                .GetLineNumberFor(conditionalExpression.ColonToken);

        private readonly string _indentationUnit = indentationUnit;

        /// <summary>
        /// Validates that the conditional expression is properly formatted:
        /// - ? and : operators must be on separate lines after the condition
        /// - Both operators must be indented one level from the condition's last line
        /// </summary>
        public bool IsProperlyFormatted()
        {
            // Rule 1: Operators must be on separate lines after condition
            if (QuestionLine <= ConditionEndLine || ColonLine <= WhenTrueLine)
            {
                return false;
            }

            // Rule 2: Both operators must have same indentation (one level from condition)
            var conditionIndentation =
                SourceText
                    .GetLineIndentationFor(ConditionEndLine);

            var expectedOperatorIndentation =
                conditionIndentation + _indentationUnit;

            var questionIndentation =
                SourceText
                    .GetLineIndentationFor(QuestionLine);

            var colonIndentation =
                SourceText
                    .GetLineIndentationFor(ColonLine);

            return questionIndentation == expectedOperatorIndentation
                && colonIndentation == expectedOperatorIndentation;
        }
    }
}
