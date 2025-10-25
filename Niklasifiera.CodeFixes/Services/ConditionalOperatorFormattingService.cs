namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Service for formatting conditional operators (ternary if statements).
/// </summary>
public interface IConditionalOperatorFormattingService
{
    /// <summary>
    /// Determines if a code fix should be skipped due to trivia concerns.
    /// </summary>
    Task<bool> ShouldSkipCodeFixAsync
        (
        Document document,
        ConditionalExpressionSyntax conditionalExpression
        );

    /// <summary>
    /// Formats the conditional expression according to configuration.
    /// </summary>
    Task<ConditionalExpressionSyntax> FormatAsync
        (
        Document document,
        ConditionalExpressionSyntax conditionalExpression,
        SourceText sourceText
        );
}

/// <summary>
/// Service for formatting conditional operators (ternary if statements).
/// </summary>
public class ConditionalOperatorFormattingService(IConfigurationService configurationService)
    : IConditionalOperatorFormattingService, ICodeFixService
{
    public const string DiagnosticId =
        "NIKL003";

    string ICodeFixService.DiagnosticId => DiagnosticId;

    private readonly IConfigurationService _configurationService =
        configurationService
            ?? throw new ArgumentNullException(nameof(configurationService));

    public async Task RegisterCodeFixAsync(CodeFixContext context)
    {
        var root =
            await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

        if (root is null)
        {
            return;
        }

        var diagnostic =
            context.Diagnostics
                .First();

        var diagnosticSpan =
            diagnostic.Location.SourceSpan;

        var conditionalExpression =
            root.FindToken(diagnosticSpan.Start)
                .Parent?
                    .AncestorsAndSelf()
                    .OfType<ConditionalExpressionSyntax>()
                    .FirstOrDefault();

        if (conditionalExpression is null)
        {
            return;
        }

        var shouldSkipCodeFix =
            await ShouldSkipCodeFixAsync(context.Document, conditionalExpression)
                .ConfigureAwait(false);

        if (shouldSkipCodeFix)
        {
            return;
        }

        context
            .RegisterCodeFix
            (
                CodeAction.Create
                (
                    title: CodeFixResources.ConditionalOperatorCodeFixTitle,
                    createChangedDocument: x => FormatDocumentAsync(context.Document, root, conditionalExpression, x),
                    equivalenceKey: nameof(CodeFixResources.ConditionalOperatorCodeFixTitle)
                ),
                diagnostic
            );
    }

    public async Task<bool> ShouldSkipCodeFixAsync
        (
        Document document,
        ConditionalExpressionSyntax conditionalExpression
        )
    {
        // Always skip if preprocessor directives are present
        var containsAnyPreprocessorTrivia =
            conditionalExpression
                .DescendantTrivia(descendIntoTrivia: true)
                .Any(x => x.IsPreprocessorDirective());

        if (containsAnyPreprocessorTrivia)
        {
            return true;
        }

        // Check if the node contains non-whitespace trivia
        if (!conditionalExpression.ContainsNonWhitespaceTrivia())
        {
            return false;
        }

        var triviaHandling =
            await _configurationService
                .GetTriviaHandlingBehaviorAsync(document)
                .ConfigureAwait(false);

        return triviaHandling == TriviaHandlingBehavior.Skip;
    }

    public async Task<ConditionalExpressionSyntax> FormatAsync
        (
        Document document,
        ConditionalExpressionSyntax conditionalExpression,
        SourceText sourceText
        )
    {
        var indentationUnit =
            await _configurationService
                .GetIndentationUnitAsync(document)
                .ConfigureAwait(false);

        var lineEnding =
            await _configurationService
                .GetLineEndingAsync(document)
                .ConfigureAwait(false);

        var lineEndingTrivia =
            SyntaxFactory.EndOfLine(lineEnding);

        var triviaHandling =
            await _configurationService
                .GetTriviaHandlingBehaviorAsync(document)
                .ConfigureAwait(false);

        var shouldPreserveTrivia =
            triviaHandling == TriviaHandlingBehavior.Preserve;

        var parameters =
            new FormattingParameters
            (
                sourceText,
                indentationUnit,
                lineEndingTrivia,
                shouldPreserveTrivia
            );

        // Format nested conditionals recursively (innermost first)
        return FormatConditionalRecursively(conditionalExpression, parameters);
    }

    private ConditionalExpressionSyntax FormatConditionalRecursively
        (
        ConditionalExpressionSyntax conditionalExpression,
        FormattingParameters parameters
        )
    {
        // Determine the assignment context ONCE at the top level
        var isTopLevelAssignment =
            IsPartOfAssignment(conditionalExpression);

        return FormatConditionalRecursivelyInternal(conditionalExpression, parameters, isTopLevelAssignment);
    }

    private ConditionalExpressionSyntax FormatConditionalRecursivelyInternal
        (
        ConditionalExpressionSyntax conditionalExpression,
        FormattingParameters parameters,
        bool isInAssignmentContext
        )
    {
        // Calculate indentation for this level
        var (ConditionIndent, OperatorIndent) =
            CalculateIndentation
            (
                conditionalExpression,
                parameters,
                isInAssignmentContext
            );

        // Recursively format nested conditionals
        var formattedNested =
            FormatNestedConditionals
            (
                conditionalExpression,
                parameters,
                OperatorIndent
            );

        // Format this level
        var context =
            new FormattingContext
            (
                ConditionIndent,
                OperatorIndent,
                parameters.LineEndingTrivia,
                parameters.PreserveTrivia,
                isInAssignmentContext
            );

        return FormatConditionalExpression(formattedNested, context);
    }

    private (string ConditionIndent, string OperatorIndent) CalculateIndentation
        (
        ConditionalExpressionSyntax conditionalExpression,
        FormattingParameters parameters,
        bool isInAssignmentContext
        )
    {
        if (isInAssignmentContext)
        {
            return CalculateAssignmentIndentation(conditionalExpression, parameters);
        }

        return CalculateStatementIndentation(conditionalExpression, parameters);
    }

    private (string ConditionIndent, string OperatorIndent) CalculateAssignmentIndentation
        (
        ConditionalExpressionSyntax conditionalExpression,
        FormattingParameters parameters
        )
    {
        var baseIndent =
            GetBaseIndentationForAssignment(conditionalExpression, parameters.SourceText);

        var conditionIndent =
            baseIndent + parameters.IndentationUnit;

        var operatorIndent =
            CalculateOperatorIndent
            (
                conditionalExpression.Condition,
                parameters,
                conditionIndent
            );

        return (conditionIndent, operatorIndent);
    }

    private (string ConditionIndent, string OperatorIndent) CalculateStatementIndentation
        (
        ConditionalExpressionSyntax conditionalExpression,
        FormattingParameters parameters
        )
    {
        var baseIndent =
            GetBaseIndentationForStatement(conditionalExpression, parameters.SourceText);

        var conditionEndLine =
            parameters.SourceText
                .GetLineNumberFor(conditionalExpression.Condition.GetLastToken());

        var conditionEndIndentation =
            parameters.SourceText
                .GetLineIndentationFor(conditionEndLine);

        var operatorIndent =
            conditionEndIndentation + parameters.IndentationUnit;

        return (baseIndent, operatorIndent);
    }

    private string CalculateOperatorIndent
        (
        ExpressionSyntax condition,
        FormattingParameters parameters,
        string conditionIndent
        )
    {
        var conditionStartLine =
            parameters.SourceText
                .GetLineNumberFor(condition.GetFirstToken());

        var conditionEndLine =
            parameters.SourceText
                .GetLineNumberFor(condition.GetLastToken());

        if (conditionStartLine == conditionEndLine)
        {
            // Single-line condition: operators at conditionIndent + 4
            return conditionIndent + parameters.IndentationUnit;
        }

        // Multi-line condition: operators at conditionEndIndent + 4
        var conditionEndIndentation =
            parameters.SourceText
                .GetLineIndentationFor(conditionEndLine);

        return conditionEndIndentation + parameters.IndentationUnit;
    }

    private ConditionalExpressionSyntax FormatNestedConditionals
        (
        ConditionalExpressionSyntax conditionalExpression,
        FormattingParameters parameters,
        string operatorIndent
        )
    {
        var formattedWhenTrue =
            conditionalExpression.WhenTrue is ConditionalExpressionSyntax nestedTrue
                ? FormatNestedConditionalWithIndent(nestedTrue, parameters, operatorIndent)
                : conditionalExpression.WhenTrue;

        var formattedWhenFalse =
            conditionalExpression.WhenFalse is ConditionalExpressionSyntax nestedFalse
                ? FormatNestedConditionalWithIndent(nestedFalse, parameters, operatorIndent)
                : conditionalExpression.WhenFalse;

        return conditionalExpression
            .WithWhenTrue(formattedWhenTrue)
            .WithWhenFalse(formattedWhenFalse);
    }

    private ConditionalExpressionSyntax FormatNestedConditionalWithIndent
        (
        ConditionalExpressionSyntax nestedConditional,
        FormattingParameters parameters,
        string parentOperatorIndent
        )
    {
        // Nested conditionals: the condition starts at the parent's operator indentation level
        var conditionIndent =
            parentOperatorIndent;

        // Calculate operator indentation
        // Check if the nested condition is already on its own line or needs to be moved
        var conditionStartLine =
            parameters.SourceText
                .GetLineNumberFor(nestedConditional.Condition.GetFirstToken());

        var conditionEndLine =
            parameters.SourceText
                .GetLineNumberFor(nestedConditional.Condition.GetLastToken());

        string operatorIndent;

        if (conditionStartLine == conditionEndLine)
        {
            // Single-line condition (either originally or after formatting)
            // Operators should be indented one level from where the condition starts
            operatorIndent =
                conditionIndent + parameters.IndentationUnit;
        }
        else
        {
            // Multi-line condition - operators indent from where condition ends
            var conditionEndIndentation =
                parameters.SourceText
                    .GetLineIndentationFor(conditionEndLine);

            operatorIndent =
                conditionEndIndentation + parameters.IndentationUnit;
        }

        // Recursively format deeper nesting - pass the operator indent so child conditionals
        // know where to start their condition
        var formattedWhenTrue =
            nestedConditional.WhenTrue is ConditionalExpressionSyntax deeperNested
                ? FormatNestedConditionalWithIndent(deeperNested, parameters, operatorIndent)
                : nestedConditional.WhenTrue;

        var formattedWhenFalse =
            nestedConditional.WhenFalse is ConditionalExpressionSyntax deeperNestedFalse
                ? FormatNestedConditionalWithIndent(deeperNestedFalse, parameters, operatorIndent)
                : nestedConditional.WhenFalse;

        var updatedNested =
            nestedConditional
                .WithWhenTrue(formattedWhenTrue)
                .WithWhenFalse(formattedWhenFalse);

        // Format this nested level
        var context =
            new FormattingContext
            (
                conditionIndent,
                operatorIndent,
                parameters.LineEndingTrivia,
                parameters.PreserveTrivia,
                false // Nested conditionals don't add newline before condition
            );

        return FormatConditionalExpression(updatedNested, context);
    }

    private ConditionalExpressionSyntax FormatConditionalWithContext
        (
        ConditionalExpressionSyntax conditionalExpression,
        FormattingParameters parameters,
        bool isInAssignmentContext
        )
    {
        // Use the passed context instead of detecting it
        var isAssignment = isInAssignmentContext;

        string baseIndentation;
        string conditionIndentation;
        string operatorIndentation;

        if (isAssignment)
        {
            // GOOD format for assignments:
            // variable =
            //     condition
            //         ? "Yes"
            //         : "No";
            baseIndentation =
                GetBaseIndentationForAssignment(conditionalExpression, parameters.SourceText);

            conditionIndentation =
                baseIndentation + parameters.IndentationUnit;

            operatorIndentation =
                conditionIndentation + parameters.IndentationUnit;
        }
        else
        {
            // GOOD format for returns:
            // return condition
            //     ? "Yes"
            //     : "No";
            // 
            // For multi-line conditions:
            // return condition
            //     .ToString() == "True"
            //         ? "Yes"
            //         : "No";
            // Operators indent from condition's last line
            baseIndentation =
                GetBaseIndentationForStatement(conditionalExpression, parameters.SourceText);

            conditionIndentation =
                baseIndentation;

            // Calculate operator indentation based on where condition actually ends
            var conditionEndLine =
                parameters.SourceText
                    .GetLineNumberFor(conditionalExpression.Condition.GetLastToken());

            var conditionEndIndentation =
                parameters.SourceText
                    .GetLineIndentationFor(conditionEndLine);

            operatorIndentation =
                conditionEndIndentation + parameters.IndentationUnit;
        }

        var context =
            new FormattingContext
            (
                conditionIndentation,
                operatorIndentation,
                parameters.LineEndingTrivia,
                parameters.PreserveTrivia,
                isAssignment
            );

        return FormatConditionalExpression(conditionalExpression, context);
    }

    private string GetBaseIndentationForAssignment
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

        return "";
    }

    private string GetBaseIndentationForStatement
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

        return "";
    }

    private ConditionalExpressionSyntax FormatConditionalExpression
        (
        ConditionalExpressionSyntax conditionalExpression,
        FormattingContext context
        )
    {
        var formattedCondition =
            FormatCondition(conditionalExpression.Condition, context);

        var formattedQuestionToken =
            FormatOperatorToken(conditionalExpression.QuestionToken, context);

        var formattedWhenTrue =
            FormatExpression(conditionalExpression.WhenTrue, context.PreserveTrivia);

        var formattedColonToken =
            FormatOperatorToken(conditionalExpression.ColonToken, context);

        var formattedWhenFalse =
            FormatExpression(conditionalExpression.WhenFalse, context.PreserveTrivia);

        return SyntaxFactory.ConditionalExpression
        (
            formattedCondition,
            formattedQuestionToken,
            formattedWhenTrue,
            formattedColonToken,
            formattedWhenFalse
        );
    }

    private ExpressionSyntax FormatCondition
        (
        ExpressionSyntax condition,
        FormattingContext context
        )
    {
        if (context.IsAssignment)
        {
            // For assignments, preserve comments but control whitespace precisely
            var leadingTrivia =
                SyntaxFactory.TriviaList
                (
                    context.LineEndingTrivia,
                    SyntaxFactory.Whitespace(context.ConditionIndentation)
                );

            // Use WithPreservedTrivia if preserve mode, otherwise strip all trivia
            return context.PreserveTrivia
                ? condition
                    .WithPreservedTrivia(leadingTrivia, SyntaxFactory.TriviaList())
                : condition
                    .WithLeadingTrivia(leadingTrivia)
                    .WithTrailingTrivia(SyntaxFactory.TriviaList());
        }

        // For returns/statements, preserve trivia if requested but keep condition on same line
        return context.PreserveTrivia
            ? condition
                .WithPreservedTrivia(SyntaxFactory.TriviaList(), SyntaxFactory.TriviaList())
            : condition
                .WithLeadingTrivia(SyntaxFactory.TriviaList())
                .WithTrailingTrivia(SyntaxFactory.TriviaList());
    }

    private SyntaxToken FormatOperatorToken
        (
        SyntaxToken token,
        FormattingContext context
        )
    {
        var leadingTrivia =
            SyntaxFactory.TriviaList
            (
                context.LineEndingTrivia,
                SyntaxFactory.Whitespace(context.OperatorIndentation)
            );

        var trailingTrivia =
            SyntaxFactory.TriviaList(SyntaxFactory.Space);

        // Preserve comments but control whitespace precisely
        return context.PreserveTrivia
            ? token.WithPreservedTrivia(leadingTrivia, trailingTrivia)
            : SyntaxFactory.Token(leadingTrivia, token.Kind(), trailingTrivia);
    }

    private ExpressionSyntax FormatExpression
        (
        ExpressionSyntax expression,
        bool preserveTrivia
        )
        // Expressions after operators should have no leading/trailing whitespace
        // But preserve comments if in preserve mode
        => preserveTrivia
            ? expression
                .WithPreservedTrivia(SyntaxFactory.TriviaList(), SyntaxFactory.TriviaList())
            : expression
                .WithLeadingTrivia(SyntaxFactory.TriviaList())
                .WithTrailingTrivia(SyntaxFactory.TriviaList());

    private bool IsPartOfAssignment(ConditionalExpressionSyntax conditionalExpression)
    {
        var parent =
            conditionalExpression.Parent;

        if (parent is EqualsValueClauseSyntax equalsValue)
        {
            return equalsValue.Parent is VariableDeclaratorSyntax
                || equalsValue.Parent?.Parent is AssignmentExpressionSyntax;
        }

        if (parent is AssignmentExpressionSyntax)
        {
            return true;
        }

        return false;
    }

    private async Task<Document> FormatDocumentAsync
        (
        Document document,
        SyntaxNode root,
        ConditionalExpressionSyntax conditionalExpression,
        CancellationToken cancellationToken
        )
    {
        var sourceText =
            await document
                .GetTextAsync(cancellationToken)
                .ConfigureAwait(false);

        var newConditionalExpression =
            await FormatAsync(document, conditionalExpression, sourceText)
                .ConfigureAwait(false);

        // For assignments, we need to modify both the conditional AND the equals token
        if (IsPartOfAssignment(conditionalExpression))
        {
            var declarator =
                conditionalExpression
                    .FirstAncestorOrSelf<VariableDeclaratorSyntax>();

            if (declarator?.Initializer != null)
            {
                // Create new initializer with no trailing space on = and the formatted conditional
                var newEqualsToken =
                    declarator.Initializer.EqualsToken
                        .WithTrailingTrivia(SyntaxFactory.TriviaList());

                var newInitializer =
                    declarator.Initializer
                        .WithEqualsToken(newEqualsToken)
                        .WithValue(newConditionalExpression);

                var newDeclarator =
                    declarator
                        .WithInitializer(newInitializer);

                var newRoot =
                    root.ReplaceNode(declarator, newDeclarator);

                return document
                    .WithSyntaxRoot(newRoot);
            }

            // Handle regular assignment (not variable declaration)
            var assignment =
                conditionalExpression.Parent as AssignmentExpressionSyntax;

            if (assignment != null)
            {
                // Create new assignment with no trailing space on = and the formatted conditional
                var newOperatorToken =
                    assignment.OperatorToken
                        .WithTrailingTrivia(SyntaxFactory.TriviaList());

                var newAssignment =
                    assignment
                        .WithOperatorToken(newOperatorToken)
                        .WithRight(newConditionalExpression);

                var newRoot =
                    root.ReplaceNode(assignment, newAssignment);

                return document
                    .WithSyntaxRoot(newRoot);
            }
        }

        // For non-assignment cases (like return statements), just replace the conditional
        var finalRoot =
            root.ReplaceNode(conditionalExpression, newConditionalExpression);

        return document
            .WithSyntaxRoot(finalRoot);
    }

    /// <summary>
    /// Encapsulates parameters passed to formatting methods.
    /// </summary>
    private sealed class FormattingParameters
        (
        SourceText sourceText,
        string indentationUnit,
        SyntaxTrivia lineEndingTrivia,
        bool preserveTrivia
        )
    {
        public SourceText SourceText { get; } = sourceText;
        public string IndentationUnit { get; } = indentationUnit;
        public SyntaxTrivia LineEndingTrivia { get; } = lineEndingTrivia;
        public bool PreserveTrivia { get; } = preserveTrivia;
    }

    /// <summary>
    /// Encapsulates formatting parameters to reduce function argument count.
    /// </summary>
    private sealed class FormattingContext
        (
        string conditionIndentation,
        string operatorIndentation,
        SyntaxTrivia lineEndingTrivia,
        bool preserveTrivia,
        bool isAssignment
        )
    {
        public string ConditionIndentation { get; } = conditionIndentation;
        public string OperatorIndentation { get; } = operatorIndentation;
        public SyntaxTrivia LineEndingTrivia { get; } = lineEndingTrivia;
        public bool PreserveTrivia { get; } = preserveTrivia;
        public bool IsAssignment { get; } = isAssignment;
    }
}
