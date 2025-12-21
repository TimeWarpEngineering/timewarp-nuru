# Fix source generator to preserve file-scoped static usings

## Description

The `NuruDelegateCommandGenerator` copies delegate bodies into generated code, but the generated code doesn't have access to file-scoped `using static` directives from the source file. This causes compilation errors when delegates use unqualified method calls like `WriteLine` that depend on `using static System.Console;`.

## Checklist

- [ ] Investigate `NuruDelegateCommandGenerator` in `source/timewarp-nuru-analyzers/`
- [ ] Detect file-scoped `using static` directives in source files containing Map() calls
- [ ] Include detected static usings in generated code output
- [ ] Test with `samples/aot-example/aot-example.cs` which uses `using static System.Console;`
- [ ] Verify generated code compiles without CS0103 errors

## Notes

### Symptom

`samples/aot-example/aot-example.csproj` fails to build with:
```
error CS0103: The name 'WriteLine' does not exist in the current context
```

The error occurs in generated file `GeneratedDelegateCommands.g.cs` at multiple locations.

### Root Cause

The source file `aot-example.cs` has:
```csharp
using static System.Console;
```

But this is file-scoped and not available to the generated code in `GeneratedDelegateCommands.g.cs`.

### Solution Approach

Global usings are fine since they apply to all files in the project. File-scoped static usings need to be detected by the source generator and duplicated in the generated output.

### Related

Split from task 231 (Fix 7 failing sample tests) since this is a generator bug, not a sample issue.
