namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Service for formatting method/constructor signatures while preserving trivia.
/// </summary>
public interface ISignatureFormattingService
{
    /// <summary>
    /// Determines if a code fix should be skipped due to trivia concerns.
    /// </summary>
    Task<bool> ShouldSkipCodeFixAsync
        (
        Document document,
        ParameterListSyntax parameterList
        );

    /// <summary>
    /// Formats the parameter list according to configuration.
    /// </summary>
    Task<ParameterListSyntax> FormatSignatureAsync
        (
        Document document,
        ParameterListSyntax parameterList,
        SourceText sourceText
        );

    /// <summary>
    /// Formats the parameter list and handles document-level changes.
    /// </summary>
    Task<(SyntaxNode newRoot, bool hasChanges)> FormatSignatureWithContextAsync
        (
        Document document,
        SyntaxNode root,
        ParameterListSyntax parameterList,
        SourceText sourceText
        );
}

/// <summary>
/// Service for formatting method/constructor signatures while preserving trivia.
/// </summary>
public class SignatureFormattingService(IConfigurationService configurationService)
    : ISignatureFormattingService, ICodeFixService
{
    public const string DiagnosticId = "NIKL001";

    string ICodeFixService.DiagnosticId => DiagnosticId;

    private readonly IConfigurationService _configurationService =
        configurationService
            ?? throw new ArgumentNullException(nameof(configurationService));

    private readonly struct ParameterFormattingContext
        (
        int index,
        string indentation,
        SyntaxTrivia lineEndingTrivia,
        bool preserveTrivia
        )
    {
        public int Index { get; } = index;
        public string Indentation { get; } = indentation;
        public SyntaxTrivia LineEndingTrivia { get; } = lineEndingTrivia;
        public bool PreserveTrivia { get; } = preserveTrivia;
    }

    private readonly struct ParenthesesFormattingContext
        (
        string indentation,
        SyntaxTrivia lineEndingTrivia,
        SyntaxTriviaList finalTrivia,
        bool preserveTrivia
        )
    {
        public string Indentation { get; } = indentation;
        public SyntaxTrivia LineEndingTrivia { get; } = lineEndingTrivia;
        public SyntaxTriviaList FinalTrivia { get; } = finalTrivia;
        public bool PreserveTrivia { get; } = preserveTrivia;
    }

    public async Task<bool> ShouldSkipCodeFixAsync
        (
        Document document,
        ParameterListSyntax parameterList
        )
    {
        // Always skip if preprocessor directives are present (too risky to reformat)
        var hasPreprocessorDirectives =
            parameterList
                .DescendantTrivia(descendIntoTrivia: true)
                .Any(x => x.IsPreprocessorDirective());

        if (hasPreprocessorDirectives)
        {
            return true; // Always skip when preprocessor directives are present
        }

        // Check if the node contains non-whitespace trivia
        if (!parameterList.ContainsNonWhitespaceTrivia())
        {
            return false; // No trivia to worry about, proceed with fix
        }

        // Get the behavior setting from configuration
        var triviaHandling =
            await _configurationService
                .GetTriviaHandlingBehaviorAsync(document)
                .ConfigureAwait(false);

        return triviaHandling switch
        {
            TriviaHandlingBehavior.Skip
                => true, // Skip fixes when trivia is present

            TriviaHandlingBehavior.Preserve
                => false, // Apply fixes and preserve trivia
            _
                => true, // Default: skip fixes to be safe
        };
    }

    public async Task<ParameterListSyntax> FormatSignatureAsync
        (
        Document document,
        ParameterListSyntax parameterList,
        SourceText sourceText
        )
    {
        var parameterCount =
            parameterList.Parameters.Count;

        var triviaHandling =
            await _configurationService
                .GetTriviaHandlingBehaviorAsync(document)
                .ConfigureAwait(false);

        var shouldPreserveTrivia =
            triviaHandling == TriviaHandlingBehavior.Preserve;

        if (parameterCount <= 1)
        {
            // Format on a single line
            return shouldPreserveTrivia
                ? FormatSingleLineWithTriviaPreservation(parameterList)
                : FormatSingleLine(parameterList);
        }
        else
        {
            // Format on multiple lines
            return await FormatMultipleLinesAsync
            (
                document,
                parameterList,
                sourceText,
                shouldPreserveTrivia
            )
            .ConfigureAwait(false);
        }
    }

    private ParameterListSyntax FormatSingleLine(ParameterListSyntax parameterList)
    {
        // Remove all trivia and format on single line (original behavior)
        var newParameters =
            SyntaxFactory.SeparatedList
            (
                parameterList.Parameters.Select(x => x.WithoutTrivia()),
                parameterList.Parameters.GetSeparators().Select(x => x.WithoutTrivia())
            );

        return SyntaxFactory
            .ParameterList(newParameters)
            .WithOpenParenToken(parameterList.OpenParenToken.WithoutTrivia())
            .WithCloseParenToken(parameterList.CloseParenToken.WithoutTrivia());
    }

    private ParameterListSyntax FormatSingleLineWithTriviaPreservation(ParameterListSyntax parameterList)
    {
        // Preserve non-whitespace trivia while reformatting to single line
        var newParameters = new List<ParameterSyntax>();
        var newSeparators = new List<SyntaxToken>();

        for (var i = 0; i < parameterList.Parameters.Count; i++)
        {
            var originalParameter =
                parameterList.Parameters[i];

            // Keep non-whitespace trivia but clean up spacing
            var newLeadingWhitespace =
                i == 0
                    ? SyntaxFactory.TriviaList()
                    : SyntaxFactory.TriviaList(SyntaxFactory.Space);

            newParameters
                .Add
                (
                    originalParameter
                        .WithPreservedTrivia
                        (
                            newLeadingWhitespace,
                            SyntaxFactory.TriviaList()
                        )
                );
        }

        // Handle separators (commas)
        for (var i = 0; i < parameterList.Parameters.GetSeparators().Count(); i++)
        {
            var originalSeparator =
                parameterList.Parameters
                    .GetSeparators()
                    .ElementAt(i);

            // Preserve trivia but ensure proper spacing
            newSeparators
                .Add
                (
                    originalSeparator
                        .WithPreservedTrivia
                        (
                            SyntaxFactory.TriviaList(),
                            SyntaxFactory.TriviaList(SyntaxFactory.Space)
                        )
                );
        }

        var separatedList =
            SyntaxFactory
                .SeparatedList
                (
                    newParameters,
                    newSeparators
                );

        // Handle opening and closing parentheses
        var openParen =
            parameterList.OpenParenToken
                .WithPreservedTrivia
                (
                    SyntaxFactory.TriviaList(),
                    SyntaxFactory.TriviaList()
                );

        var closeParen =
            parameterList.CloseParenToken
                .WithPreservedTrivia
                (
                    SyntaxFactory.TriviaList(),
                    SyntaxFactory.TriviaList()
                );

        return SyntaxFactory
            .ParameterList
            (
                openParen,
                separatedList,
                closeParen
            );
    }

    private async Task<ParameterListSyntax> FormatMultipleLinesAsync
        (
        Document document,
        ParameterListSyntax parameterList,
        SourceText sourceText,
        bool preserveTrivia
        )
    {
        var (parenIndentation, parameterIndentation, lineEndingTrivia) =
            await GetSignatureFormattingContextAsync(document, parameterList, sourceText)
                .ConfigureAwait(false);

        var separators =
            CreateCommaSeparators(parameterList.Parameters, lineEndingTrivia, preserveTrivia);

        var newParameters =
            CreateFormattedParameters(parameterList.Parameters, parameterIndentation, lineEndingTrivia, preserveTrivia);

        var newParametersList =
            SyntaxFactory
                .SeparatedList
                (
                    newParameters,
                    separators
                );

        var (openParen, closeParen) =
            CreateFormattedParentheses(parameterList, parenIndentation, lineEndingTrivia, preserveTrivia);

        return SyntaxFactory
            .ParameterList
            (
                openParen,
                newParametersList,
                closeParen
            );
    }

    private async Task<(string parenIndentation, string parameterIndentation, SyntaxTrivia lineEndingTrivia)> GetSignatureFormattingContextAsync
        (
        Document document,
        ParameterListSyntax parameterList,
        SourceText sourceText
        )
    {
        // Find the parent declaration to determine proper indentation context
        var parentNode =
            parameterList.Parent;

        var parentIndentation =
            parentNode != null
                ? GetDeclarationIndentation(parentNode, sourceText)
                : "";

        // Get configuration
        var indentationUnit =
            await _configurationService
                .GetIndentationUnitAsync(document);

        var lineEnding =
            await _configurationService
                .GetLineEndingAsync(document);

        var lineEndingTrivia =
            SyntaxFactory.EndOfLine(lineEnding);

        // Create indentation for opening/closing parens and parameters
        var parenIndentation =
            parentIndentation + indentationUnit;

        var parameterIndentation =
            parentIndentation + indentationUnit;

        return (parenIndentation, parameterIndentation, lineEndingTrivia);
    }

    private static SyntaxToken[] CreateCommaSeparators
        (
        SeparatedSyntaxList<ParameterSyntax> parameters,
        SyntaxTrivia lineEndingTrivia,
        bool preserveTrivia
        )
    {
        var separators =
            new SyntaxToken[parameters.Count - 1];

        for (var i = 0; i < separators.Length; i++)
        {
            if (preserveTrivia)
            {
                var originalSeparator =
                    parameters
                        .GetSeparators()
                        .ElementAt(i);

                separators[i] =
                    originalSeparator
                        .WithPreservedTrivia
                        (
                            SyntaxFactory.TriviaList(),
                            SyntaxFactory.TriviaList(lineEndingTrivia)
                        );
            }
            else
            {
                separators[i] =
                    SyntaxFactory
                        .Token
                        (
                            SyntaxFactory.TriviaList(),
                            SyntaxKind.CommaToken,
                            SyntaxFactory.TriviaList(lineEndingTrivia)
                        );
            }
        }

        return separators;
    }

    private static ParameterSyntax[] CreateFormattedParameters
        (
        SeparatedSyntaxList<ParameterSyntax> parameters,
        string parameterIndentation,
        SyntaxTrivia lineEndingTrivia,
        bool preserveTrivia
        )
    {
        var newParameters =
            new ParameterSyntax[parameters.Count];

        for (var i = 0; i < parameters.Count; i++)
        {
            var originalParameter =
                parameters[i];

            var context =
                new ParameterFormattingContext(i, parameterIndentation, lineEndingTrivia, preserveTrivia);

            newParameters[i] =
                CreateFormattedParameter(originalParameter, context);
        }

        return newParameters;
    }

    private static ParameterSyntax CreateFormattedParameter
        (
        ParameterSyntax originalParameter,
        ParameterFormattingContext context
        )
    {
        var leadingTrivia =
            CreateParameterLeadingTrivia(context.Index, context.Indentation, context.LineEndingTrivia);

        return context.PreserveTrivia
            ? originalParameter
                .WithPreservedTrivia(leadingTrivia, SyntaxFactory.TriviaList())
            : originalParameter
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(SyntaxFactory.TriviaList());
    }

    private static SyntaxTriviaList CreateParameterLeadingTrivia
        (
        int index,
        string parameterIndentation,
        SyntaxTrivia lineEndingTrivia
        )
        => index == 0
            ? SyntaxFactory.TriviaList
            (
                lineEndingTrivia,
                SyntaxFactory.Whitespace(parameterIndentation)
            )
            : SyntaxFactory.TriviaList
            (
                SyntaxFactory.Whitespace(parameterIndentation)
            );

    private static (SyntaxToken openParen, SyntaxToken closeParen) CreateFormattedParentheses
        (
        ParameterListSyntax parameterList,
        string parenIndentation,
        SyntaxTrivia lineEndingTrivia,
        bool preserveTrivia
        )
    {
        var finalCloseParenTrivia =
            CalculateFinalCloseParenTrivia(parameterList, lineEndingTrivia);

        var parenContext =
            new ParenthesesFormattingContext(parenIndentation, lineEndingTrivia, finalCloseParenTrivia, preserveTrivia);

        var openParen =
            CreateFormattedOpenParen(parameterList, parenContext);

        var closeParen =
            CreateFormattedCloseParen(parameterList, parenContext);

        return (openParen, closeParen);
    }

    private static SyntaxTriviaList CalculateFinalCloseParenTrivia
        (
        ParameterListSyntax parameterList,
        SyntaxTrivia lineEndingTrivia
        )
    {
        // Handle brace formatting
        var closeParenTrailingTrivia =
            parameterList.CloseParenToken.TrailingTrivia;

        var hasInlineBrace =
            closeParenTrailingTrivia
                .Any(x => x.IsKind(SyntaxKind.OpenBraceToken));

        return hasInlineBrace
            ? SyntaxFactory.TriviaList
            (
                closeParenTrailingTrivia
                    .Where(x => !x.IsKind(SyntaxKind.OpenBraceToken))
                    .Concat([lineEndingTrivia])
            )
            : closeParenTrailingTrivia;
    }

    private static SyntaxToken CreateFormattedOpenParen
        (
        ParameterListSyntax parameterList,
        ParenthesesFormattingContext context
        )
        => CreateFormattedParen(parameterList, context, parameterList.OpenParenToken, SyntaxFactory.TriviaList());

    private static SyntaxToken CreateFormattedCloseParen
        (
        ParameterListSyntax parameterList,
        ParenthesesFormattingContext context
        )
        => CreateFormattedParen(parameterList, context, parameterList.CloseParenToken, context.FinalTrivia);

    private static SyntaxToken CreateFormattedParen
        (
        ParameterListSyntax parameterList,
        ParenthesesFormattingContext context,
        SyntaxToken syntaxToken,
        SyntaxTriviaList newTrailingWhitespace
        )
    {
        var leadingTrivia =
            CreateParenLeadingTrivia(context.Indentation, context.LineEndingTrivia);

        return context.PreserveTrivia
            ? parameterList.CloseParenToken
                .WithPreservedTrivia(leadingTrivia, newTrailingWhitespace)
            : SyntaxFactory
                .Token(leadingTrivia, syntaxToken.Kind(), newTrailingWhitespace);
    }

    private static SyntaxTriviaList CreateParenLeadingTrivia
        (
        string parenIndentation,
        SyntaxTrivia lineEndingTrivia
        )
        => SyntaxFactory.TriviaList
        (
            lineEndingTrivia,
            SyntaxFactory.Whitespace(parenIndentation)
        );

    #region Helper Methods

    private static string GetDeclarationIndentation
        (
        SyntaxNode parentNode,
        SourceText sourceText
        )
    {
        var declarationLineStart = parentNode switch
        {
            MethodDeclarationSyntax method
                => method.Identifier.SpanStart,

            ConstructorDeclarationSyntax constructor
                => constructor.Identifier.SpanStart,

            TypeDeclarationSyntax type
                => type.Identifier.SpanStart,
            _
                => parentNode.SpanStart,
        };

        return sourceText
            .GetIndentation(declarationLineStart);
    }

    #endregion

    #region Trivia Helper Methods

    public async Task<(SyntaxNode newRoot, bool hasChanges)> FormatSignatureWithContextAsync
        (
        Document document,
        SyntaxNode root,
        ParameterListSyntax parameterList,
        SourceText sourceText
        )
    {
        var newParameterList =
            await FormatSignatureAsync(document, parameterList, sourceText)
                .ConfigureAwait(false);

        var newRoot =
            root.ReplaceNode(parameterList, newParameterList);

        return (newRoot, true);
    }

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

        // Handle parameter list formatting
        var parameterList =
            root.FindToken(diagnosticSpan.Start)
                .Parent?
                    .AncestorsAndSelf()
                    .OfType<ParameterListSyntax>()
                    .First();

        // Check for non-whitespace trivia and handle according to configuration
        if (parameterList is null || await ShouldSkipCodeFixAsync(context.Document, parameterList).ConfigureAwait(false))
        {
            // Skip this code fix - analyzer will still report the diagnostic
            return;
        }

        context
            .RegisterCodeFix
            (
                CodeAction.Create
                (
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: x => FormatSignatureDocumentAsync(context.Document, root, parameterList, x),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)
                ),
                diagnostic
            );
    }

    private async Task<Document> FormatSignatureDocumentAsync
        (
        Document document,
        SyntaxNode root,
        ParameterListSyntax parameterList,
        CancellationToken cancellationToken
        )
    {
        var sourceText =
            await document
                .GetTextAsync(cancellationToken)
                .ConfigureAwait(false);

        // Delegate all formatting logic to the service
        var (newRoot, hasChanges) =
            await FormatSignatureWithContextAsync(document, root, parameterList, sourceText)
                .ConfigureAwait(false);

        return hasChanges
            ? document.WithSyntaxRoot(newRoot)
            : document;
    }

    #endregion
}
