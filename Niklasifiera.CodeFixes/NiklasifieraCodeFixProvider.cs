namespace Niklasifiera;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Immutable;
using System.Composition;
using System.Text;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NiklasifieraCodeFixProvider)), Shared]
public class NiklasifieraCodeFixProvider
    : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [NiklasifieraAnalyzer.SignatureDiagnosticId, NiklasifieraAnalyzer.InheritanceDiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root =
            await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

        var diagnostic =
            context.Diagnostics
                .First();

        var diagnosticSpan =
            diagnostic.Location.SourceSpan;

        if (diagnostic.Id == NiklasifieraAnalyzer.SignatureDiagnosticId)
        {
            // Handle parameter list formatting
            var parameterList =
                root.FindToken(diagnosticSpan.Start)
                    .Parent
                        .AncestorsAndSelf()
                        .OfType<ParameterListSyntax>()
                        .First();

            context
                .RegisterCodeFix
                (
                    CodeAction.Create
                    (
                        title: CodeFixResources.CodeFixTitle,
                        createChangedDocument: x => FormatSignatureAsync(context.Document, parameterList, x),
                        equivalenceKey: nameof(CodeFixResources.CodeFixTitle)
                    ),
                    diagnostic
                );
        }
        else if (diagnostic.Id == NiklasifieraAnalyzer.InheritanceDiagnosticId)
        {
            // Handle inheritance/interface formatting
            var baseList =
                root.FindToken(diagnosticSpan.Start)
                    .Parent
                        .AncestorsAndSelf()
                        .OfType<BaseListSyntax>()
                        .First();

            context
                .RegisterCodeFix
                (
                    CodeAction.Create
                    (
                        title: CodeFixResources.InheritanceCodeFixTitle,
                        createChangedDocument: x => FormatInheritanceAsync(context.Document, baseList, x),
                        equivalenceKey: nameof(CodeFixResources.InheritanceCodeFixTitle)
                    ),
                    diagnostic
                );
        }
    }

    private async Task<Document> FormatSignatureAsync
        (
        Document document,
        ParameterListSyntax parameterList,
        CancellationToken cancellationToken
        )
    {
        var root =
            await document
                .GetSyntaxRootAsync(cancellationToken)
                .ConfigureAwait(false);

        var sourceText =
            await document
                .GetTextAsync(cancellationToken)
                .ConfigureAwait(false);

        var parameterCount =
            parameterList.Parameters.Count;

        ParameterListSyntax newParameterList;

        if (parameterCount <= 1)
        {
            // Format on a single line
            newParameterList =
                FormatSingleLine(parameterList);
        }
        else
        {
            // Format on multiple lines
            newParameterList =
                await FormatMultipleLinesAsync(parameterList, sourceText, document);
        }

        var newRoot =
            root.ReplaceNode(parameterList, newParameterList);

        return document
            .WithSyntaxRoot(newRoot);
    }

    private async Task<Document> FormatInheritanceAsync
        (
        Document document,
        BaseListSyntax baseList,
        CancellationToken cancellationToken
        )
    {
        var root =
            await document
                .GetSyntaxRootAsync(cancellationToken)
                .ConfigureAwait(false);

        var sourceText =
            await document
                .GetTextAsync(cancellationToken)
                .ConfigureAwait(false);

        // Always format inheritance on separate lines (both single and multiple inheritance)
        var newBaseList =
            await FormatInheritanceMultipleLinesAsync(baseList, sourceText, document);

        newBaseList =
            newBaseList
                .WithTrailingTrivia(baseList.GetTrailingTrivia());

        // Clean up trailing whitespace from the parent type declaration and ensure proper line breaks
        var newRoot =
            root.ReplaceNode(baseList, newBaseList);

        if (baseList.Parent is TypeDeclarationSyntax parentType)
        {
            // Find the parent type in the new tree
            var newParentType =
                newRoot
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .FirstOrDefault(x => x.Identifier.ValueText == parentType.Identifier.ValueText);

            if (newParentType != null)
            {
                // Check if this type has a parameter list (primary constructor)
                var hasParameterList =
                    newParentType.ParameterList != null;

                SyntaxToken cleanedIdentifier;

                if (hasParameterList)
                {
                    // For primary constructors, only remove trailing whitespace, preserve newlines
                    cleanedIdentifier =
                        CleanTrailingWhitespace(newParentType.Identifier);
                }
                else
                {
                    // For regular classes, remove all trailing trivia to ensure clean formatting
                    cleanedIdentifier =
                        CleanTrailingWhitespace(newParentType.Identifier)
                            .WithTrailingTrivia(SyntaxFactory.TriviaList());
                }

                var updatedParentType =
                    newParentType
                        .WithIdentifier(cleanedIdentifier);

                newRoot =
                    newRoot
                        .ReplaceNode(newParentType, updatedParentType);
            }
        }

        return document
            .WithSyntaxRoot(newRoot);
    }

    private ParameterListSyntax FormatSingleLine(ParameterListSyntax parameterList)
    {
        // Remove all trivia and format on single line
        var newParameters =
            SyntaxFactory
                .SeparatedList
                (
                    parameterList.Parameters
                        .Select(x => x.WithoutTrivia()),
                    parameterList.Parameters
                        .GetSeparators()
                        .Select(x => x.WithoutTrivia())
                );

        return SyntaxFactory
            .ParameterList(newParameters)
            .WithOpenParenToken(parameterList.OpenParenToken.WithoutTrivia())
            .WithCloseParenToken(parameterList.CloseParenToken.WithoutTrivia());
    }

    private async Task<ParameterListSyntax> FormatMultipleLinesAsync
        (
        ParameterListSyntax parameterList,
        SourceText sourceText,
        Document document
        )
    {
        // Find the parent declaration to determine proper indentation context
        var parentNode =
            parameterList.Parent;

        var parentIndentation =
            GetDeclarationIndentation(parentNode, sourceText);

        // Get the indentation unit (spaces or tabs) from settings
        var indentationUnit =
            await GetIndentationUnitAsync(document, sourceText);

        // Create indentation for opening/closing parens and parameters
        var parenIndentation =
            parentIndentation + indentationUnit;

        var parameterIndentation =
            parentIndentation + indentationUnit;

        // Get the appropriate line ending
        var lineEndingTrivia =
            await GetLineEndingTriviaAsync(document, sourceText);

        // Build new parameter list with proper formatting
        var separators =
            new SyntaxToken[parameterList.Parameters.Count - 1];

        for (var i = 0; i < separators.Length; i++)
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

        var newParameters =
            new ParameterSyntax[parameterList.Parameters.Count];

        for (var i = 0; i < parameterList.Parameters.Count; i++)
        {
            // Add leading indentation
            var param =
                parameterList.Parameters[i]
                    .WithoutTrivia()
                    .WithLeadingTrivia
                    (
                        SyntaxFactory
                            .TriviaList
                            (
                                SyntaxFactory
                                    .Whitespace(parameterIndentation)
                            )
                    );

            newParameters[i] =
                param;
        }

        var newParametersList =
            SyntaxFactory
                .SeparatedList(newParameters, separators);

        // Preserve any trailing trivia from the closing paren, but ensure proper brace formatting
        var closeParenTrailingTrivia =
            parameterList.CloseParenToken.TrailingTrivia;

        // If there's a brace in the trailing trivia, we need to fix its placement
        var hasInlineBrace =
            closeParenTrailingTrivia
                .Any(x => x.IsKind(SyntaxKind.OpenBraceToken));

        SyntaxTriviaList finalCloseParenTrivia;

        if (hasInlineBrace)
        {
            // Remove any inline braces and add proper newline
            var filteredTrivia =
                closeParenTrailingTrivia
                    .Where(x => !x.IsKind(SyntaxKind.OpenBraceToken));

            finalCloseParenTrivia =
                SyntaxFactory
                    .TriviaList
                    (
                        filteredTrivia
                            .Concat([lineEndingTrivia])
                    );
        }
        else
        {
            finalCloseParenTrivia =
                closeParenTrailingTrivia;
        }

        // Opening paren on new line with indentation
        var openParen =
            SyntaxFactory
                .Token
                (
                    SyntaxFactory
                        .TriviaList
                        (
                            lineEndingTrivia,
                            SyntaxFactory.Whitespace(parenIndentation)
                        ),
                    SyntaxKind.OpenParenToken,
                    SyntaxFactory
                        .TriviaList(lineEndingTrivia)
                );

        // Closing paren on its own line with same indentation as opening paren, with proper trailing trivia
        var closeParen =
            SyntaxFactory
                .Token
                (
                    SyntaxFactory
                        .TriviaList
                        (
                            lineEndingTrivia,
                            SyntaxFactory.Whitespace(parenIndentation)
                        ),
                    SyntaxKind.CloseParenToken,
                    finalCloseParenTrivia
                );

        return SyntaxFactory
            .ParameterList
            (
                openParen,
                newParametersList,
                closeParen
            );
    }

    private string GetDeclarationIndentation
        (
        SyntaxNode parentNode,
        SourceText sourceText
        )
    {
        // Use the indentation of the line with the method identifier
        var declarationLineStart =
            parentNode switch
            {
                // Use the indentation of the line with the method identifier
                MethodDeclarationSyntax method
                    => method.Identifier.SpanStart,

                // Use the indentation of the line with the constructor identifier
                ConstructorDeclarationSyntax constructor
                    => constructor.Identifier.SpanStart,

                // For primary constructors, use the indentation of the line with the type identifier
                TypeDeclarationSyntax type
                    => type.Identifier.SpanStart,

                // Fallback: use the start of the parent node
                _
                    => parentNode.SpanStart,
            };

        var declarationLine =
            sourceText.Lines
                .GetLineFromPosition(declarationLineStart);

        return GetIndentation(declarationLine.ToString());
    }

    private string GetIndentation(string line)
    {
        var stringBuilder =
            new StringBuilder();

        foreach (var character in line)
        {
            if (character is ' ' or '\t')
            {
                _ = stringBuilder
                    .Append(character);
            }
            else
            {
                break;
            }
        }

        return stringBuilder
            .ToString();
    }

    private async Task<BaseListSyntax> FormatInheritanceMultipleLinesAsync
        (
        BaseListSyntax baseList,
        SourceText sourceText,
        Document document
        )
    {
        // Find the parent type declaration to determine proper indentation
        var parentType =
            baseList.Parent as TypeDeclarationSyntax;

        var parentIndentation =
            GetDeclarationIndentation(parentType, sourceText);

        // Get the indentation unit (spaces or tabs) from settings
        var indentationUnit =
            await GetIndentationUnitAsync(document, sourceText);

        // Create indentation for types
        var typeIndentation =
            parentIndentation + indentationUnit;

        // Get the appropriate line ending
        var lineEndingTrivia =
            await GetLineEndingTriviaAsync(document, sourceText);

        // Build new type list with proper formatting - leading commas except for first type
        var separators =
            new SyntaxToken[baseList.Types.Count - 1];

        for (var i = 0; i < separators.Length; i++)
        {
            // Leading comma with newline and indentation before it
            separators[i] =
                SyntaxFactory
                    .Token
                    (
                        SyntaxFactory
                            .TriviaList
                            (
                                lineEndingTrivia,
                                SyntaxFactory.Whitespace(typeIndentation)
                            ),
                        SyntaxKind.CommaToken,
                        SyntaxFactory.TriviaList(SyntaxFactory.Space)
                    );
        }

        var newTypes =
            new BaseTypeSyntax[baseList.Types.Count];

        for (var i = 0; i < baseList.Types.Count; i++)
        {
            var type =
                baseList.Types[i]
                    .WithoutTrivia();

            if (i == 0)
            {
                // First type gets a space after the colon
                type =
                    type.WithLeadingTrivia(SyntaxFactory.Space);
            }
            else
            {
                // Other types don't need leading trivia (the comma separator handles it)
                type =
                    type.WithoutLeadingTrivia();
            }

            newTypes[i] =
                type;
        }

        var newTypesList =
            SyntaxFactory
                .SeparatedList(newTypes, separators);

        // Colon should be on its own line with proper indentation
        var colon =
            SyntaxFactory
                .Token
                (
                    SyntaxFactory
                        .TriviaList
                        (
                            lineEndingTrivia,
                            SyntaxFactory.Whitespace(typeIndentation)
                        ),
                    SyntaxKind.ColonToken,
                    SyntaxFactory.TriviaList()
                );

        return SyntaxFactory
            .BaseList(colon, newTypesList);
    }

    private async Task<SyntaxTrivia> GetLineEndingTriviaAsync
        (
        Document document,
        SourceText sourceText
        )
    {
        // First try to get line ending from .editorconfig
        var syntaxTree =
            await document
                .GetSyntaxTreeAsync()
                .ConfigureAwait(false);

        if (syntaxTree != null)
        {
            var options =
                document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider
                    .GetOptions(syntaxTree);

            if (options.TryGetValue("end_of_line", out var endOfLineValue))
            {
                return endOfLineValue switch
                {
                    "lf" => SyntaxFactory.LineFeed,
                    "crlf" => SyntaxFactory.CarriageReturnLineFeed,
                    "cr" => SyntaxFactory.CarriageReturn,
                    _ => DetectLineEndingFromSource(sourceText)
                };
            }
        }

        // Fallback: detect from existing source
        return DetectLineEndingFromSource(sourceText);
    }

    private SyntaxTrivia DetectLineEndingFromSource(SourceText sourceText)
    {
        var text =
            sourceText.ToString();

        // Check for CRLF first (most specific)
        if (text.Contains("\r\n"))
        {
            return SyntaxFactory.CarriageReturnLineFeed;
        }

        // Check for LF
        if (text.Contains("\n"))
        {
            return SyntaxFactory.LineFeed;
        }

        // Check for CR (least common)
        if (text.Contains("\r"))
        {
            return SyntaxFactory.CarriageReturn;
        }

        // Default to LF if no line endings found
        return SyntaxFactory.LineFeed;
    }

    private async Task<string> GetIndentationUnitAsync
        (
        Document document,
        SourceText sourceText
        )
    {
        // First try to get indentation settings from .editorconfig
        var syntaxTree =
            await document
                .GetSyntaxTreeAsync()
                .ConfigureAwait(false);

        if (syntaxTree == null)
        {
            // Fallback: detect indentation from existing source code
            return DetectIndentationFromSource(sourceText);
        }

        var options =
                document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider
                    .GetOptions(syntaxTree);

        // Check indent_style (tab or space)
        var useTab = false;

        if (options.TryGetValue("indent_style", out var indentStyle))
        {
            useTab =
                indentStyle
                    .Equals("tab", StringComparison.OrdinalIgnoreCase);
        }

        // Return the appropriate indentation unit
        if (useTab)
        {
            return "\t";
        }

        // Check indent_size
        var indentSize = 4; // Default fallback

        if (options.TryGetValue("indent_size", out var indentSizeValue))
        {
            if (int.TryParse(indentSizeValue, out var parsedSize) && parsedSize > 0)
            {
                indentSize = parsedSize;
            }
        }

        return new string(' ', indentSize);
    }

    private string DetectIndentationFromSource(SourceText sourceText)
    {
        var lines = sourceText.Lines;
        var spaceCounts = new Dictionary<int, int>();
        var hasTabIndentation = false;

        foreach (var line in lines)
        {
            var lineText =
                line.ToString();

            if (string.IsNullOrWhiteSpace(lineText))
            {
                continue;
            }

            var indentationLength = 0;

            foreach (var ch in lineText)
            {
                if (ch == ' ')
                {
                    indentationLength++;
                    continue;
                }

                if (ch == '\t')
                {
                    return "\t";
                }

                break;
            }

            // Count space-based indentation levels
            if (indentationLength > 0)
            {
                if (spaceCounts.ContainsKey(indentationLength))
                {
                    spaceCounts[indentationLength]++;
                }
                else
                {
                    spaceCounts[indentationLength] = 1;
                }
            }
        }

        // If we detected tab usage anywhere, prefer tabs
        if (hasTabIndentation)
        {
            return "\t";
        }

        // Find the most common indentation level that's likely a single unit
        var commonIndentations =
            spaceCounts
                .Where(x => x.Key <= 8) // Reasonable indentation sizes
                .OrderByDescending(x => x.Value)
                .ToList();

        if (commonIndentations.Any())
        {
            // Look for patterns that suggest indent unit size
            var possibleUnits =
                new[] { 2, 3, 4, 8 };

            foreach (var unit in possibleUnits)
            {
                if (commonIndentations.Any(x => x.Key == unit || x.Key % unit == 0))
                {
                    return new string(' ', unit);
                }
            }

            // Fall back to the most common indentation level
            return new string(' ', commonIndentations.First().Key);
        }

        // Ultimate fallback: 4 spaces
        return "    ";
    }

    private SyntaxToken CleanTrailingWhitespace(SyntaxToken token)
    {
        // Remove trailing whitespace from the token's trailing trivia
        var cleanedTrivia =
            token.TrailingTrivia
                .Where(x => !x.IsKind(SyntaxKind.WhitespaceTrivia))
                .ToList();

        return token
            .WithTrailingTrivia(cleanedTrivia);
    }
}
