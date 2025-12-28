# Source generator does not support top-level statements

## Description

The DslInterpreter expects a `BlockSyntax` (method body), but top-level statements don't have one. They are `GlobalStatementSyntax` nodes directly under `CompilationUnitSyntax`.

This causes samples like `hello-world.cs` to fail with:
```
System.InvalidOperationException: RunAsync was not intercepted. Ensure the source generator is enabled.
```

## Symptom

```bash
./samples/hello-world/hello-world.cs
# Unhandled exception. System.InvalidOperationException: RunAsync was not intercepted.
```

## Root Cause

`AppExtractor.FindContainingBlock()` walks up the syntax tree looking for a `BlockSyntax`, but top-level statements are wrapped in `GlobalStatementSyntax` nodes that are children of `CompilationUnitSyntax` - there is no `BlockSyntax`.

```csharp
// Current code - fails for top-level statements
private static BlockSyntax? FindContainingBlock(RoslynSyntaxNode node)
{
  RoslynSyntaxNode? current = node.Parent;
  while (current is not null)
  {
    if (current is BlockSyntax block)
      return block;
    current = current.Parent;
  }
  return null;  // <-- Returns null for top-level statements
}
```

## Proposed Fix

Modify `AppExtractor` to handle top-level statements:

1. Detect when we're in top-level statement context (no `BlockSyntax` found, but `CompilationUnitSyntax` ancestor exists)
2. Collect all `GlobalStatementSyntax` nodes from the `CompilationUnitSyntax`
3. Either:
   - Create a synthetic `BlockSyntax` containing those statements, OR
   - Add an `Interpret(CompilationUnitSyntax)` overload to `DslInterpreter`

## Checklist

- [ ] Update `AppExtractor.FindContainingBlock` to detect top-level statement context
- [ ] Handle `GlobalStatementSyntax` / `CompilationUnitSyntax` in the interpreter
- [ ] Test with `samples/hello-world/hello-world.cs`
- [ ] Test with other top-level statement samples

## Files to Modify

| File | Change |
|------|--------|
| `generators/extractors/app-extractor.cs` | Handle top-level statements in `FindContainingBlock` |
| `generators/interpreter/dsl-interpreter.cs` | Possibly add `Interpret(CompilationUnitSyntax)` overload |

## Notes

The generator tests in `generator-01-intercept.cs` pass because they use a test harness that wraps code in a method body with `BlockSyntax`. Real-world top-level statement usage was not tested.
