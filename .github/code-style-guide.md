# Niklasifiera Code Style Guide

## Overview

This document defines the coding standards and formatting conventions used throughout the Niklasifiera project. Following these conventions ensures consistency and readability across the codebase.

## General Principles

- **Clarity over brevity**: Prefer readable code over clever one-liners
- **Vertical spacing**: Use blank lines to separate logical sections
- **Consistency**: Follow established patterns within the file and project
- **Self-documenting code**: Use descriptive names that reduce the need for comments

## File Structure

### Namespace Declaration

Use file-scoped namespaces (C# 10+):

```csharp
namespace Niklasifiera.Services;
```

### Using Directives

- Place using directives after the namespace declaration
- Group by category with blank lines between groups:
  1. Microsoft.CodeAnalysis.*
  2. Microsoft.* (other)
  3. System.*
  4. Third-party libraries
  5. Project namespaces

```csharp
namespace Niklasifiera.Services;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
```

### Class Declaration

```csharp
/// <summary>
/// Service for analyzing method and constructor signature formatting.
/// </summary>
public class SignatureAnalyzerService
    : IAnalyzerService
{
    // Class members
}
```

**Rules:**
- XML documentation comments for public types and members
- Base class/interface on separate line, indented with 4 spaces
- Opening brace on new line

## Formatting Conventions

### Method Signatures

#### Simple Parameters (0-1 parameters)

Single line:

```csharp
private bool IsIncorrectlySplit(ParameterListSyntax parameterList)
    => false;
```

```csharp
public void Initialize(AnalysisContext context)
{
    // Method body
}
```

#### Multiple Parameters (2+ parameters)

Multi-line with parameters on separate lines:

```csharp
private bool ShouldReportDiagnostic
    (
    ParameterListSyntax parameterList,
    SyntaxNode parentDeclaration,
    AnalyzerConfigOptionsProvider optionsProvider
    )
{
    // Method body
}
```

**Rules:**
- Opening parenthesis on new line, indented 4 spaces
- Each parameter on its own line, indented 4 spaces
- Closing parenthesis on new line, indented 4 spaces
- Opening brace on new line at method indentation level

### Constants and Fields

Constants/fields with simple assignments on single line:

```csharp
public const string DiagnosticId = "NIKL001";

private const string Category = "Formatting";
```

Constants/fields with complex assignments split across lines:

```csharp
public const string DiagnosticId =
    "NIKL001";

private readonly IConfigurationService _configurationService =
    configurationService
        ?? throw new ArgumentNullException(nameof(configurationService));
```

### Object Initialization

#### Simple Object Creation

Multi-line with properties on separate lines:

```csharp
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
```

**Rules:**
- Assignment operator on declaration line
- `new` keyword on new line, indented 4 spaces
- Opening parenthesis on same line as `new`
- Each argument/parameter on separate line, indented 4 spaces
- Closing parenthesis on new line, indented 4 spaces
- Semicolon on same line as closing parenthesis

### Method Calls

#### Simple Calls

Single line for short method calls:

```csharp
context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
```

#### Multi-Parameter Calls

Split across lines:

```csharp
context
    .RegisterSyntaxNodeAction
    (
        Analyze,
        SyntaxKind.ClassDeclaration,
        SyntaxKind.StructDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.RecordStructDeclaration
    );
```

**Rules:**
- Object reference on first line
- Method name on new line, indented 4 spaces (continuing the call chain)
- Opening parenthesis on new line, indented 4 spaces
- Arguments on separate lines, indented 8 spaces
- Closing parenthesis on new line, indented 4 spaces
- Semicolon on same line as closing parenthesis

#### Chained Method Calls

```csharp
var diagnostic =
    Diagnostic
        .Create
        (
            Rule,
            parameterList.GetLocation(),
            identifierText
        );
```

**Rules:**
- Variable assignment operator on declaration line
- Each method in chain on new line, indented 4 spaces per level
- Method parameters follow multi-parameter call rules

### Variable Declarations

#### Simple Assignments

```csharp
var parameterCount = parameters.Count;
```

#### Complex Assignments

```csharp
var declarationIndentation =
    sourceText
        .GetLineIndentationFor(declarationStartLine);
```

**Rules:**
- Assignment operator on declaration line
- Value expression starts on new line, indented 4 spaces
- Continuation indented 4 spaces per level

### Pattern Matching and Switch Expressions

#### Switch Expressions

```csharp
var (parameterList, parentDeclaration, identifierText) = context.Node switch
{
    MethodDeclarationSyntax method
        => (method.ParameterList, method, method.Identifier.Text),

    ConstructorDeclarationSyntax constructor
        => (constructor.ParameterList, constructor, constructor.Identifier.Text),

    TypeDeclarationSyntax type when type.ParameterList is not null
        => (type.ParameterList, (SyntaxNode)type, type.Identifier.Text),

    _ => (null, null, null)
};
```

**Rules:**
- `switch` keyword on same line as variable
- Opening brace on new line
- Each case pattern on separate line
- Arrow operator (`=>`) on new line, indented 8 spaces
- Value expression follows the arrow, can be multi-line if needed
- Closing brace and semicolon on separate line

#### Pattern Matching in Return Statements

```csharp
private (SyntaxToken? openBrace, SyntaxToken? closeBrace) GetBraceTokens(SyntaxNode parentDeclaration)
    => parentDeclaration switch
    {
        MethodDeclarationSyntax method
            => (method.Body?.OpenBraceToken, method.Body?.CloseBraceToken),

        ConstructorDeclarationSyntax constructor
            => (constructor.Body?.OpenBraceToken, constructor.Body?.CloseBraceToken),

        TypeDeclarationSyntax type
            => (type.OpenBraceToken, type.CloseBraceToken),

        _ => (null, null)
    };
```

### Conditional Statements

#### Simple Conditions

```csharp
if (parameterList == null)
{
    return false;
}
```

#### Guard Clauses

Early returns without else:

```csharp
if (parameterList is null || parentDeclaration is null)
{
    return;
}

if (!ShouldReportDiagnostic(parameterList, parentDeclaration, context.Options.AnalyzerConfigOptionsProvider))
{
    return;
}

// Continue with main logic
```

#### Complex Conditions

```csharp
if (!openBraceToken.HasValue ||
    openBraceToken.Value.IsKind(SyntaxKind.None))
{
    return true;
}
```

**Rules:**
- Logical operators at the end of lines
- Continuation aligned with opening condition

### Loops

```csharp
foreach (var parameter in parameters)
{
    if (!IsParameterCorrectlyFormatted(parameter, sourceText, previousLine, expectedIndentation))
    {
        return false;
    }

    previousLine =
        sourceText
            .GetLineNumberFor(parameter);
}
```

### LINQ Queries

Multi-line for complex queries:

```csharp
var newParameters =
    parameterList.Parameters
        .Select(x => x.WithoutTrivia())
        .ToArray();
```

```csharp
public sealed override ImmutableArray<string> FixableDiagnosticIds
    => _codeFixServices
        .Select(x => x.DiagnosticId)
        .ToImmutableArray();
```

### Lambda Expressions

#### Simple Lambdas

```csharp
.Select(x => x.WithoutTrivia())
```

#### Multi-Line Lambdas

```csharp
context.RegisterSyntaxNodeAction(context =>
{
    var node = context.Node;
    // More logic
}, SyntaxKind.MethodDeclaration);
```

### Async/Await

Always use `ConfigureAwait(false)` for library code:

```csharp
var sourceText =
    await document
        .GetTextAsync(cancellationToken)
        .ConfigureAwait(false);
```

```csharp
if (parameterList is null || 
    await ShouldSkipCodeFixAsync(context.Document, parameterList)
        .ConfigureAwait(false))
{
    return;
}
```

### Collection Expressions

Use collection expressions (C# 12):

```csharp
private readonly IAnalyzerService[] _services =
[
    new SignatureAnalyzerService(),
    new InheritanceAnalyzerService()
];
```

### Primary Constructors

Use primary constructors when appropriate:

```csharp
public class SignatureFormattingService(IConfigurationService configurationService)
    : ISignatureFormattingService, ICodeFixService
{
    private readonly IConfigurationService _configurationService =
        configurationService
            ?? throw new ArgumentNullException(nameof(configurationService));
}
```

## Naming Conventions

### General Rules

- **PascalCase**: Types, methods, properties, constants, public fields
- **camelCase**: Private fields (with `_` prefix), parameters, local variables
- **Descriptive names**: Use full words, avoid abbreviations (except common ones like `Id`, `Sql`, `Xml`)

### Examples

```csharp
// Types
public class SignatureAnalyzerService
public interface IAnalyzerService

// Constants
public const string DiagnosticId = "NIKL001";
private const string Category = "Formatting";

// Fields
private readonly IConfigurationService _configurationService;

// Properties
public DiagnosticDescriptor Rule { get; }

// Methods
public void InitializeAnalyzer(AnalysisContext context)
private bool ShouldReportDiagnostic(...)

// Parameters
private void Analyze(SyntaxNodeAnalysisContext context)

// Local variables
var parameterList = root.FindToken(diagnosticSpan.Start);
var sourceText = await document.GetTextAsync();
```

## Comments and Documentation

### XML Documentation

Required for all public types and members:

```csharp
/// <summary>
/// Service for analyzing method and constructor signature formatting.
/// </summary>
public class SignatureAnalyzerService
    : IAnalyzerService
{
    /// <summary>
    /// Gets the diagnostic rule that this analyzer service handles.
    /// </summary>
    public DiagnosticDescriptor Rule { get; }

    /// <summary>
    /// Initializes the analyzer and registers syntax node actions.
    /// </summary>
    public void InitializeAnalyzer(AnalysisContext context)
    {
        // Implementation
    }
}
```

### Inline Comments

Use sparingly, prefer self-documenting code:

```csharp
// Rule: Opening brace must be on its own line after the closing parenthesis
if (openBraceLine <= closeParenLine)
{
    return false;
}

// Rule: Opening brace must be indented at the same level as the declaration
var openBraceIndentation =
    sourceText
        .GetLineIndentationFor(openBraceLine);
```

### Region Comments

Use regions to organize large files:

```csharp
#region Helper Methods

private static string GetDeclarationIndentation(...)
{
    // Implementation
}

#endregion
```

## Code Organization

### File Organization

1. Namespace declaration
2. Using directives
3. Type declaration (class/interface)
4. Constants
5. Fields
6. Properties
7. Constructors
8. Public methods
9. Protected methods
10. Private methods
11. Nested types

### Method Organization Within Class

Group related methods together:

```csharp
public class SignatureFormattingService
{
    // 1. Interface implementation
    public async Task RegisterCodeFixAsync(CodeFixContext context) { }
    
    // 2. Public API
    public async Task<bool> ShouldSkipCodeFixAsync(...) { }
    public async Task<ParameterListSyntax> FormatSignatureAsync(...) { }
    
    // 3. Private formatting methods
    private ParameterListSyntax FormatSingleLine(...) { }
    private ParameterListSyntax FormatSingleLineWithTriviaPreservation(...) { }
    private async Task<ParameterListSyntax> FormatMultipleLinesAsync(...) { }
    
    // 4. Helper methods
    private static string GetDeclarationIndentation(...) { }
}
```

## Modern C# Features

### Use Modern Syntax When Appropriate

- ✅ File-scoped namespaces
- ✅ Primary constructors
- ✅ Collection expressions `[]`
- ✅ Pattern matching and switch expressions
- ✅ Target-typed `new()`
- ✅ Expression-bodied members
- ✅ Nullable reference types
- ✅ `is null` instead of `== null`

### Example

```csharp
namespace Niklasifiera.Services;

public class ExampleService(IDependency dependency)
{
    private readonly IDependency _dependency = 
        dependency ?? throw new ArgumentNullException(nameof(dependency));
    
    public string[] Items { get; } = ["item1", "item2"];
    
    public string? GetValue(string? input)
        => input switch
        {
            null => null,
            "" => "empty",
            _ => input.ToUpper()
        };
}
```

## EditorConfig Settings

The project uses these settings (from `.editorconfig`):

```ini
[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Organize usings
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false
```

## Summary Checklist

When writing code, ensure:

- ✅ File-scoped namespace at the top
- ✅ Using directives after namespace
- ✅ XML documentation on public members
- ✅ Multi-line method signatures with 2+ parameters
- ✅ Opening braces on their own lines
- ✅ 4-space indentation throughout
- ✅ Constants and simple assignments can be single line
- ✅ Complex assignments split with assignment operator on first line
- ✅ `ConfigureAwait(false)` on all awaits in library code
- ✅ Descriptive variable names
- ✅ Guard clauses for early returns
- ✅ Pattern matching where appropriate
- ✅ Collection expressions for arrays
- ✅ Consistent vertical spacing

## Examples from the Project

See these files for reference implementations:
- `Niklasifiera/Services/SignatureAnalyzerService.cs`
- `Niklasifiera/Services/InheritanceAnalyzerService.cs`
- `Niklasifiera.CodeFixes/Services/SignatureFormattingService.cs`
- `Niklasifiera.CodeFixes/Services/InheritanceFormattingService.cs`
