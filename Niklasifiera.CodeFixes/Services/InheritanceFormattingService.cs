namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Service for formatting inheritance declarations while preserving trivia.
/// </summary>
public interface IInheritanceFormattingService
{
    /// <summary>
    /// Determines if a code fix should be skipped due to trivia concerns.
    /// </summary>
    Task<bool> ShouldSkipCodeFixAsync
        (
        Document document,
        BaseListSyntax baseList
        );

    /// <summary>
    /// Formats the inheritance declaration according to configuration.
    /// </summary>
    Task<BaseListSyntax> FormatInheritanceAsync
        (
        Document document,
        BaseListSyntax baseList,
        SourceText sourceText
        );

    /// <summary>
    /// Formats the inheritance declaration and handles all related context (braces, comments, etc.).
    /// </summary>
    Task<(SyntaxNode newRoot, bool hasChanges)> FormatInheritanceWithContextAsync
        (
        Document document,
        SyntaxNode root,
        BaseListSyntax baseList,
        SourceText sourceText
        );
}

/// <summary>
/// Service for formatting inheritance declarations while preserving trivia.
/// </summary>
public class InheritanceFormattingService(IConfigurationService configurationService)
    : IInheritanceFormattingService, ICodeFixService
{
    public const string DiagnosticId = "NIKL002";

    string ICodeFixService.DiagnosticId => DiagnosticId;

    private readonly IConfigurationService _configurationService =
        configurationService
            ?? throw new ArgumentNullException(nameof(configurationService));

    public async Task<bool> ShouldSkipCodeFixAsync
        (
        Document document,
        BaseListSyntax baseList
        )
    {
        // Always skip if preprocessor directives are present (too risky to reformat)
        var hasPreprocessorDirectives =
            baseList
                .DescendantTrivia(descendIntoTrivia: true)
                .Any(x => x.IsPreprocessorDirective());

        if (hasPreprocessorDirectives)
        {
            return true; // Always skip when preprocessor directives are present
        }

        // Check if the node contains non-whitespace trivia
        if (!baseList.ContainsNonWhitespaceTrivia())
        {
            return false; // No trivia to worry about, proceed with fix
        }

        // Get the behavior setting from configuration
        var triviaHandling =
            await _configurationService
                .GetTriviaHandlingBehaviorAsync(document);

        return triviaHandling switch
        {
            TriviaHandlingBehavior.Skip => true,           // Skip fixes when trivia is present
            TriviaHandlingBehavior.Preserve => false,     // Apply fixes and preserve trivia
            _ => true                                      // Default: skip fixes to be safe
        };
    }

    public async Task<BaseListSyntax> FormatInheritanceAsync
        (
        Document document,
        BaseListSyntax baseList,
        SourceText sourceText
        )
    {
        var triviaHandling =
            await _configurationService
                .GetTriviaHandlingBehaviorAsync(document)
                .ConfigureAwait(false);

        if (triviaHandling == TriviaHandlingBehavior.Preserve)
        {
            return await FormatWithTriviaPreservationAsync(document, baseList, sourceText)
                .ConfigureAwait(false);
        }

        return await FormatWithoutTriviaPreservationAsync(document, baseList, sourceText)
            .ConfigureAwait(false);
    }

    private async Task<BaseListSyntax> FormatWithTriviaPreservationAsync
        (
        Document document,
        BaseListSyntax baseList,
        SourceText sourceText
        )
    {
        // Check if we have any comments to intelligently reposition
        var commentsToReposition =
            CollectCommentsFromBaseList(baseList);

        // Separate inline comments from line comments
        var inlineComments =
            commentsToReposition
                .Where(x => x.IsKind(SyntaxKind.MultiLineCommentTrivia))
                .ToList();

        var lineComments =
            commentsToReposition
                .Where(x => x.IsKind(SyntaxKind.SingleLineCommentTrivia))
                .ToList();

        if (lineComments.Any())
        {
            // Handle intelligent comment repositioning for line comments only
            return await FormatWithIntelligentCommentRepositioning
            (
                document,
                baseList,
                sourceText,
                lineComments
            )
            .ConfigureAwait(false);
        }
        else
        {
            // No line comments to reposition (inline comments are preserved automatically), use original logic
            return await FormatWithOriginalTriviaPreservation
            (
                document,
                baseList,
                sourceText
            )
            .ConfigureAwait(false);
        }
    }

    private async Task<BaseListSyntax> FormatWithoutTriviaPreservationAsync
        (
        Document document,
        BaseListSyntax baseList,
        SourceText sourceText
        )
    {
        var (_, typeIndentation, lineEndingTrivia) =
            await GetFormattingContextAsync(document, baseList, sourceText)
                .ConfigureAwait(false);

        var separators =
            CreateCommaSeparators(baseList.Types, typeIndentation, lineEndingTrivia, preserveTrivia: false);

        var newTypes =
            CreateFormattedBaseTypes(baseList.Types, preserveTrivia: false);

        var newTypesList =
            SyntaxFactory
                .SeparatedList
                (
                    newTypes,
                    separators
                );

        var colon =
            CreateFormattedColon(baseList.ColonToken, typeIndentation, lineEndingTrivia, preserveTrivia: false);

        return SyntaxFactory
            .BaseList
            (
                colon,
                newTypesList
            );
    }

    private async Task<BaseListSyntax> FormatWithOriginalTriviaPreservation
        (
        Document document,
        BaseListSyntax baseList,
        SourceText sourceText
        )
    {
        // Find the parent type declaration to determine proper indentation
        var parentType =
            baseList.Parent as TypeDeclarationSyntax;

        if (parentType == null)
        {
            // Fallback to normal formatting if we can't find the parent type
            return await FormatWithoutTriviaPreservationAsync
            (
                document,
                baseList,
                sourceText
            )
            .ConfigureAwait(false);
        }

        var (_, typeIndentation, lineEndingTrivia) =
            await GetFormattingContextAsync(document, baseList, sourceText)
                .ConfigureAwait(false);

        var separators =
            CreateCommaSeparators(baseList.Types, typeIndentation, lineEndingTrivia, preserveTrivia: true);

        var newTypes =
            CreateFormattedBaseTypes(baseList.Types, preserveTrivia: true);

        var newTypesList =
            SyntaxFactory
                .SeparatedList
                (
                    newTypes,
                    separators
                );

        var colon =
            CreateFormattedColon(baseList.ColonToken, typeIndentation, lineEndingTrivia, preserveTrivia: true);

        return SyntaxFactory
            .BaseList
            (
                colon,
                newTypesList
            );
    }

    private static SyntaxToken[] CreateCommaSeparators
        (
        SeparatedSyntaxList<BaseTypeSyntax> types,
        string typeIndentation,
        SyntaxTrivia lineEndingTrivia,
        bool preserveTrivia
        )
    {
        var separators =
            new SyntaxToken[types.Count - 1];

        var newlineWithIndentation =
            SyntaxFactory
                .TriviaList
                (
                    lineEndingTrivia,
                    SyntaxFactory.Whitespace(typeIndentation)
                );

        for (var i = 0; i < separators.Length; i++)
        {
            if (preserveTrivia)
            {
                var originalSeparator =
                    types
                        .GetSeparators()
                        .ElementAt(i);

                // Leading comma with newline and indentation before it, preserving trivia
                separators[i] =
                    originalSeparator
                        .WithPreservedTrivia
                        (
                            newlineWithIndentation,
                            SyntaxFactory.TriviaList(SyntaxFactory.Space)
                        );
            }
            else
            {
                // Leading comma with newline and indentation before it
                separators[i] =
                    SyntaxFactory
                        .Token
                        (
                            newlineWithIndentation,
                            SyntaxKind.CommaToken,
                            SyntaxFactory.TriviaList(SyntaxFactory.Space)
                        );
            }
        }

        return separators;
    }

    private static BaseTypeSyntax[] CreateFormattedBaseTypes
        (
        SeparatedSyntaxList<BaseTypeSyntax> originalTypes,
        bool preserveTrivia
        )
    {
        var newTypes =
            new BaseTypeSyntax[originalTypes.Count];

        for (var i = 0; i < originalTypes.Count; i++)
        {
            var originalType = originalTypes[i];
            var isFirstType = i == 0;

            newTypes[i] =
                CreateFormattedBaseType(originalType, isFirstType, preserveTrivia);
        }

        return newTypes;
    }

    private static BaseTypeSyntax CreateFormattedBaseType
        (
        BaseTypeSyntax originalType,
        bool isFirstType,
        bool preserveTrivia
        )
    {
        var leadingTrivia =
            isFirstType
                ? SyntaxFactory.TriviaList(SyntaxFactory.Space)
                : SyntaxFactory.TriviaList();

        return preserveTrivia
            ? originalType
                .WithPreservedTrivia(leadingTrivia, SyntaxFactory.TriviaList())
            : originalType
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(SyntaxFactory.TriviaList());
    }

    private static SyntaxToken CreateFormattedColon
        (
        SyntaxToken colonToken,
        string typeIndentation,
        SyntaxTrivia lineEndingTrivia,
        bool preserveTrivia
        )
    {
        var newlineWithIndentation =
            SyntaxFactory
                .TriviaList
                (
                    lineEndingTrivia,
                    SyntaxFactory.Whitespace(typeIndentation)
                );

        if (preserveTrivia)
        {
            // Colon should be on its own line with proper indentation, preserving trivia
            return colonToken
                .WithPreservedTrivia
                (
                    newlineWithIndentation,
                    SyntaxFactory.TriviaList()
                );
        }

        // Colon should be on its own line with proper indentation
        return SyntaxFactory.Token
        (
            newlineWithIndentation,
            SyntaxKind.ColonToken,
            SyntaxFactory.TriviaList()
        );
    }

    private async Task<(string parentIndentation, string typeIndentation, SyntaxTrivia lineEndingTrivia)> GetFormattingContextAsync
        (
        Document document,
        BaseListSyntax baseList,
        SourceText sourceText
        )
    {
        // Find the parent type declaration to determine proper indentation
        var parentIndentation =
            baseList.Parent is TypeDeclarationSyntax parentType
                ? GetDeclarationIndentation(parentType, sourceText)
                : ""; // Fallback: if we can't find the parent type, use empty indentation

        // Get configuration
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

        // Create indentation for types
        var typeIndentation =
            parentIndentation + indentationUnit;

        return (parentIndentation, typeIndentation, lineEndingTrivia);
    }

    private async Task<BaseListSyntax> FormatWithIntelligentCommentRepositioning
        (
        Document document,
        BaseListSyntax baseList,
        SourceText sourceText,
        List<SyntaxTrivia> commentsToReposition
        )
    {
        // Separate inline comments from line comments
        var inlineComments =
            commentsToReposition
                .Where(x => x.IsKind(SyntaxKind.MultiLineCommentTrivia))
                .ToList();

        var lineComments =
            commentsToReposition
                .Where(x => x.IsKind(SyntaxKind.SingleLineCommentTrivia))
                .ToList();

        // For inline comments, we keep them on the parent type and don't include them in repositioning
        // Only remove line comments from the base list, keep inline comments on parent type
        var cleanedBaseList =
            RemoveLineCommentsFromBaseList(baseList);

        var formattedBaseList =
            await FormatWithOriginalTriviaPreservation
            (
                document,
                cleanedBaseList,
                sourceText
            )
            .ConfigureAwait(false);

        // Only reposition line comments, not inline comments
        if (lineComments.Any())
        {
            return formattedBaseList.WithAdditionalAnnotations
            (
                new SyntaxAnnotation
                (
                    "IntelligentCommentRepositioning",
                    string.Join("|", lineComments.Select(x => x.ToString()))
                )
            );
        }
        else
        {
            // No line comments to reposition, just return the formatted base list
            return formattedBaseList;
        }
    }

    #region Helper Methods

    private static List<SyntaxTrivia> CollectCommentsFromBaseList(BaseListSyntax baseList)
    {
        var comments =
            new List<SyntaxTrivia>();

        // Get comments from the parent type declaration (for inline comments like /* comment */)
        if (baseList.Parent is TypeDeclarationSyntax parentType)
        {
            // Check for trailing trivia on the class identifier (like /* inline comment */)
            if (parentType.Identifier.HasTrailingTrivia)
            {
                comments
                    .AddRange
                    (
                        parentType.Identifier.TrailingTrivia
                            .Where(x => x.IsComment())
                    );
            }
        }

        // Get comments from the colon token
        if (baseList.ColonToken.HasLeadingTrivia)
        {
            comments
                .AddRange
                (
                    baseList.ColonToken.LeadingTrivia
                        .Where(x => x.IsComment())
                );
        }

        if (baseList.ColonToken.HasTrailingTrivia)
        {
            comments
                .AddRange
                (
                    baseList.ColonToken.TrailingTrivia
                        .Where(x => x.IsComment())
                );
        }

        // Get comments from the base types and separators
        foreach (var baseType in baseList.Types)
        {
            if (baseType.HasLeadingTrivia)
            {
                comments
                    .AddRange
                    (
                        baseType
                            .GetLeadingTrivia()
                            .Where(x => x.IsComment())
                    );
            }

            if (baseType.HasTrailingTrivia)
            {
                comments
                    .AddRange
                    (
                        baseType
                            .GetTrailingTrivia()
                            .Where(x => x.IsComment())
                    );
            }
        }

        // Get comments from separators
        foreach (var separator in baseList.Types.GetSeparators())
        {
            if (separator.HasLeadingTrivia)
            {
                comments
                    .AddRange
                    (
                        separator.LeadingTrivia
                            .Where(x => x.IsComment())
                    );
            }

            if (separator.HasTrailingTrivia)
            {
                comments
                    .AddRange
                    (
                        separator.TrailingTrivia
                            .Where(x => x.IsComment())
                    );
            }
        }

        return comments;
    }

    private static BaseListSyntax RemoveLineCommentsFromBaseList(BaseListSyntax baseList)
    {
        // Create a new base list with only line comments removed, keeping inline comments
        var colonWithoutComments =
            baseList.ColonToken
                .WithLeadingTrivia(FilterOutLineComments(baseList.ColonToken.LeadingTrivia))
                .WithTrailingTrivia(FilterOutLineComments(baseList.ColonToken.TrailingTrivia));

        var typesWithoutComments =
            baseList.Types
                .Select
                (
                    type
                        => type
                            .WithLeadingTrivia(FilterOutLineComments(type.GetLeadingTrivia()))
                            .WithTrailingTrivia(FilterOutLineComments(type.GetTrailingTrivia()))
                )
                .ToArray();

        var separatorsWithoutComments =
            baseList.Types
                .GetSeparators()
                .Select
                (
                    separator
                        => separator
                            .WithLeadingTrivia(FilterOutLineComments(separator.LeadingTrivia))
                            .WithTrailingTrivia(FilterOutLineComments(separator.TrailingTrivia))
                )
                .ToArray();

        var newTypesList =
            SyntaxFactory.SeparatedList(typesWithoutComments, separatorsWithoutComments);

        var cleanedBaseList =
            SyntaxFactory.BaseList(colonWithoutComments, newTypesList);

        // For inline comments on parent type identifier, keep them (don't remove them)
        // This preserves inline comments like /* comment */ on the class identifier

        return cleanedBaseList;
    }

    private static SyntaxTriviaList FilterOutLineComments(SyntaxTriviaList triviaList)
        => SyntaxFactory
            .TriviaList
            (
                triviaList
                    .Where(trivia => !trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            );

    private static string GetDeclarationIndentation
        (
        SyntaxNode parentNode,
        SourceText sourceText
        )
    {
        // Use the indentation of the line with the declaration identifier
        var declarationLineStart =
            parentNode switch
            {
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

    public async Task<(SyntaxNode newRoot, bool hasChanges)> FormatInheritanceWithContextAsync
        (
        Document document,
        SyntaxNode root,
        BaseListSyntax baseList,
        SourceText sourceText
        )
    {
        // Format the base list first
        var newBaseList =
            await FormatInheritanceAsync(document, baseList, sourceText)
                .ConfigureAwait(false);

        var newRoot =
            root.ReplaceNode(baseList, newBaseList);

        // Handle intelligent comment repositioning if annotations are present
        var annotatedNodes =
            newRoot
                .GetAnnotatedNodes("IntelligentCommentRepositioning")
                .ToList();

        if (annotatedNodes.Any())
        {
            var annotatedBaseList =
                annotatedNodes
                    .OfType<BaseListSyntax>()
                    .FirstOrDefault();

            if (annotatedBaseList != null)
            {
                newRoot =
                    await HandleIntelligentCommentRepositioningAsync(document, newRoot, annotatedBaseList, sourceText)
                        .ConfigureAwait(false);
            }
        }

        // Handle proper brace formatting for the parent type declaration
        if (baseList.Parent is not TypeDeclarationSyntax parentType)
        {
            return (newRoot, true);
        }

        var newParentType =
            newRoot
                .DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault(x => x.Identifier.ValueText == parentType.Identifier.ValueText);

        if (newParentType is null)
        {
            return (newRoot, true);
        }

        var updatedParentType =
            await FormatParentTypeBracesAsync(document, newParentType, sourceText)
                .ConfigureAwait(false);

        newRoot =
            newRoot
                .ReplaceNode(newParentType, updatedParentType);

        return (newRoot, true);
    }

    private async Task<TypeDeclarationSyntax> FormatParentTypeBracesAsync
        (
        Document document,
        TypeDeclarationSyntax parentType,
        SourceText sourceText
        )
    {
        var lineEnding =
            await _configurationService
                .GetLineEndingAsync(document)
                .ConfigureAwait(false);

        // Get the indentation of the class declaration itself for the opening brace
        var classIndentation =
            sourceText
                .GetIndentation(parentType.Identifier.SpanStart);

        // Ensure opening brace is on new line with same indentation as class declaration
        var updatedParentType =
            parentType
                .WithOpenBraceToken
                (
                    parentType.OpenBraceToken
                        .WithLeadingTrivia
                        (
                            SyntaxFactory
                                .TriviaList
                                (
                                    SyntaxFactory.EndOfLine(lineEnding),
                                    SyntaxFactory.Whitespace(classIndentation)
                                )
                        )
                );

        // Clean up any trailing whitespace
        if (parentType.ParameterList != null)
        {
            // For primary constructors, clean up trailing whitespace on parameter list
            updatedParentType =
                updatedParentType
                    .WithParameterList
                    (
                        parentType.ParameterList
                            .WithCloseParenToken
                            (
                                parentType.ParameterList.CloseParenToken
                                    .WithTrailingTrivia(SyntaxFactory.TriviaList())
                            )
                    );
        }
        else
        {
            // For regular classes, preserve any trivia that might have been repositioned intelligently
            var hasIntelligentRepositioning =
                updatedParentType
                    .GetAnnotatedNodes("IntelligentCommentRepositioning")
                    .Any();

            var hasInlineComments =
                updatedParentType.Identifier.TrailingTrivia
                    .Any(x => x.IsKind(SyntaxKind.MultiLineCommentTrivia));

            if (!hasIntelligentRepositioning && !hasInlineComments)
            {
                // Clean up trailing whitespace on class identifier only if no intelligent repositioning or inline comments
                updatedParentType =
                    updatedParentType
                        .WithIdentifier
                        (
                            updatedParentType.Identifier
                                .WithTrailingTrivia(SyntaxFactory.TriviaList())
                        );
            }
        }

        return updatedParentType;
    }

    private async Task<SyntaxNode> HandleIntelligentCommentRepositioningAsync
        (
        Document document,
        SyntaxNode root,
        SyntaxNode annotatedNode,
        SourceText sourceText
        )
    {
        // Get the comments from the annotation
        var annotation =
            annotatedNode
                .GetAnnotations("IntelligentCommentRepositioning")
                .FirstOrDefault();

        if (annotation?.Data == null)
        {
            return root;
        }

        var commentsText =
            annotation.Data
                .Split('|');

        var comments =
            commentsText
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => SyntaxFactory.ParseTrailingTrivia(x).FirstOrDefault())
                .Where(x => !x.IsKind(SyntaxKind.None))
                .ToList();

        if (!comments.Any())
        {
            return root;
        }

        // Find the parent type declaration
        var parentType =
            annotatedNode
                .Ancestors()
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault();

        if (parentType == null)
        {
            return root;
        }

        // Separate inline comments (/* */) from line comments (//)
        var inlineComments =
            comments
                .Where(x => x.IsKind(SyntaxKind.MultiLineCommentTrivia))
                .ToList();

        var lineComments =
            comments
                .Where(x => x.IsKind(SyntaxKind.SingleLineCommentTrivia))
                .ToList();

        // Get indentation configuration
        var lineEnding =
            await _configurationService
                .GetLineEndingAsync(document)
                .ConfigureAwait(false);

        var indentationUnit =
            await _configurationService
                .GetIndentationUnitAsync(document)
                .ConfigureAwait(false);

        // Calculate indentation from the original parent type
        var classIndentation =
            GetDeclarationIndentation(parentType, sourceText);

        var commentIndentation =
            classIndentation + indentationUnit;

        // Get the CURRENT parent type from the updated root
        var currentParentType =
            root.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault(x => x.Identifier.ValueText == parentType.Identifier.ValueText);

        if (currentParentType?.BaseList == null)
        {
            return root;
        }

        SyntaxNode newParentType =
            currentParentType;

        // Handle inline comments
        if (inlineComments.Any())
        {
            newParentType =
                currentParentType
                    .WithIdentifier
                    (
                        currentParentType.Identifier
                            .WithTrailingTrivia
                            (
                                SyntaxFactory
                                    .TriviaList
                                    (
                                        SyntaxFactory.Whitespace(" "),
                                        inlineComments[0]
                                    )
                            )
                    );
        }

        // Handle line comments in the base list
        var baseList =
            currentParentType.BaseList;

        var newBaseList =
            ProcessLineCommentsInBaseList(baseList, lineComments, lineEnding, commentIndentation);

        // Remove the annotation
        newBaseList =
            newBaseList
                .WithoutAnnotations("IntelligentCommentRepositioning");

        // Apply changes
        if (inlineComments.Any() && newParentType != currentParentType)
        {
            var newParentTypeWithUpdatedBaseList =
                ((TypeDeclarationSyntax)newParentType)
                    .WithBaseList(newBaseList);

            return root
                .ReplaceNode(currentParentType, newParentTypeWithUpdatedBaseList);
        }

        return root
            .ReplaceNode(currentParentType.BaseList, newBaseList);
    }

    private static BaseListSyntax ProcessLineCommentsInBaseList
        (
        BaseListSyntax baseList,
        List<SyntaxTrivia> lineComments,
        string lineEnding,
        string commentIndentation
        )
    {
        var newBaseList = baseList;

        // Add the first line comment before the colon
        if (lineComments.Count > 0)
        {
            var colonWithComment =
                baseList.ColonToken
                    .WithLeadingTrivia
                    (
                        SyntaxFactory
                            .TriviaList
                            (
                                SyntaxFactory.EndOfLine(lineEnding),
                                SyntaxFactory.Whitespace(commentIndentation),
                                lineComments[0],
                                SyntaxFactory.EndOfLine(lineEnding),
                                SyntaxFactory.Whitespace(commentIndentation)
                            )
                    );

            newBaseList =
                newBaseList
                    .WithColonToken(colonWithComment);
        }

        // Add remaining line comments before the comma separators
        if (lineComments.Count > 1 && baseList.Types.Count > 1)
        {
            var separators =
                baseList.Types
                    .GetSeparators()
                    .ToArray();

            var newSeparators =
                new SyntaxToken[separators.Length];

            for (var i = 0; i < separators.Length && i + 1 < lineComments.Count; i++)
            {
                newSeparators[i] =
                    separators[i]
                        .WithLeadingTrivia
                        (
                            SyntaxFactory.TriviaList
                            (
                                SyntaxFactory.EndOfLine(lineEnding),
                                SyntaxFactory.Whitespace(commentIndentation),
                                lineComments[i + 1],
                                SyntaxFactory.EndOfLine(lineEnding),
                                SyntaxFactory.Whitespace(commentIndentation)
                            )
                        );
            }

            // Use remaining separators as-is
            for (var i = lineComments.Count - 1; i < separators.Length; i++)
            {
                newSeparators[i] =
                    separators[i];
            }

            var newTypesList =
                SyntaxFactory
                    .SeparatedList(baseList.Types, newSeparators);

            newBaseList =
                newBaseList
                    .WithTypes(newTypesList);
        }

        return newBaseList;
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

        // Handle inheritance/interface formatting
        var baseList =
            root.FindToken(diagnosticSpan.Start)
                .Parent?
                    .AncestorsAndSelf()
                    .OfType<BaseListSyntax>()
                    .First();

        // Check for non-whitespace trivia and handle according to configuration
        if (baseList is null || await ShouldSkipCodeFixAsync(context.Document, baseList).ConfigureAwait(false))
        {
            // Skip this code fix - analyzer will still report the diagnostic
            return;
        }

        context
            .RegisterCodeFix
            (
                CodeAction.Create
                (
                    title: CodeFixResources.InheritanceCodeFixTitle,
                    createChangedDocument: x => FormatInheritanceDocumentAsync(context.Document, root, baseList, x),
                    equivalenceKey: nameof(CodeFixResources.InheritanceCodeFixTitle)
                ),
                diagnostic
            );
    }

    private async Task<Document> FormatInheritanceDocumentAsync
        (
        Document document,
        SyntaxNode root,
        BaseListSyntax baseList,
        CancellationToken cancellationToken
        )
    {
        var sourceText =
            await document
                .GetTextAsync(cancellationToken)
                .ConfigureAwait(false);

        // Delegate all formatting logic to the service
        var (newRoot, hasChanges) =
            await FormatInheritanceWithContextAsync(document, root, baseList, sourceText)
                .ConfigureAwait(false);

        return hasChanges
            ? document.WithSyntaxRoot(newRoot)
            : document;
    }

    #endregion
}
