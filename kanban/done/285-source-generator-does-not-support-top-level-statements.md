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

## Checklist

- [x] Update `AppExtractor.FindContainingBlock` to detect top-level statement context
- [x] Handle `GlobalStatementSyntax` / `CompilationUnitSyntax` in the interpreter
- [x] Test with `samples/hello-world/hello-world.cs`
- [x] Test with other top-level statement samples

## Files Modified

| File | Change |
|------|--------|
| `generators/extractors/app-extractor.cs` | Added `FindCompilationUnit` and fall back to `InterpretTopLevelStatements` when no `BlockSyntax` |
| `generators/interpreter/dsl-interpreter.cs` | Added `InterpretTopLevelStatements(CompilationUnitSyntax)` method |
| `samples/Directory.Build.props` | Added `InterceptorsNamespaces` and direct analyzer `ProjectReference` |

## Results

Top-level statement support now works correctly:
- `samples/hello-world/hello-world.cs` - outputs "Hello World"
- `tests/temp-top-level-test/temp-top-level-test.cs` - outputs "Hello World"

### Implementation Details

1. **AppExtractor**: When `FindContainingBlock` returns null, we now check for `CompilationUnitSyntax` and call `InterpretTopLevelStatements`

2. **DslInterpreter**: New method `InterpretTopLevelStatements` iterates over `GlobalStatementSyntax` members and processes each statement

3. **samples/Directory.Build.props**: Two changes required:
   - Added `InterceptorsNamespaces` property (enables interceptor feature)
   - Added direct analyzer `ProjectReference` (analyzer references don't flow transitively)
