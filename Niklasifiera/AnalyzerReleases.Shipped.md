; Shipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.6

### New Rules

| Rule ID | Category   | Severity | Notes                                                                                              |
| ------- | ---------- | -------- | -------------------------------------------------------------------------------------------------- |
| NIKL003 | Formatting | Warning  | Conditional (ternary) operators should be formatted across multiple lines for improved readability |

## Release 1.0.5

### Changed Rules

| Rule ID | Category   | Severity | Notes                                                                                                |
| ------- | ---------- | -------- | ---------------------------------------------------------------------------------------------------- |
| NIKL001 | Formatting | Warning  | Code fix now attempts to preserve non-whitespace trivia (unless opted to skip code-fix when present) |
| NIKL002 | Formatting | Warning  | Code fix now attempts to preserve non-whitespace trivia (unless opted to skip code-fix when present) |

## Release 1.0.4

### Changed Rules

| Rule ID | Category   | Severity | Notes                                                                            |
| ------- | ---------- | -------- | -------------------------------------------------------------------------------- |
| NIKL001 | Formatting | Warning  | Code fix now uses EOL and indentation settings from .editorconfig when available |
| NIKL002 | Formatting | Warning  | Code fix now uses EOL and indentation settings from .editorconfig when available |

## Release 1.0.3

### New Rules

| Rule ID | Category   | Severity | Notes                                                                                                                  |
| ------- | ---------- | -------- | ---------------------------------------------------------------------------------------------------------------------- |
| NIKL001 | Formatting | Warning  | Method and constructor signatures with multiple parameters should be formatted with each parameter on its own line     |
| NIKL002 | Formatting | Warning  | Type inheritance/interface implementations should be formatted with each interface on its own line with leading commas |
