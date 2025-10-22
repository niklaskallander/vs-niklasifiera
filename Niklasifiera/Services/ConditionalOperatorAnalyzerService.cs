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

        var isAssignment =
            IsPartOfAssignment(conditionalExpression);

        return !IsCorrectlyFormatted(conditionalExpression, sourceText, optionsProvider, isAssignment);
    }

    private bool IsPartOfAssignment(ConditionalExpressionSyntax conditionalExpression)
    {
        var parent =
            conditionalExpression.Parent;

        // Check for direct assignment
        if (parent is EqualsValueClauseSyntax equalsValue)
        {
            return equalsValue.Parent is VariableDeclaratorSyntax
                || equalsValue.Parent?.Parent is AssignmentExpressionSyntax;
        }

        // Check for assignment expression
        if (parent is AssignmentExpressionSyntax)
        {
            return true;
        }

        return false;
    }

    private bool IsCorrectlyFormatted
        (
        ConditionalExpressionSyntax conditionalExpression,
        SourceText sourceText,
        AnalyzerConfigOptionsProvider optionsProvider,
        bool isAssignment
        )
    {
        var context =
            new ConditionalFormattingContext
            (
                conditionalExpression,
                sourceText,
                ConfigurationReader.GetIndentationUnit(conditionalExpression.SyntaxTree, optionsProvider)
            );

        if (isAssignment)
        {
            return ValidateAssignmentFormatting(conditionalExpression, context);
        }

        return ValidateReturnOrOtherFormatting(conditionalExpression, context);
    }

    private bool ValidateAssignmentFormatting
        (
        ConditionalExpressionSyntax conditionalExpression,
        ConditionalFormattingContext context
        )
    {
        var equalsTokenLine =
            GetEqualsTokenLine(conditionalExpression, context.SourceText);

        if (equalsTokenLine is null)
        {
            return true; // Cannot determine context, assume correct.
        }

        // Validate multi-line split
        if (!context.IsProperlySplitForAssignment(equalsTokenLine.Value))
        {
            return false;
        }

        // Get base indentation
        var assignmentIndentation =
            GetAssignmentIndentation(conditionalExpression, context.SourceText);

        if (assignmentIndentation is null)
        {
            return true; // Cannot determine indentation, assume correct.
        }

        // Validate indentation
        return context
            .ValidateAssignmentIndentation(assignmentIndentation);
    }

    private bool ValidateReturnOrOtherFormatting
        (
        ConditionalExpressionSyntax conditionalExpression,
        ConditionalFormattingContext context
        )
    {
        var parentStatement =
            conditionalExpression
                .FirstAncestorOrSelf<StatementSyntax>();

        if (parentStatement is null)
        {
            return true;
        }

        var statementLine =
            context.SourceText
                .GetLineNumberFor(parentStatement.GetFirstToken());

        // Validate multi-line split
        if (!context.IsProperlySplitForReturnOrOther(statementLine))
        {
            return false;
        }

        // Get base indentation
        var statementIndentation =
            GetStatementIndentation(conditionalExpression, context.SourceText);

        if (statementIndentation is null)
        {
            return true; // Cannot determine statement, assume correct
        }

        // Validate indentation
        return context
            .ValidateReturnOrOtherIndentation(statementIndentation);
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

        public int ConditionLine { get; } =
            sourceText
                .GetLineNumberFor(conditionalExpression.Condition.GetFirstToken());

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

        public bool IsProperlySplitForAssignment(int equalsTokenLine)
            => ConditionLine > equalsTokenLine
            && QuestionLine > ConditionLine
            && ColonLine > WhenTrueLine;

        public bool IsProperlySplitForReturnOrOther(int statementLine)
            => ConditionLine == statementLine
            && QuestionLine > ConditionLine
            && ColonLine > WhenTrueLine;

        public bool ValidateAssignmentIndentation(string baseIndentation)
        {
            var expectedConditionIndentation =
                baseIndentation + _indentationUnit;

            var expectedOperatorIndentation =
                expectedConditionIndentation + _indentationUnit;

            var conditionIndentation =
                SourceText
                    .GetLineIndentationFor(ConditionLine);

            var questionIndentation =
                SourceText
                    .GetLineIndentationFor(QuestionLine);

            var colonIndentation =
                SourceText
                    .GetLineIndentationFor(ColonLine);

            return conditionIndentation == expectedConditionIndentation
                && questionIndentation == expectedOperatorIndentation
                && colonIndentation == expectedOperatorIndentation;
        }

        public bool ValidateReturnOrOtherIndentation(string baseIndentation)
        {
            var expectedOperatorIndentation =
                baseIndentation + _indentationUnit;

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

    private string? GetAssignmentIndentation
        (
        ConditionalExpressionSyntax conditionalExpression,
        SourceText sourceText
        )
    {
        var parent =
            conditionalExpression.Parent;

        if (parent is EqualsValueClauseSyntax equalsValue)
        {
            if (equalsValue.Parent is VariableDeclaratorSyntax declarator)
            {
                var declaratorLine =
                    sourceText
                        .GetLineNumberFor(declarator);

                return sourceText
                    .GetLineIndentationFor(declaratorLine);
            }
        }

        if (parent is AssignmentExpressionSyntax assignment)
        {
            var assignmentLine =
                sourceText
                    .GetLineNumberFor(assignment);

            return sourceText
                .GetLineIndentationFor(assignmentLine);
        }

        return null;
    }

    private int? GetEqualsTokenLine
        (
        ConditionalExpressionSyntax conditionalExpression,
        SourceText sourceText
        )
    {
        var parent =
            conditionalExpression.Parent;

        if (parent is EqualsValueClauseSyntax equalsValue)
        {
            return sourceText
                .GetLineNumberFor(equalsValue.EqualsToken);
        }

        if (parent is AssignmentExpressionSyntax assignment)
        {
            return sourceText
                .GetLineNumberFor(assignment.OperatorToken);
        }

        return null;
    }

    private string? GetStatementIndentation
        (
        ConditionalExpressionSyntax conditionalExpression,
        SourceText sourceText
        )
    {
        var parent =
            conditionalExpression.Parent;

        while (parent != null)
        {
            if (parent is StatementSyntax statement)
            {
                var statementLine =
                    sourceText
                        .GetLineNumberFor(statement);

                return sourceText
                    .GetLineIndentationFor(statementLine);
            }

            parent = parent.Parent;
        }

        return null;
    }
}
