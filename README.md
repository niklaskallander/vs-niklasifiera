# Niklasifiera

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/niklaskallander/vs-niklasifiera)
[![NuGet](https://img.shields.io/nuget/v/Niklasifiera.svg)](https://www.nuget.org/packages/Niklasifiera/)
[![License](https://img.shields.io/github/license/niklaskallander/vs-niklasifiera.svg)](LICENSE)

![Alt text](https://raw.githubusercontent.com/niklaskallander/vs-niklasifiera/refs/heads/main/icon-big.png "Niklasifiera Icon")

A C# Roslyn analyzer that enforces consistent formatting for method signatures and type inheritance declarations. Niklasifiera helps maintain clean, readable code by automatically detecting and fixing formatting issues in your C# projects.

## Features

### 🔍 Signature Formatting (NIKL001)
Enforces consistent formatting for method and constructor signatures with multiple parameters:

**❌ Before (triggers analyzer):**
```csharp
public void ProcessData(string data, int timeout, CancellationToken token)
{
    // method body
}
```

**✅ After (auto-fixed):**
```csharp
public void ProcessData
    (
    string data,
    int timeout,
    CancellationToken token
    )
{
    // method body
}
```

### 🏗️ Inheritance Formatting (NIKL002)
Enforces consistent formatting for type inheritance and interface implementations:

**❌ Before (triggers analyzer):**
```csharp
public class MyService : IDisposable, IAsyncDisposable
{
    // class body
}
```

**✅ After (auto-fixed):**
```csharp
public class MyService
    : IDisposable
    , IAsyncDisposable
{
    // class body
}
```

### 🚀 Modern C# Support
- **Primary Constructors** (C# 12+)
- **Record Types** 
- **Generic Constraints**
- **Constructor Declarations**
- **Interface Implementations**

## Installation

### Via NuGet Package Manager
```bash
dotnet add package Niklasifiera
```

### Via Package Manager Console
```powershell
Install-Package Niklasifiera
```

### Via PackageReference
```xml
<PackageReference Include="Niklasifiera" Version="1.0.5">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

## Rules

| Rule ID | Category | Severity | Description |
|---------|----------|----------|-------------|
| NIKL001 | Formatting | Warning | Method and constructor signatures with multiple parameters should be formatted with each parameter on its own line |
| NIKL002 | Formatting | Warning | Type inheritance/interface implementations should be formatted with each interface on its own line with leading commas |

## Configuration

### EditorConfig Support
You can configure rule severity in your `.editorconfig` file:

```ini
[*.cs]
# Disable signature formatting
dotnet_diagnostic.NIKL001.severity = none

# Make inheritance formatting an error
dotnet_diagnostic.NIKL002.severity = error

# Keep as warnings (default)
dotnet_diagnostic.NIKL001.severity = warning
dotnet_diagnostic.NIKL002.severity = warning
```

### Trivia Preservation (New!)
Niklasifiera now supports preserving comments and other non-whitespace trivia during code fixes:

```ini
[*.cs]
# Control how code fixes handle trivia (comments, directives, etc.)
niklasifiera_preserve_trivia = skip     # Skip fixes when trivia is present (default/safe)
# niklasifiera_preserve_trivia = preserve  # Apply fixes and attempt to preserve trivia (currently only works for comments, if other trivia is encountered, code-fixes will be skipped.)
```

**Options:**
- `skip` (default): Skip code fixes when non-whitespace trivia is detected to avoid losing comments
- `preserve`: Apply code fixes while preserving comments and other important trivia

**Example with trivia preservation:**
```csharp
// ❌ Before (with preserve mode)
public void ProcessData(/* input */ int data, string format /* output */)

// ✅ After (comments preserved!)
public void ProcessData
    (
    /* input */ int data,
    string format /* output */
    )
```

For more details, see [TRIVIA_PRESERVATION.md](TRIVIA_PRESERVATION.md).

### Suppression
Suppress rules using standard C# suppression attributes:

```csharp
[SuppressMessage("Niklasifiera", "NIKL001:Method signature should be reformatted")]
public void ShortMethod(int a, int b) { }

#pragma warning disable NIKL002
public class LegacyClass : IDisposable, IComparable
#pragma warning restore NIKL002
{
    // class implementation
}
```

## Examples

### Primary Constructors
```csharp
// ❌ Triggers NIKL001
public class DataService(HttpClient client, ILogger logger, string apiKey)
{
}

// ✅ Properly formatted
public class DataService
    (
    HttpClient client,
    ILogger logger,
    string apiKey
    )
{
}
```

### Record Types
```csharp
// ❌ Triggers NIKL001
public record Person(string FirstName, string LastName, DateTime BirthDate);

// ✅ Properly formatted
public record Person
    (
    string FirstName,
    string LastName,
    DateTime BirthDate
    );
```

### Complex Inheritance
```csharp
// ❌ Triggers NIKL002
public class Repository : IRepository, IDisposable, IAsyncDisposable
{
}

// ✅ Properly formatted
public class Repository
    : IRepository
    , IDisposable
    , IAsyncDisposable
{
}
```

## Building from Source

### Prerequisites
- .NET 9.0 SDK or later
- Visual Studio 2022 or Visual Studio Code

### Build Steps
```bash
git clone https://github.com/niklaskallander/vs-niklasifiera.git
cd vs-niklasifiera
dotnet restore
dotnet build -c Release
```

### Running Tests
```bash
dotnet test
```

## Project Structure

```
├── Niklasifiera/                    # Main analyzer project
│   ├── NiklasifieraAnalyzer.cs      # Core analyzer logic
│   └── Resources.resx               # Localized messages
├── Niklasifiera.CodeFixes/          # Code fix providers
│   └── NiklasifieraCodeFixProvider.cs
├── Niklasifiera.Test/               # Unit tests
│   └── NiklasifieraUnitTests.cs     # 21 comprehensive tests
├── Niklasifiera.Samples/            # Example code for testing
├── Niklasifiera.Package/            # NuGet packaging
└── niklasifiera.sln                 # Solution file
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the terms specified in the [LICENSE](LICENSE) file.

## Author

**Niklas Källander**
- GitHub: [@niklaskallander](https://github.com/niklaskallander)

## Changelog

### Version 1.1.0 (Latest)
- 🎉 **New Feature**: Trivia preservation support
  - Added `niklasifiera_preserve_trivia` configuration option
  - Preserves comments, directives, and other non-whitespace trivia during code fixes
  - Two modes: `skip` (default/safe) and `preserve` (apply fixes with trivia preservation)
  - Prevents accidental loss of important code comments and annotations
- 🔧 Enhanced code fix provider with trivia-aware formatting
- 📚 Added comprehensive documentation for trivia preservation

### Version 1.0.0
- Initial release
- NIKL001: Method and constructor signature formatting
- NIKL002: Type inheritance formatting
- Support for primary constructors, records, and modern C# features
- Comprehensive test suite with 21 test cases
- Full code fix provider support

---

<div align="center">
  <sub>Built with ❤️ for cleaner C# code</sub>
</div>
