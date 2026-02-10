# Fix CS8803 error in fluent-syntax-examples.cs

## Description

The file `./samples/fluent/03-syntax/fluent-syntax-examples.cs` has a build error:
- Error: CS8803 - Top-level statements must precede namespace and type declarations
- Location: Line 279, Column 1

## Checklist

- [ ] Analyze the file structure to understand the ordering issue
- [ ] Fix the build error by moving top-level statements before namespace/type declarations
- [ ] Run `dotnet run ./samples/fluent/03-syntax/fluent-syntax-examples.cs` to verify the fix
- [ ] Ensure all MCP regions remain intact and extractable

## Notes

**Root Cause Analysis:**
The file contains both class definitions (for Endpoint DSL examples) and top-level statements (for Fluent DSL examples and app building). In C# 9+, top-level statements must come before any namespace or type declarations. The Fluent DSL examples at lines 279+ are placed after the Endpoint DSL class definitions, causing CS8803.

**Potential Fixes:**
1. **Option A:** Move the top-level statements (lines 269+ with Fluent DSL examples) to the TOP of the file, before the class definitions
2. **Option B:** Wrap the top-level statements inside a proper `Main` method within a class

**File Structure (current):**
- Lines 1-26: Shebang + comments + using statements
- Lines 27-268: Endpoint DSL examples (class definitions with [NuruRoute] attributes)
- Lines 269+: Fluent DSL examples (top-level statements using `builder.Map()...`)

**Verification Command:**
```bash
dotnet run ./samples/fluent/03-syntax/fluent-syntax-examples.cs
```

This file is used by the TimeWarp.Nuru MCP Server for extracting code snippets. All #region blocks must remain intact and the file must compile successfully.
