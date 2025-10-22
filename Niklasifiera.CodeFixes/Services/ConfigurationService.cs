namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;

/// <summary>
/// Service for resolving configuration settings from .editorconfig and other sources.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the trivia handling behavior for the given document.
    /// </summary>
    Task<TriviaHandlingBehavior> GetTriviaHandlingBehaviorAsync(Document document);

    /// <summary>
    /// Gets the line ending style for the given document.
    /// </summary>
    Task<string> GetLineEndingAsync(Document document);

    /// <summary>
    /// Gets the indentation unit (spaces or tabs) for the given document.
    /// </summary>
    Task<string> GetIndentationUnitAsync(Document document);
}

/// <summary>
/// Default implementation of IConfigurationService that delegates to the shared ConfigurationReader.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    public async Task<TriviaHandlingBehavior> GetTriviaHandlingBehaviorAsync(Document document)
    {
        var syntaxTree =
            await document
                .GetSyntaxTreeAsync()
                .ConfigureAwait(false);

        if (syntaxTree == null)
        {
            return TriviaHandlingBehavior.Skip; // Default to safe behavior
        }

        return ConfigurationReader.GetTriviaHandlingBehavior
        (
            syntaxTree,
            document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider
        );
    }

    public async Task<string> GetLineEndingAsync(Document document)
    {
        var syntaxTree =
            await document
                .GetSyntaxTreeAsync()
                .ConfigureAwait(false);

        if (syntaxTree == null)
        {
            return Environment.NewLine;
        }

        return ConfigurationReader.GetLineEnding
        (
            syntaxTree,
            document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider
        );
    }

    public async Task<string> GetIndentationUnitAsync(Document document)
    {
        var syntaxTree =
            await document
                .GetSyntaxTreeAsync()
                .ConfigureAwait(false);

        if (syntaxTree == null)
        {
            return "    "; // Default to 4 spaces
        }

        return ConfigurationReader.GetIndentationUnit
        (
            syntaxTree,
            document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider
        );
    }
}
