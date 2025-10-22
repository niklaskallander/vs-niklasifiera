# Niklasifiera Code Style Guide

## Overview

This document defines the coding standards and formatting conventions used throughout the Niklasifiera project. Following these conventions ensures consistency and readability across the codebase.

## General Principles

- **Clarity over brevity**: Prefer readable code over clever one-liners
- **Vertical spacing**: Use blank lines to separate logical sections
- **Consistency**: Follow established patterns within the file and project
- **Self-documenting code**: Use descriptive names that reduce the need for comments
- **Avoid code smells**: Keep functions focused, reduce duplication, and use proper abstractions

## Code Health and Quality

### Avoiding Code Smells

The project maintains high code health by following these principles:

#### 1. Avoid Code Duplication

**Problem**: Duplicated code makes maintenance harder since changes must be made in multiple places.

**Solution**: Extract common logic into shared methods or helper classes.

**❌ Bad - Duplicated logic:**

```csharp
private bool ValidateAssignment(...)
{
    var line1 = sourceText.GetLineNumberFor(token1);
    var line2 = sourceText.GetLineNumberFor(token2);
    var indent = sourceText.GetLineIndentationFor(line1);
    // validation logic
}

private bool ValidateReturn(...)
{
    var line1 = sourceText.GetLineNumberFor(token1);
    var line2 = sourceText.GetLineNumberFor(token2);
    var indent = sourceText.GetLineIndentationFor(line1);
    // similar validation logic
}
```

**✅ Good - Shared context object:**

```csharp
private sealed class ValidationContext
{
    public int Line1 { get; }
    public int Line2 { get; }
    public string Indentation { get; }
    
    public ValidationContext
        (
        SourceText sourceText,
        SyntaxToken token1,
        SyntaxToken token2
        )
    {
        Line1 =
            sourceText
                .GetLineNumberFor(token1);

        Line2 =
            sourceText
                .GetLineNumberFor(token2);

        Indentation =
            sourceText
                .GetLineIndentationFor(Line1);
    }
    
    public bool Validate() { /* shared logic */ }
}

private bool ValidateAssignment(...)
{
    var context =
        new ValidationContext(sourceText, token1, token2);

    return context
        .Validate();
}
```

#### 2. Limit Function Arguments (Max 4)

**Problem**: Functions with many parameters are hard to understand and maintain.

**Solution**: Group related parameters into context objects.

**❌ Bad - Too many parameters:**

```csharp
private bool ValidateFormatting
    (
    SourceText sourceText,
    int line1,
    int line2,
    int line3,
    string indent1,
    string indent2,
    bool preserveTrivia
    )
{
    // Implementation
}
```

**✅ Good - Context object:**

```csharp
private sealed class FormattingContext
{
    public SourceText SourceText { get; }

    public int Line1 { get; }
    public int Line2 { get; }
    public int Line3 { get; }

    public string Indent1 { get; }
    public string Indent2 { get; }
    
    public bool PreserveTrivia { get; }
    
    public FormattingContext(/* parameters */) { /* initialization */ }
}

private bool ValidateFormatting(FormattingContext context)
{
    // Implementation
}
```

#### 3. Avoid Primitive Obsession

**Problem**: Using primitive types (int, string, bool) everywhere lacks semantic meaning and type safety.

**Solution**: Create domain-specific types that encapsulate validation and semantics.

**❌ Bad - Primitive obsession:**

```csharp
private bool ValidateIndentation
    (
    SourceText sourceText,
    int line,
    string expectedIndent
    )
{
    var actualIndent =
        sourceText
            .GetLineIndentationFor(line);

    return actualIndent == expectedIndent;
}
```

**✅ Good - Domain objects:**

```csharp
private sealed class LineIndentation
{
    public int LineNumber { get; }
    public string Indentation { get; }
    
    public LineIndentation
        (
        SourceText sourceText,
        int lineNumber
        )
    {
        LineNumber = lineNumber;

        Indentation =
            sourceText
                .GetLineIndentationFor(lineNumber);
    }
    
    public bool Matches(string expected)
        => Indentation == expected;
}

private bool ValidateIndentation
    (
    LineIndentation actual,
    string expected
    )
    => actual
        .Matches(expected);
```

#### 4. Single Responsibility Principle

**Problem**: Functions that do too many things are hard to test and maintain.

**Solution**: Break down complex functions into smaller, focused methods.

**❌ Bad - Too many responsibilities:**

```csharp
private bool ValidateAndFormat(...)
{
    // Extract line numbers
    var line1 =
        GetLine1();

    var line2 =
        GetLine2();
    
    // Validate split
    if (line1 <= line2)
    {
        return false;
    }
    
    // Calculate indentation
    var indent =
        CalculateIndent();
    
    // Validate indentation
    if (!ValidateIndent(indent))
    {
        return false;
    }
    
    // Format the code
    return Format();
}
```

**✅ Good - Separate concerns:**

```csharp
private bool ShouldReportDiagnostic(...)
{
    if (!IsProperlySplit())
    {
        return false;
    }
    
    if (!HasCorrectIndentation())
    {
        return false;
    }

    return true;
}

private bool IsProperlySplit() { /* focused logic */ }

private bool HasCorrectIndentation() { /* focused logic */ }
```

#### 5. Encapsulation

**Problem**: Exposing internal state or implementation details.

**Solution**: Use private fields and provide public methods for operations.

**✅ Good - Encapsulated context:**

```csharp
private sealed class ConditionalFormattingContext
{
    public SourceText SourceText { get; }

    public int ConditionLine { get; }
    public int QuestionLine { get; }
    public int ColonLine { get; }
    
    private readonly string _indentationUnit;
    
    public ConditionalFormattingContext(...) { /* initialization */ }
    
    // Encapsulated validation logic
    public bool IsProperlySplit(int referenceLine)
        => ConditionLine > referenceLine
        && QuestionLine > ConditionLine
        && ColonLine > QuestionLine;
    
    public bool ValidateIndentation(string baseIndentation)
    {
        // Implementation uses private _indentationUnit
    }
}
```

### Code Health Metrics

The project aims for:
- **Code Health Score**: ≥ 9.5/10.0
- **Function Arguments**: ≤ 4 parameters
- **Code Duplication**: < 5%
- **Primitive Obsession**: < 30% of functions

Use these guidelines when adding new analyzers or code fixes to maintain high code quality.

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

### Conditional (Ternary) Operators

Always format conditional operators across multiple lines for readability.

#### Assignment Context

Condition goes on a new line after the equals sign:

```csharp
// ❌ Bad - single line
var result = condition ? "Yes" : "No";

// ✅ Good - multi-line assignment
var result =
    condition
        ? "Yes"
        : "No";

// ✅ Good - complex conditions
var message =
    isValid && hasPermission
        ? GetSuccessMessage()
        : GetErrorMessage();
```

**Rules:**
- Assignment operator on declaration line
- Condition on new line, indented 4 spaces
- `?` operator on new line, indented 8 spaces
- True expression after `?`, with single space
- `:` operator on new line, indented 8 spaces
- False expression after `:`, with single space

#### Return/Other Context

Condition stays on the same line as the statement:

```csharp
// ❌ Bad - single line
return age >= 18 ? "Adult" : "Minor";

// ✅ Good - multi-line return
return age >= 18
    ? "Adult"
    : "Minor";

// ✅ Good - complex return
return count > 0 && isEnabled
    ? ProcessItems(count)
    : GetDefaultValue();
```

**Rules:**
- Condition on same line as `return` (or other statement)
- `?` operator on new line, indented 4 spaces
- True expression after `?`, with single space
- `:` operator on new line, indented 4 spaces
- False expression after `:`, with single space

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
- `Niklasifiera/Services/ConditionalOperatorAnalyzerService.cs`
- `Niklasifiera.CodeFixes/Services/SignatureFormattingService.cs`
- `Niklasifiera.CodeFixes/Services/InheritanceFormattingService.cs`
- `Niklasifiera.CodeFixes/Services/ConditionalOperatorFormattingService.cs`
