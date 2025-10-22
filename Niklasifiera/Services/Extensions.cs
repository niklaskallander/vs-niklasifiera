namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Linq;

public static class Extensions
{
    public static int GetDeclarationStartLine
        (
        this SourceText sourceText,
        SyntaxNode parentDeclaration
        )
        => parentDeclaration switch
        {
            MethodDeclarationSyntax method
                // For methods, the declaration starts at the method identifier (after return type, modifiers, generics)
                => sourceText
                    .GetLineNumberFor(method.Identifier),

            ConstructorDeclarationSyntax constructor
                // For constructors, the declaration starts at the constructor identifier
                => sourceText
                    .GetLineNumberFor(constructor.Identifier),

            TypeDeclarationSyntax type
                // For primary constructors, the declaration starts at the type identifier
                => sourceText
                    .GetLineNumberFor(type.Identifier),
            _
                // Fallback: use the start of the parent declaration
                => sourceText
                    .GetLineNumberFor(parentDeclaration),
        };

    public static string GetIndentation
        (
        this SourceText sourceText,
        int lineStart
        )
    {
        var linePosition =
            sourceText.Lines
                .GetLinePosition(lineStart);

        var line =
            sourceText.Lines[linePosition.Line];

        return sourceText
            .ToString(line.Span)
            .GetIndentation();
    }

    public static string GetIndentation(this string line)
    {
        var indentation = "";

        foreach (var character in line)
        {
            if (character is ' ' or '\t')
            {
                indentation += character;
            }
            else
            {
                break;
            }
        }

        return indentation;
    }

    public static int GetLineNumberFor
        (
        this SourceText sourceText,
        int spanStart
        )
        => sourceText.Lines
            .GetLineFromPosition(spanStart)
            .LineNumber;

    public static int GetLineNumberFor
        (
        this SourceText sourceText,
        SyntaxToken token
        )
        => sourceText
            .GetLineNumberFor(token.SpanStart);

    public static int GetLineNumberFor
        (
        this SourceText sourceText,
        SyntaxNode node
        )
        => sourceText
            .GetLineNumberFor(node.SpanStart);

    public static string GetLineIndentationFor
        (
        this SourceText sourceText,
        int lineNumber
        )
        => sourceText.Lines[lineNumber]
            .ToString()
            .GetIndentation();

    public static bool IsComment(this SyntaxTrivia trivia)
        => trivia.Kind() switch
        {
            SyntaxKind.SingleLineCommentTrivia or
            SyntaxKind.MultiLineCommentTrivia or
            SyntaxKind.SingleLineDocumentationCommentTrivia or
            SyntaxKind.MultiLineDocumentationCommentTrivia or
            SyntaxKind.DocumentationCommentExteriorTrivia
                => true,
            _
                => false
        };

    public static bool IsPreprocessorDirective(this SyntaxTrivia trivia)
        => trivia.Kind() switch
        {
            SyntaxKind.IfDirectiveTrivia or
            SyntaxKind.ElseDirectiveTrivia or
            SyntaxKind.ElifDirectiveTrivia or
            SyntaxKind.EndIfDirectiveTrivia or
            SyntaxKind.DefineDirectiveTrivia or
            SyntaxKind.UndefDirectiveTrivia or
            SyntaxKind.RegionDirectiveTrivia or
            SyntaxKind.EndRegionDirectiveTrivia or
            SyntaxKind.LineDirectiveTrivia or
            SyntaxKind.PragmaWarningDirectiveTrivia or
            SyntaxKind.PragmaChecksumDirectiveTrivia or
            SyntaxKind.ReferenceDirectiveTrivia or
            SyntaxKind.LoadDirectiveTrivia or
            SyntaxKind.ShebangDirectiveTrivia or
            SyntaxKind.NullableDirectiveTrivia
                => true,
            _
                => false
        };

    public static SyntaxToken WithPreservedTrivia
        (
        this SyntaxToken originalToken,
        SyntaxTriviaList newLeadingWhitespace,
        SyntaxTriviaList newTrailingWhitespace
        )
        => originalToken
            .WithLeadingTrivia(newLeadingWhitespace.CombinedWith(originalToken.LeadingTrivia))
            .WithTrailingTrivia(newTrailingWhitespace.CombinedWith(originalToken.TrailingTrivia));

    public static T WithPreservedTrivia<T>
        (
        this T originalNode,
        SyntaxTriviaList newLeadingWhitespace,
        SyntaxTriviaList newTrailingWhitespace
        )
        where T : SyntaxNode
        => originalNode
            .WithLeadingTrivia(newLeadingWhitespace.CombinedWith(originalNode.GetLeadingTrivia()))
            .WithTrailingTrivia(newTrailingWhitespace.CombinedWith(originalNode.GetTrailingTrivia()));

    public static SyntaxTriviaList CombinedWith
        (
        this SyntaxTriviaList target,
        SyntaxTriviaList other
        )
    {
        var preserved =
            PreserveNonWhitespaceTrivia(other);

        var combined =
            target.AddRange(preserved);

        return combined;
    }

    public static bool ContainsNonWhitespaceTrivia(this SyntaxNode node)
        => node
            .DescendantTrivia(descendIntoTrivia: true)
            .Any(IsNonWhitespaceTrivia);

    public static bool IsNonWhitespaceTrivia(this SyntaxTrivia trivia)
        => trivia.Kind() switch
        {
            SyntaxKind.WhitespaceTrivia or SyntaxKind.EndOfLineTrivia => false,
            _ => true
        };

    public static SyntaxTriviaList PreserveNonWhitespaceTrivia(this SyntaxTriviaList originalTrivia)
    {
        var preservedTrivia =
            originalTrivia
                .Where(IsNonWhitespaceTrivia);

        return SyntaxFactory.TriviaList(preservedTrivia);
    }
}
