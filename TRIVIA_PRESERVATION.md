# Trivia Preservation in Niklasifiera Code Fixes

## Overview

The Niklasifiera analyzer has been enhanced to handle non-whitespace trivia (comments, directives, etc.) when applying code fixes. This prevents important code elements from being accidentally removed during formatting.

## Configuration

Add the following setting to your `.editorconfig` file to control trivia handling behavior:

```ini
[*.cs]
niklasifiera_preserve_trivia = skip  # or "preserve"
```

### Options

- **`skip`** (default): Skip applying code fixes when non-whitespace trivia is detected. The analyzer will still report warnings, but code fixes won't be applied to avoid losing trivia.
- **`preserve`**: Apply code fixes and preserve non-whitespace trivia while reformatting whitespace only.

## Examples

### Before (without trivia preservation)
```csharp
// This method does something important
public void ProcessData(/* important comment */ int data, string format) // inline comment
{
    // Code fix would remove ALL trivia, including comments
}
```

### With `niklasifiera_preserve_trivia = skip`
- The analyzer reports the formatting warning
- No code fix is applied (to preserve the comments)
- Manual formatting is required

### With `niklasifiera_preserve_trivia = preserve`
```csharp
// This method does something important
public void ProcessData
    (
    /* important comment */ int data,
    string format // inline comment
    )
{
    // Comments are preserved during reformatting
}
```

## Types of Trivia Preserved

The following trivia types are considered non-whitespace and will be preserved:
- Single-line comments (`//`)
- Multi-line comments (`/* */`)
- Documentation comments (`///`)
- Preprocessor directives (`#if`, `#endif`, etc.)
- Pragma directives (`#pragma`)
- All other non-whitespace trivia

## Types of Trivia That Get Reformatted

The following trivia types are considered whitespace and will be reformatted:
- Spaces
- Tabs
- Line endings (newlines)

## Recommended Usage

1. **For most projects**: Use `skip` (default) to be safe and preserve all existing trivia
2. **For automated formatting**: Use `preserve` if you want automatic fixes that respect comments
3. **For legacy code**: Use `skip` to avoid any risk of losing important trivia

## Migration

If you're upgrading from an earlier version of Niklasifiera:
- The default behavior is now safer (skips fixes when trivia is present)
- To get the old behavior (remove all trivia), set `niklasifiera_preserve_trivia = preserve` and ensure your code doesn't have important trivia that needs to be preserved
- Review any existing code that has trivia near parameter lists or inheritance declarations

## Limitations

- The trivia preservation is best-effort and may not handle all edge cases perfectly
- Complex trivia arrangements might still require manual formatting
- The analyzer will always report formatting issues regardless of the trivia setting