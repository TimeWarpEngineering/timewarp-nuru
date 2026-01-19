## Agent Context Regions

Use `#region` blocks to embed context directly in source files. This ensures agents have relevant information when reading a file without needing separate documentation.

### Why

- **Context is local** - no hunting for separate docs
- **Stays in sync** - noticed when code changes
- **Regions collapse in IDEs** - not intrusive to humans
- **Token efficient** - focused context per file

### Standard Regions

#### For Source Files

```csharp
#region Purpose
// One-line description of what this file/class does
#endregion

#region Design
// Key design decisions and rationale
// Constraints and dependencies
// Why certain approaches were chosen over alternatives
#endregion
```

#### For Test Files

```csharp
#region Purpose
// What functionality is being tested
// What class/feature this validates
// Any prerequisites or context for understanding the tests
#endregion
```

### Best Practices

- **Be concise**: 5-10 lines max. Long regions defeat the purpose.
- **Focus on "why"**: Code shows "what" - regions explain "why"
- **Update when code changes**: Stale context is worse than none
- **Skip trivial files**: Not every file needs regions
- **Use for non-obvious decisions**: Document constraints an agent wouldn't infer from code alone
