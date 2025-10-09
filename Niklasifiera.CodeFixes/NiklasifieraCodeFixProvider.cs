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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            context.Diagnostics.First();

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
                        createChangedDocument: c => FormatSignatureAsync(context.Document, parameterList, c),
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
                        createChangedDocument: c => FormatInheritanceAsync(context.Document, baseList, c),
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
                FormatMultipleLines(parameterList, sourceText);
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
        BaseListSyntax newBaseList =
            FormatInheritanceMultipleLines(baseList, sourceText)
                .WithTrailingTrivia(baseList.GetTrailingTrivia());

        var newRoot =
            root.ReplaceNode(baseList, newBaseList);

        // Apply formatting to ensure proper spacing and line breaks
        var formattedRoot = Formatter.Format(newRoot, baseList.Span, document.Project.Solution.Workspace);

        return document
            .WithSyntaxRoot(formattedRoot);
    }

    private ParameterListSyntax FormatSingleLine(ParameterListSyntax parameterList)
    {
        // Remove all trivia and format on single line
        var newParameters =
            SyntaxFactory
                .SeparatedList
                (
                    parameterList.Parameters.Select(p => p.WithoutTrivia()),
                    parameterList.Parameters.GetSeparators().Select(s => s.WithoutTrivia())
                );

        return SyntaxFactory
            .ParameterList(newParameters)
            .WithOpenParenToken(parameterList.OpenParenToken.WithoutTrivia())
            .WithCloseParenToken(parameterList.CloseParenToken.WithoutTrivia());
    }

    private ParameterListSyntax FormatMultipleLines
        (
        ParameterListSyntax parameterList,
        SourceText sourceText
        )
    {
        // Find the parent declaration to determine proper indentation context
        var parentNode =
            parameterList.Parent;

        var parentIndentation =
            GetDeclarationIndentation(parentNode, sourceText);

        // Create indentation for opening/closing parens and parameters
        var parenIndentation =
            parentIndentation + "    ";

        var parameterIndentation =
            parentIndentation + "    ";

        // Build new parameter list with proper formatting
        var separators =
            new SyntaxToken[parameterList.Parameters.Count - 1];

        for (int i = 0; i < separators.Length; i++)
        {
            separators[i] =
                SyntaxFactory
                    .Token
                    (
                        SyntaxFactory.TriviaList(),
                        SyntaxKind.CommaToken,
                        SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed)
                    );
        }

        var newParameters =
            new ParameterSyntax[parameterList.Parameters.Count];

        for (int i = 0; i < parameterList.Parameters.Count; i++)
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
                .Any(t => t.IsKind(SyntaxKind.OpenBraceToken));

        SyntaxTriviaList finalCloseParenTrivia;

        if (hasInlineBrace)
        {
            // Remove any inline braces and add proper newline
            var filteredTrivia =
                closeParenTrailingTrivia
                    .Where(t => !t.IsKind(SyntaxKind.OpenBraceToken));

            finalCloseParenTrivia =
                SyntaxFactory
                    .TriviaList
                    (
                        filteredTrivia
                            .Concat([SyntaxFactory.CarriageReturnLineFeed])
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
                            SyntaxFactory.CarriageReturnLineFeed,
                            SyntaxFactory.Whitespace(parenIndentation)
                        ),
                    SyntaxKind.OpenParenToken,
                    SyntaxFactory
                        .TriviaList(SyntaxFactory.CarriageReturnLineFeed)
                );

        // Closing paren on its own line with same indentation as opening paren, with proper trailing trivia
        var closeParen =
            SyntaxFactory
                .Token
                (
                    SyntaxFactory
                        .TriviaList
                        (
                            SyntaxFactory.CarriageReturnLineFeed,
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
        var sb =
            new StringBuilder();

        foreach (char c in line)
        {
            if (c is ' ' or '\t')
            {
                _ = sb.Append(c);
            }
            else
            {
                break;
            }
        }

        return sb
            .ToString();
    }

    private BaseListSyntax FormatInheritanceMultipleLines
        (
        BaseListSyntax baseList,
        SourceText sourceText
        )
    {
        // Find the parent type declaration to determine proper indentation
        var parentType =
            baseList.Parent as TypeDeclarationSyntax;

        var parentIndentation =
            GetDeclarationIndentation(parentType, sourceText);

        // Create indentation for types
        var typeIndentation =
            parentIndentation + "    ";

        // Build new type list with proper formatting - leading commas except for first type
        var separators =
            new SyntaxToken[baseList.Types.Count - 1];

        for (int i = 0; i < separators.Length; i++)
        {
            // Leading comma with newline and indentation before it
            separators[i] =
                SyntaxFactory
                    .Token
                    (
                        SyntaxFactory
                            .TriviaList
                            (
                                SyntaxFactory.CarriageReturnLineFeed,
                                SyntaxFactory.Whitespace(typeIndentation)
                            ),
                        SyntaxKind.CommaToken,
                        SyntaxFactory.TriviaList(SyntaxFactory.Space)
                    );
        }

        var newTypes =
            new BaseTypeSyntax[baseList.Types.Count];

        for (int i = 0; i < baseList.Types.Count; i++)
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
                            SyntaxFactory.CarriageReturnLineFeed,
                            SyntaxFactory.Whitespace(typeIndentation)
                        ),
                    SyntaxKind.ColonToken,
                    SyntaxFactory.TriviaList()
                );

        return SyntaxFactory
            .BaseList(colon, newTypesList);
    }
}
