# Implement InputTokenizer

## Description

Implement the `InputTokenizer` component that parses raw command-line input into a structured `ParsedInput` record. This is the first stage of the new completion pipeline, responsible for understanding what the user has typed and what they're currently typing.

**Input**: Raw string like `"backup data --com"`
**Output**: Structured `ParsedInput` with completed words, partial word, and trailing space info

## Parent

062_Redesign-CompletionEngine-Architecture

## Requirements

- Parse input respecting quotes (single and double)
- Handle escape sequences within quotes
- Detect trailing whitespace accurately
- Identify partial word being typed vs completed words
- Handle edge cases: empty input, only spaces, mid-word cursor
- Case-preserving (don't modify case of input)
- Efficient parsing (no regex if possible)

## Checklist

- [ ] Create `ParsedInput` record in `TimeWarp.Nuru.Completion`
- [ ] Create `InputTokenizer` static class
- [ ] Implement `Parse(string input)` method
- [ ] Handle quoted strings (preserve content)
- [ ] Handle escape sequences (\", \', \\)
- [ ] Detect trailing whitespace
- [ ] Identify partial word (last word if no trailing space)
- [ ] Handle empty input case
- [ ] Handle multiple consecutive spaces
- [ ] Add comprehensive unit tests
- [ ] XML documentation for public API

## Notes

### Data Structure

```csharp
namespace TimeWarp.Nuru.Completion;

/// <summary>
/// Represents parsed command-line input for completion.
/// </summary>
/// <param name="CompletedWords">Words that are fully typed (space after them).</param>
/// <param name="PartialWord">The word currently being typed, or null if cursor is after a space.</param>
/// <param name="HasTrailingSpace">True if input ends with whitespace.</param>
public record ParsedInput(
  string[] CompletedWords,
  string? PartialWord,
  bool HasTrailingSpace
);
```

### Examples

| Input | CompletedWords | PartialWord | HasTrailingSpace |
|-------|----------------|-------------|------------------|
| `""` | [] | null | false |
| `"g"` | [] | "g" | false |
| `"git "` | ["git"] | null | true |
| `"git s"` | ["git"] | "s" | false |
| `"git status"` | ["git"] | "status" | false |
| `"git status "` | ["git", "status"] | null | true |
| `"backup data --com"` | ["backup", "data"] | "--com" | false |
| `"echo \"hello world\""` | ["echo"] | "\"hello world\"" | false |
| `"  "` | [] | null | true |

### Implementation Approach

```csharp
public static class InputTokenizer
{
  public static ParsedInput Parse(string input)
  {
    if (string.IsNullOrEmpty(input))
      return new ParsedInput([], null, false);
    
    bool hasTrailingSpace = char.IsWhiteSpace(input[^1]);
    
    // Use existing CommandLineParser.Parse() for word splitting
    string[] allWords = CommandLineParser.Parse(input);
    
    if (hasTrailingSpace || allWords.Length == 0)
    {
      return new ParsedInput(allWords, null, hasTrailingSpace);
    }
    
    // Last word is partial (no space after it)
    string[] completedWords = allWords[..^1];
    string partialWord = allWords[^1];
    
    return new ParsedInput(completedWords, partialWord, false);
  }
}
```

### Reuse Existing Code

The existing `CommandLineParser.Parse()` in `TimeWarp.Nuru.Repl` already handles:
- Quote parsing
- Escape sequences
- Word splitting

We can leverage this and just add the partial word detection logic.

### Test Cases

```csharp
// Empty input
InputTokenizer.Parse("").ShouldBe(new ParsedInput([], null, false));

// Single partial word
InputTokenizer.Parse("g").ShouldBe(new ParsedInput([], "g", false));

// Complete word with space
InputTokenizer.Parse("git ").ShouldBe(new ParsedInput(["git"], null, true));

// Multiple words, partial last
InputTokenizer.Parse("git sta").ShouldBe(new ParsedInput(["git"], "sta", false));

// Option being typed
InputTokenizer.Parse("build --ver").ShouldBe(new ParsedInput(["build"], "--ver", false));

// Quoted strings
InputTokenizer.Parse("echo \"hello").ShouldBe(new ParsedInput(["echo"], "\"hello", false));
```

### Location

Create in: `Source/TimeWarp.Nuru.Completion/Completion/InputTokenizer.cs`

### Success Criteria

- [ ] All example cases pass
- [ ] Handles quotes and escapes correctly
- [ ] Efficient (no unnecessary allocations)
- [ ] Well-documented
- [ ] Ready for RouteMatchEngine to consume
