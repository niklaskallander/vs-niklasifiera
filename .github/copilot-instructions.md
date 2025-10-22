# Niklasifiera - Copilot Instructions

## Project Overview

Niklasifiera is a Roslyn-based code analyzer and code fix provider for C# that enforces specific formatting rules for method/constructor signatures and type inheritance declarations. The project uses a plugin architecture pattern for both analyzers and code fixes, making it easy to extend with new diagnostic rules.

## Code Style

**All code must follow the project's code style conventions.** See [code-style-guide.md](code-style-guide.md) for detailed formatting rules and examples. Key points:
- Multi-line method signatures with 2+ parameters
- Opening braces on separate lines
- 4-space indentation
- File-scoped namespaces
- Collection expressions `[]`
- `ConfigureAwait(false)` on all awaits

## Architecture

### Plugin Architecture Pattern

Both the analyzer and code fix provider use a self-registration plugin pattern:

1. **Analyzer Side**: Services implement `IAnalyzerService` and register themselves
2. **CodeFix Side**: Services implement `ICodeFixService` and register themselves
3. **Main Orchestrators**: Thin coordinators that delegate to services

This design provides:
- ✅ Clean separation of concerns
- ✅ Self-contained diagnostic services
- ✅ Easy extensibility
- ✅ Minimal boilerplate in main classes

### Project Structure

```
Niklasifiera/                           # Analyzer project
├── NiklasifieraAnalyzer.cs             # Main analyzer (orchestrator)
├── Services/
│   ├── IAnalyzerService.cs             # Analyzer service interface
│   ├── SignatureAnalyzerService.cs     # NIKL001 diagnostic
│   ├── InheritanceAnalyzerService.cs   # NIKL002 diagnostic
│   ├── ConfigurationReader.cs          # Reads .editorconfig settings
│   └── Extensions.cs                   # Syntax tree helper methods

Niklasifiera.CodeFixes/                 # Code fix project
├── NiklasifieraCodeFixProvider.cs      # Main code fix provider (orchestrator)
├── Services/
│   ├── ICodeFixService.cs              # Code fix service interface
│   ├── SignatureFormattingService.cs   # NIKL001 code fix
│   ├── InheritanceFormattingService.cs # NIKL002 code fix
│   └── ConfigurationService.cs         # Reads formatting configuration

Niklasifiera.Test/                      # Unit tests
Niklasifiera.Package/                   # NuGet package
Niklasifiera.Samples/                   # Sample code for testing
```

## Core Interfaces

### IAnalyzerService

```csharp
public interface IAnalyzerService
{
    /// <summary>
    /// Gets the diagnostic rule that this analyzer service handles.
    /// </summary>
    DiagnosticDescriptor Rule { get; }

    /// <summary>
    /// Initializes the analyzer and registers syntax node actions.
    /// </summary>
    void InitializeAnalyzer(AnalysisContext context);
}
```

**Key Principles:**
- Each service owns its `DiagnosticDescriptor` (diagnostic rule)
- Services register their own syntax node actions
- Services handle their own diagnostic reporting
- Services are self-contained and stateless

### ICodeFixService

```csharp
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
```

**Key Principles:**
- Each service owns its diagnostic ID constant
- Services handle their own syntax node finding
- Services check if fixes should be skipped
- Services register their own code actions

## Adding a New Analyzer and Code Fix

Follow these steps to add a new diagnostic rule with automatic code fix:

### Step 1: Create the Analyzer Service

Create a new file `Niklasifiera/Services/YourFeatureAnalyzerService.cs`:

```csharp
namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

public class YourFeatureAnalyzerService
    : IAnalyzerService
{
    // 1. Define your diagnostic ID (use next available number)
    public const string DiagnosticId =
        "NIKL003";
    
    private const string Category =
        "Formatting";
    
    // 2. Define localized strings for the diagnostic
    private static readonly LocalizableString Title =
        new LocalizableResourceString
        (
            nameof(Resources.YourFeatureAnalyzerTitle),
            Resources.ResourceManager,
            typeof(Resources)
        );
    
    private static readonly LocalizableString MessageFormat =
        new LocalizableResourceString
        (
            nameof(Resources.YourFeatureAnalyzerMessageFormat),
            Resources.ResourceManager,
            typeof(Resources)
        );
    
    private static readonly LocalizableString Description =
        new LocalizableResourceString
        (
            nameof(Resources.YourFeatureAnalyzerDescription),
            Resources.ResourceManager,
            typeof(Resources)
        );

    // 3. Create the diagnostic rule
    public DiagnosticDescriptor Rule { get; } =
        new
        (
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description
        );

    // 4. Implement the initialization method
    public void InitializeAnalyzer(AnalysisContext context)
    {
        // Register for specific syntax kinds you want to analyze
        context
            .RegisterSyntaxNodeAction
            (
                Analyze,
                SyntaxKind.YourTargetSyntaxKind
            );
    }

    // 5. Implement the analysis logic
    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        // Cast to your specific node type
        var node =
            (YourSyntaxNodeType)context.Node;
        
        // Check if diagnostic should be reported
        if (!ShouldReportDiagnostic(node, context))
        {
            return;
        }

        // Create and report diagnostic
        var diagnostic =
            Diagnostic
                .Create
                (
                    Rule,
                    node.GetLocation(),
                    node.Identifier.Text
                );

        context
            .ReportDiagnostic(diagnostic);
    }

    // 6. Implement validation logic
    private bool ShouldReportDiagnostic
        (
        YourSyntaxNodeType node,
        SyntaxNodeAnalysisContext context
        )
    {
        // Your validation logic here
        return !IsCorrectlyFormatted(node);
    }

    private bool IsCorrectlyFormatted(YourSyntaxNodeType node)
    {
        // Break down complex validation into smaller methods
        // Follow Single Responsibility Principle
        return true;
    }
}
```

### Step 2: Register the Analyzer Service

In `Niklasifiera/NiklasifieraAnalyzer.cs`, add your service to the array:

```csharp
private readonly IAnalyzerService[] _services =
[
    new SignatureAnalyzerService(),
    new InheritanceAnalyzerService(),
    new YourFeatureAnalyzerService()  // Add your new service
];
```

That's it for the analyzer! The main analyzer will automatically:
- Include your rule in `SupportedDiagnostics`
- Call your `InitializeAnalyzer` method
- Let your service handle everything else

### Step 3: Create the Code Fix Service

Create a new file `Niklasifiera.CodeFixes/Services/YourFeatureFormattingService.cs`:

```csharp
namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

// 1. Define the interface (if you want to keep services testable)
public interface IYourFeatureFormattingService
{
    Task<bool> ShouldSkipCodeFixAsync
        (
        Document document,
        YourSyntaxNodeType node
        );
    
    Task<YourSyntaxNodeType> FormatAsync
        (
        Document document,
        YourSyntaxNodeType node,
        SourceText sourceText
        );
}

// 2. Implement the service
public class YourFeatureFormattingService(IConfigurationService configurationService)
    : IYourFeatureFormattingService, ICodeFixService
{
    // 3. Define diagnostic ID (must match analyzer)
    public const string DiagnosticId =
        "NIKL003";
    
    string ICodeFixService.DiagnosticId => DiagnosticId;

    private readonly IConfigurationService _configurationService =
        configurationService
            ?? throw new ArgumentNullException(nameof(configurationService));

    // 4. Implement code fix registration
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
            context.Diagnostics.First();

        var diagnosticSpan =
            diagnostic.Location.SourceSpan;

        // Find your target syntax node
        var node =
            root.FindToken(diagnosticSpan.Start)
                .Parent?
                    .AncestorsAndSelf()
                    .OfType<YourSyntaxNodeType>()
                    .First();

        // Check if fix should be skipped
        if (node is null ||
            await ShouldSkipCodeFixAsync(context.Document, node)
                .ConfigureAwait(false))
        {
            return;
        }

        // Register the code fix
        context
            .RegisterCodeFix
            (
                CodeAction.Create
                (
                    title: CodeFixResources.YourFeatureCodeFixTitle,
                    createChangedDocument: x =>
                        FormatDocumentAsync(context.Document, root, node, x),
                    equivalenceKey: nameof(CodeFixResources.YourFeatureCodeFixTitle)
                ),
                diagnostic
            );
    }

    // 5. Implement skip logic
    public async Task<bool> ShouldSkipCodeFixAsync
        (
        Document document,
        YourSyntaxNodeType node
        )
    {
        // Check for preprocessor directives (always skip)
        if (node.DescendantTrivia(descendIntoTrivia: true)
            .Any(x => x.IsPreprocessorDirective()))
        {
            return true;
        }

        // Check trivia handling configuration
        if (!node.ContainsNonWhitespaceTrivia())
        {
            return false;
        }

        var triviaHandling =
            await _configurationService
                .GetTriviaHandlingBehaviorAsync(document)
                .ConfigureAwait(false);

        return triviaHandling == TriviaHandlingBehavior.Skip;
    }

    // 6. Implement formatting logic
    public async Task<YourSyntaxNodeType> FormatAsync
        (
        Document document,
        YourSyntaxNodeType node,
        SourceText sourceText
        )
    {
        // Your formatting logic here
        // Get configuration settings
        var indentationUnit =
            await _configurationService
                .GetIndentationUnitAsync(document)
                .ConfigureAwait(false);
        
        var lineEnding =
            await _configurationService
                .GetLineEndingAsync(document)
                .ConfigureAwait(false);

        // Apply formatting transformations
        return formattedNode;
    }

    private async Task<Document> FormatDocumentAsync
        (
        Document document,
        SyntaxNode root,
        YourSyntaxNodeType node,
        CancellationToken cancellationToken
        )
    {
        var sourceText =
            await document
                .GetTextAsync(cancellationToken)
                .ConfigureAwait(false);

        var newNode =
            await FormatAsync(document, node, sourceText)
                .ConfigureAwait(false);

        var newRoot =
            root.ReplaceNode(node, newNode);

        return document.WithSyntaxRoot(newRoot);
    }
}
```

### Step 4: Register the Code Fix Service

In `Niklasifiera.CodeFixes/NiklasifieraCodeFixProvider.cs`, add your service:

```csharp
public NiklasifieraCodeFixProvider()
{
    var configurationService = new ConfigurationService();
    _codeFixServices =
    [
        new SignatureFormattingService(configurationService),
        new InheritanceFormattingService(configurationService),
        new YourFeatureFormattingService(configurationService)  // Add your service
    ];
}
```

### Step 5: Add Resource Strings

In `Niklasifiera/Resources.resx`, add entries:
- `YourFeatureAnalyzerTitle`: "Your Feature Title"
- `YourFeatureAnalyzerMessageFormat`: "Your feature '{0}' is not formatted correctly"
- `YourFeatureAnalyzerDescription`: "Description of your formatting rule"

In `Niklasifiera.CodeFixes/CodeFixResources.resx`, add:
- `YourFeatureCodeFixTitle`: "Fix your feature formatting"

### Step 6: Write Tests

In `Niklasifiera.Test/NiklasifieraUnitTests.cs`, add test methods:

```csharp
[Fact]
public async Task TestYourFeature_DetectsProblem()
{
    var test = @"
        // Your test code here with the problem
    ";

    var expected =
        VerifyCS
            .Diagnostic(YourFeatureAnalyzerService.DiagnosticId)
            .WithLocation(0)
            .WithArguments("YourIdentifier");

    await VerifyCS.VerifyAnalyzerAsync(test, expected);
}

[Fact]
public async Task TestYourFeature_AppliesCodeFix()
{
    var test = @"
        // Your test code with problem
    ";

    var fixedCode = @"
        // Your expected fixed code
    ";

    await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
}
```

## Best Practices

### Analyzer Services

1. **Keep methods focused**: Break down large validation methods into smaller, single-purpose methods
2. **Use descriptive names**: Method names should clearly indicate what they validate
3. **Avoid duplication**: Extract common validation logic into shared helper methods
4. **Pattern matching**: Use C# pattern matching for cleaner node type checking
5. **Minimize parameters**: Pass full context objects rather than many individual parameters

### Code Fix Services

1. **Preserve trivia**: Always consider comment and preprocessor directive preservation
2. **Handle edge cases**: Check for null values and unexpected syntax structures
3. **Configuration awareness**: Respect user configuration settings (indentation, line endings)
4. **Async patterns**: Use `ConfigureAwait(false)` for all async operations
5. **Testability**: Keep interfaces for dependency injection in tests

### Common Patterns

#### Finding Parent Declarations

```csharp
var declaration =
    node.Ancestors()
        .OfType<YourDeclarationType>()
        .FirstOrDefault();
```

#### Getting Indentation

```csharp
var indentation =
    sourceText.GetLineIndentationFor(lineNumber);
```

#### Creating Formatted Syntax with Trivia

```csharp
var newNode =
    originalNode
        .WithLeadingTrivia(newLeadingTrivia)
        .WithTrailingTrivia(newTrailingTrivia);
```

#### Pattern Matching for Node Types

```csharp
var result = node switch
{
    MethodDeclarationSyntax method
        => HandleMethod(method),

    ConstructorDeclarationSyntax ctor
        => HandleConstructor(ctor),

    _ => null
};
```

## Configuration

The analyzer respects `.editorconfig` settings:

```ini
# Example .editorconfig settings
[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf

# Custom analyzer settings (if needed)
dotnet_diagnostic.NIKL001.severity = warning
dotnet_diagnostic.NIKL002.severity = warning
```

## Testing Strategy

1. **Positive tests**: Code that should trigger diagnostics
2. **Negative tests**: Code that should not trigger diagnostics
3. **Code fix tests**: Verify fixes produce expected output
4. **Trivia preservation tests**: Ensure comments/directives are preserved
5. **Edge case tests**: Test unusual but valid syntax

## Debugging Tips

1. **Use the Samples project**: Add test cases to `Niklasifiera.Samples` for manual testing
2. **Debug with F5**: Run the analyzer project to launch a new VS instance with the analyzer loaded
3. **Check test output**: Unit tests provide detailed failure information
4. **Use SyntaxVisualizer**: Install Roslyn's Syntax Visualizer extension to inspect syntax trees

## Common Issues

### Issue: Analyzer not triggering
- Verify `SyntaxKind` registration matches your target nodes
- Check `ShouldReportDiagnostic` logic isn't filtering incorrectly
- Ensure the service is registered in the main analyzer

### Issue: Code fix not appearing
- Verify diagnostic ID matches between analyzer and code fix
- Check `ShouldSkipCodeFixAsync` isn't returning true
- Ensure the service is registered in the code fix provider

### Issue: Tests failing after refactoring
- Ensure test code matches expected formatting exactly (including whitespace)
- Check that diagnostic locations are correctly marked with `{|#0:...|}` syntax
- Verify fixed code includes all necessary trivia

## Resources

- [Roslyn Documentation](https://github.com/dotnet/roslyn)
- [Analyzer API Reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnostics)
- [Code Fix Provider Guide](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.codefixes)
- [Syntax Visualizer Extension](https://marketplace.visualstudio.com/items?itemName=RoslynTeam.SyntaxVisualizingTool)
