namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// Interface for code fix services that can register themselves with a CodeFixContext.
/// </summary>
public interface ICodeFixService
{
    /// <summary>
    /// Gets the diagnostic ID that this code fix service handles.
    /// </summary>
    string DiagnosticId { get; }

    /// <summary>
    /// Registers code fixes for the given context.
    /// </summary>
    Task RegisterCodeFixAsync(CodeFixContext context);
}
