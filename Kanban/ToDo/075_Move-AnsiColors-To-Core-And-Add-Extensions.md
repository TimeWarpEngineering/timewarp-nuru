# Move AnsiColors to Core Package and Add Extension Methods

## Description

Move `AnsiColors` and related color infrastructure from `TimeWarp.Nuru.Repl` to the core `TimeWarp.Nuru` package, and add fluent extension methods for cleaner colored output. This enables consumers to use colored console output without requiring the REPL package, providing a lightweight alternative to Spectre.Console.

## Requirements

- Move `AnsiColors.cs` from `TimeWarp.Nuru.Repl/Display/` to `TimeWarp.Nuru/IO/`
- Add fluent string extension methods (`.Red()`, `.Green()`, `.Bold()`, etc.)
- Ensure REPL package continues to work (update imports)
- Maintain AOT compatibility (pure string manipulation)
- Keep `SyntaxColors` in REPL package (it's REPL-specific)

## Checklist

### Move AnsiColors
- [ ] Move `AnsiColors.cs` to `Source/TimeWarp.Nuru/IO/AnsiColors.cs`
- [ ] Update namespace if needed (already `TimeWarp.Nuru`)
- [ ] Update REPL project to reference from core package
- [ ] Verify REPL still compiles and works

### Add Extension Methods
- [ ] Create `Source/TimeWarp.Nuru/IO/AnsiColorExtensions.cs`
- [ ] Implement foreground color extensions (`.Red()`, `.Green()`, `.Yellow()`, `.Cyan()`, etc.)
- [ ] Implement bright color extensions (`.BrightRed()`, `.BrightGreen()`, etc.)
- [ ] Implement background color extensions (`.OnRed()`, `.OnGreen()`, `.OnBlue()`, etc.)
- [ ] Implement formatting extensions (`.Bold()`, `.Italic()`, `.Underline()`, `.Dim()`)
- [ ] Support chaining (`.Bold().Red()` or `.Red().Bold()`)
- [ ] Add XML documentation

### Testing
- [ ] Verify `TestTerminal` captures ANSI codes correctly
- [ ] Add tests for extension methods
- [ ] Verify existing REPL tests still pass

## Notes

### API Comparison

| Library | API Style | Example |
|---------|-----------|---------|
| Spectre.Console | Markup strings | `AnsiConsole.MarkupLine("[red]Hello[/]")` |
| Chalk (Node.js) | Chained methods | `chalk.red.bold("Hello")` |
| Kokuban (.NET) | Operator overload | `Chalk.Red + "Hello"` |
| **Nuru (proposed)** | Extensions | `"Hello".Red().Bold()` |

### Proposed Extension API

```csharp
// Foreground colors
"Error!".Red()
"Success!".Green()
"Warning".Yellow()
"Info".Cyan()

// Bright colors
"Highlight".BrightYellow()
"Important".BrightRed()

// Background colors
"Alert".OnRed()
"Selected".OnBlue()

// Formatting
"Header".Bold()
"Emphasis".Italic()
"Link".Underline()

// Chaining
"Critical Error".Red().Bold()
"Success".Green().OnWhite()
"Note".Dim().Italic()
```

### Implementation Pattern

```csharp
public static class AnsiColorExtensions
{
    public static string Red(this string text)
        => AnsiColors.Red + text + AnsiColors.Reset;

    public static string Bold(this string text)
        => AnsiColors.Bold + text + AnsiColors.Reset;

    public static string OnRed(this string text)
        => AnsiColors.BgRed + text + AnsiColors.Reset;

    // Chaining works because each method wraps with Reset,
    // and ANSI codes accumulate: "\x1b[1m\x1b[31mtext\x1b[0m\x1b[0m"
    // Multiple resets are harmless
}
```

### Files to Create/Modify

**Create:**
- `Source/TimeWarp.Nuru/IO/AnsiColorExtensions.cs`

**Move:**
- `Source/TimeWarp.Nuru.Repl/Display/AnsiColors.cs` → `Source/TimeWarp.Nuru/IO/AnsiColors.cs`

**Update:**
- `Source/TimeWarp.Nuru.Repl/Display/SyntaxColors.cs` (verify still compiles)
- `Source/TimeWarp.Nuru.Repl/Display/PromptFormatter.cs` (verify still compiles)
- `Source/TimeWarp.Nuru.Repl/Repl/ReplSession.cs` (verify still compiles)
- `Source/TimeWarp.Nuru.Repl/Input/ReplConsoleReader.cs` (verify still compiles)
- `Source/TimeWarp.Nuru.Repl/Input/TabCompletionHandler.cs` (verify still compiles)

### Why This Matters

1. **No Spectre.Console dependency** for basic colored output
2. **Lighter weight** - just string manipulation, no complex rendering
3. **AOT-friendly** - pure compile-time string operations
4. **Testable** - works with `TestTerminal` output capture
5. **Discoverable** - in core package, not hidden in REPL
