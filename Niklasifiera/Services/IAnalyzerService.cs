namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Interface for analyzer services that can register themselves and analyze syntax nodes.
/// </summary>
public interface IAnalyzerService
{
    DiagnosticDescriptor Rule { get; }

    void InitializeAnalyzer(AnalysisContext context);
}
