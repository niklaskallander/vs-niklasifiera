#nullable enable

namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Specifies how trivia (comments, directives) should be handled during code fixes.
/// </summary>
public enum TriviaHandlingBehavior
{
    /// <summary>
    /// Skip code fixes when non-whitespace trivia is present (safe default).
    /// </summary>
    Skip,

    /// <summary>
    /// Apply code fixes while preserving and intelligently repositioning trivia.
    /// </summary>
    Preserve
}

/// <summary>
/// Static service for reading configuration from .editorconfig and detecting patterns from source code.
/// </summary>
public static class ConfigurationReader
{
    public static TriviaHandlingBehavior GetTriviaHandlingBehavior
        (
        SyntaxTree syntaxTree,
        AnalyzerConfigOptionsProvider optionsProvider
        )
    {
        var options =
            optionsProvider
                .GetOptions(syntaxTree);

        if (options.TryGetValue("niklasifiera_preserve_trivia", out var value))
        {
            return value.ToLowerInvariant() switch
            {
                "skip" => TriviaHandlingBehavior.Skip,
                "preserve" => TriviaHandlingBehavior.Preserve,
                _ => TriviaHandlingBehavior.Skip // Default for invalid values
            };
        }

        // Default behavior if not specified
        return TriviaHandlingBehavior.Skip;
    }

    public static string GetLineEnding
        (
        SyntaxTree syntaxTree,
        AnalyzerConfigOptionsProvider optionsProvider
        )
        => DetectLineEndingFromConfig(syntaxTree, optionsProvider)
        ?? DetectLineEndingFromSource(syntaxTree.GetText());

    public static string GetIndentationUnit
        (
        SyntaxTree syntaxTree,
        AnalyzerConfigOptionsProvider optionsProvider
        )
        => DetectIndentationFromConfig(syntaxTree, optionsProvider)
        ?? DetectIndentationFromSource(syntaxTree.GetText());

    private static string? DetectLineEndingFromConfig
        (
        SyntaxTree syntaxTree,
        AnalyzerConfigOptionsProvider optionsProvider
        )
    {
        var options =
            optionsProvider
                .GetOptions(syntaxTree);

        if (!options.TryGetValue("end_of_line", out var endOfLineValue))
        {
            return null;
        }

        return endOfLineValue?.ToLowerInvariant() switch
        {
            "lf" => "\n",
            "crlf" => "\r\n",
            "cr" => "\r",
            _ => null
        };
    }

    private static string? DetectIndentationFromConfig
        (
        SyntaxTree syntaxTree,
        AnalyzerConfigOptionsProvider optionsProvider
        )
    {
        var options =
            optionsProvider
                .GetOptions(syntaxTree);

        return DetectPotentialTabIndentationFor(options)
            ?? DetectPotentialSpaceIndentationFor(options);
    }

    private static string? DetectPotentialSpaceIndentationFor(AnalyzerConfigOptions options)
    {
        // Get indent size for spaces
        if (options.TryGetValue("indent_size", out var indentSizeValue))
        {
            if (int.TryParse(indentSizeValue, out var parsedSize) && parsedSize > 0)
            {
                return new string(' ', parsedSize);
            }
        }

        return null;
    }

    private static string? DetectPotentialTabIndentationFor(AnalyzerConfigOptions options)
    {
        // Check for tab vs space preference
        if (!options.TryGetValue("indent_style", out var indentStyle))
        {
            return null;
        }

        var useTab =
            indentStyle?
                .Equals("tab", StringComparison.OrdinalIgnoreCase) == true;

        if (useTab)
        {
            return "\t";
        }

        return null;
    }

    private static string DetectLineEndingFromSource(SourceText sourceText)
    {
        var text =
            sourceText.ToString();

        if (text.Contains("\r\n"))
        {
            return "\r\n";
        }

        if (text.Contains("\n"))
        {
            return "\n";
        }

        if (text.Contains("\r"))
        {
            return "\r";
        }

        // Default to \r\n (most common on Windows where VS runs)
        return "\r\n";
    }

    private static string DetectIndentationFromSource(SourceText sourceText)
    {
        var spaceCounts =
            CollectIndentationData(sourceText);

        return AnalyzeIndentationPattern(spaceCounts);
    }

    private static Dictionary<int, int> CollectIndentationData(SourceText sourceText)
    {
        var lines =
            sourceText.Lines
                .Take(100); // Check first 100 lines for performance

        var spaceCounts = new Dictionary<int, int>();

        foreach (var line in lines)
        {
            var (spaceCount, hasTabs) =
                ProcessLineIndentation(sourceText, line);

            if (hasTabs)
            {
                // Early return for tab detection - tabs take precedence
                return new Dictionary<int, int> { { -1, 1 } }; // Special marker for tabs
            }

            if (spaceCount > 0)
            {
                RecordSpaceIndentation(spaceCounts, spaceCount);
            }
        }

        return spaceCounts;
    }

    private static (int SpaceCount, bool HasTabs) ProcessLineIndentation
        (
        SourceText sourceText,
        TextLine line
        )
    {
        if (line.Span.Length == 0)
        {
            return (0, false);
        }

        var lineText =
            sourceText
                .ToString(line.Span);

        var spaceCount = 0;

        foreach (var character in lineText)
        {
            if (character == ' ')
            {
                spaceCount++;

                continue;
            }

            if (character == '\t')
            {
                return (0, true); // Found tab - return immediately
            }

            break; // Hit non-whitespace character
        }

        return (spaceCount, false);
    }

    private static void RecordSpaceIndentation
        (
        Dictionary<int, int> spaceCounts,
        int indentationLength
        )
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

    private static string AnalyzeIndentationPattern(Dictionary<int, int> spaceCounts)
    {
        // Check for tab marker
        if (spaceCounts.ContainsKey(-1))
        {
            return "\t";
        }

        var commonIndentations =
            FindCommonIndentationLevels(spaceCounts);

        if (commonIndentations.Any())
        {
            var detectedUnit =
                DetectIndentationUnit(commonIndentations);

            if (detectedUnit > 0)
            {
                return new string(' ', detectedUnit);
            }
        }

        // Default to 4 spaces (consistent with C# conventions)
        return "    ";
    }

    private static List<KeyValuePair<int, int>> FindCommonIndentationLevels(Dictionary<int, int> spaceCounts)
        => spaceCounts
            .Where(x => x.Key <= 8) // Reasonable indentation sizes
            .OrderByDescending(x => x.Value)
            .ToList();

    private static int DetectIndentationUnit(List<KeyValuePair<int, int>> commonIndentations)
    {
        // Look for patterns that suggest indent unit size
        var possibleUnits =
            new[] { 4, 2, 3, 8 }; // Prefer 4 spaces first

        foreach (var unit in possibleUnits)
        {
            if (commonIndentations.Any(x => x.Key % unit == 0))
            {
                return unit;
            }
        }

        // Check if we have a clear single indentation level
        var firstIndent =
            commonIndentations
                .First().Key;

        return firstIndent is <= 8 and > 0
            ? firstIndent
            : 0;
    }
}
