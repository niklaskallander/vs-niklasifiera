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

        // Determine if this is part of an assignment
        var isAssignment =
            IsPartOfAssignment(conditionalExpression);

        return FormatConditional(conditionalExpression, parameters, isAssignment);
    }

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

    private ConditionalExpressionSyntax FormatConditional
        (
        ConditionalExpressionSyntax conditionalExpression,
        FormattingParameters parameters,
        bool isAssignment
        )
    {
        string baseIndentation;
        string conditionIndentation;
        string operatorIndentation;

        if (isAssignment)
        {
            // Format: variable =
            //     condition
            //         ? trueExpression
            //         : falseExpression;
            baseIndentation =
                GetAssignmentIndentation(conditionalExpression, parameters.SourceText);

            conditionIndentation =
                baseIndentation + parameters.IndentationUnit;

            operatorIndentation =
                conditionIndentation + parameters.IndentationUnit;
        }
        else
        {
            // Format: return condition
            //     ? trueExpression
            //     : falseExpression;
            baseIndentation =
                GetStatementIndentation(conditionalExpression, parameters.SourceText);

            conditionIndentation =
                baseIndentation;

            operatorIndentation =
                baseIndentation + parameters.IndentationUnit;
        }

        var context =
            new FormattingContext
            (
                conditionIndentation,
                operatorIndentation,
                parameters.LineEndingTrivia,
                parameters.PreserveTrivia,
                !isAssignment
            );

        return FormatConditionalExpression(conditionalExpression, context);
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
        var formattedCondition =
            context.PreserveTrivia
                ? condition
                    .WithPreservedTrivia(condition.GetLeadingTrivia(), condition.GetTrailingTrivia())
                : condition
                    .WithoutTrivia();

        if (context.IsFirstPartOfStatement)
        {
            // For 'return condition', the space is part of the return keyword's trivia.
            // The condition itself should have no leading trivia.
            return formattedCondition
                .WithLeadingTrivia(SyntaxFactory.TriviaList())
                .WithTrailingTrivia(SyntaxFactory.TriviaList());
        }

        // For assignments, add newline + indentation as leading trivia
        var leadingTrivia =
            SyntaxFactory.TriviaList
            (
                context.LineEndingTrivia,
                SyntaxFactory.Whitespace(context.ConditionIndentation)
            );

        return formattedCondition
            .WithLeadingTrivia(leadingTrivia)
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

        return context.PreserveTrivia
            ? token.WithPreservedTrivia(leadingTrivia, trailingTrivia)
            : SyntaxFactory.Token
            (
                leadingTrivia,
                token.Kind(),
                trailingTrivia
            );
    }

    private ExpressionSyntax FormatExpression
        (
        ExpressionSyntax expression,
        bool preserveTrivia
        )
        // Expressions after operators should have no leading whitespace
        // and no trailing whitespace
        => preserveTrivia
            ? expression
                .WithPreservedTrivia
                (
                    SyntaxFactory.TriviaList(),
                    SyntaxFactory.TriviaList()
                )
            : expression
                .WithLeadingTrivia(SyntaxFactory.TriviaList())
                .WithTrailingTrivia(SyntaxFactory.TriviaList());

    private string GetAssignmentIndentation
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

    private string GetStatementIndentation
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
        bool isFirstPartOfStatement
        )
    {
        public string ConditionIndentation { get; } = conditionIndentation;
        public string OperatorIndentation { get; } = operatorIndentation;
        public SyntaxTrivia LineEndingTrivia { get; } = lineEndingTrivia;
        public bool PreserveTrivia { get; } = preserveTrivia;
        public bool IsFirstPartOfStatement { get; } = isFirstPartOfStatement;
    }
}
