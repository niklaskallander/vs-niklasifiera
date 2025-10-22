namespace Niklasifiera;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Collections.Immutable;

using Niklasifiera.Services;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NiklasifieraAnalyzer
    : DiagnosticAnalyzer
{
    private readonly IAnalyzerService[] _services =
    [
        new SignatureAnalyzerService(),
        new InheritanceAnalyzerService(),
        new ConditionalOperatorAnalyzerService()
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => _services
            .Select(x => x.Rule)
            .ToImmutableArray();

    public override void Initialize(AnalysisContext context)
    {
        context
            .ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context
            .EnableConcurrentExecution();

        foreach (var service in _services)
        {
            service
                .InitializeAnalyzer(context);
        }
    }
}
