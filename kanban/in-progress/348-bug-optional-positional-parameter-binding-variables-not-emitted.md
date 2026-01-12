# Bug: Optional positional parameter binding variables not emitted

## Description

When the source generator processes route patterns with optional positional parameters, it references binding variables (e.g., `boundDest`, `boundSource`) that are never declared or assigned, causing CS0103 "name does not exist" errors.

## Reproduction

**Files affected:** Multiple routing tests in `tests/timewarp-nuru-core-tests/routing/`

**Example patterns with optional positional parameters:**
```csharp
.Map("copy {source} {dest?}")
.Map("compress {source} --output {dest?}")
.Map("process {alpha} {beta?} {gamma?}")
```

**Errors:**
```
error CS0103: The name 'boundDest' does not exist in the current context
error CS0103: The name 'boundSource' does not exist in the current context
error CS0103: The name 'boundCompress' does not exist in the current context
error CS0103: The name 'boundAlpha' does not exist in the current context
error CS0103: The name 'boundBeta' does not exist in the current context
error CS0103: The name 'boundGamma' does not exist in the current context
error CS0103: The name 'boundFile' does not exist in the current context
error CS0103: The name 'boundVerbose' does not exist in the current context
error CS0103: The name 'boundOutput' does not exist in the current context
```

## Expected Behavior

The generator should emit variable declarations and assignments for all parameters before referencing them:
```csharp
string? boundDest = null;
if (args.Length > 2) {
  boundDest = args[2];
}
// Now safe to use boundDest
```

## Checklist

- [ ] Identify which emitter code path handles optional positional parameters
- [ ] Ensure binding variables are declared before use
- [ ] Ensure binding variables are assigned from args array
- [ ] Test with various optional parameter patterns
- [ ] Verify affected routing tests compile and pass

## Notes

- This appears to be a code generation ordering issue
- The variable is referenced but the declaration/assignment block is missing or out of order
- Affects `{param?}` syntax for optional positional parameters

## Files to Investigate

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- Look for optional parameter binding logic
