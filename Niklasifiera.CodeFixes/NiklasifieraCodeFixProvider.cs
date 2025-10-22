namespace Niklasifiera;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

using System.Collections.Immutable;
using System.Composition;

using Niklasifiera.Services;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NiklasifieraCodeFixProvider)), Shared]
public class NiklasifieraCodeFixProvider
    : CodeFixProvider
{
    private readonly IEnumerable<ICodeFixService> _codeFixServices;

    public NiklasifieraCodeFixProvider()
    {
        var configurationService =
            new ConfigurationService();

        _codeFixServices =
        [
            new SignatureFormattingService(configurationService),
            new InheritanceFormattingService(configurationService)
        ];
    }

    protected NiklasifieraCodeFixProvider(IEnumerable<ICodeFixService> codeFixServices)
        => _codeFixServices =
            codeFixServices
                ?? throw new ArgumentNullException(nameof(codeFixServices));

    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => _codeFixServices
            .Select(x => x.DiagnosticId)
            .ToImmutableArray();

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic =
            context.Diagnostics
                .FirstOrDefault();

        if (diagnostic is null)
        {
            return;
        }

        var service =
            _codeFixServices
                .FirstOrDefault(x => x.DiagnosticId == diagnostic.Id);

        if (service is null)
        {
            return;
        }

        await service
            .RegisterCodeFixAsync(context)
            .ConfigureAwait(false);
    }
}
