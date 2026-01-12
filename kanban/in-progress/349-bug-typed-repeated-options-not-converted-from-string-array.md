# Bug: Typed repeated options not converted from string array

## Description

When the source generator processes route patterns with typed repeated options (e.g., `{id:int}*`), the generated code passes `string[]` directly to handlers expecting typed arrays like `int[]`, `double[]`, `bool[]`, etc., causing CS1503 type conversion errors.

## Reproduction

**Files affected:** Multiple routing tests in `tests/timewarp-nuru-core-tests/routing/`

**Example patterns:**
```csharp
.Map("process --id {id:int}*").WithHandler((int[] id) => ...)
.Map("calc --values {v:double}*").WithHandler((double[] v) => ...)
.Map("docker run -i -t --env {e}* -- {*cmd}").WithHandler((bool i, bool t, string[] e, string[] cmd) => ...)
```

**Errors:**
```
error CS1503: Argument 1: cannot convert from 'string' to 'string[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'int[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'double[]'
```

## Root Cause Analysis

The `EmitValueOptionParsing` method in `route-matcher-emitter.cs` (lines 486-553):

1. **Only collects ONE value** - The loop breaks after finding the first occurrence (line 521: `break;`)
2. **Declares `string?` not `string[]`** - Line 510: `sb.AppendLine($"      string? {rawVarName} = null;");`
3. **Doesn't check `option.IsRepeated`** - The property exists but is never used

## Expected Behavior

For repeated options (`*`), the generator should:

1. Declare a `List<string>` to collect all values
2. Loop through ALL args collecting values (don't break early)
3. Convert to typed array after collection

**Expected generated code:**
```csharp
// For --env {e}*
List<string> __e_list = [];
for (int __idx = 0; __idx < routeArgs.Length; __idx++)
{
  if (routeArgs[__idx] == "--env" && __idx + 1 < routeArgs.Length && !routeArgs[__idx + 1].StartsWith("-"))
  {
    __e_list.Add(routeArgs[__idx + 1]);
    __idx++; // Skip the value
  }
}
string[] e = __e_list.ToArray();

// For typed --id {id:int}*
List<string> __id_list = [];
// ... collect as above ...
int[] id = __id_list.Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
```

## Plan

1. Modify `EmitValueOptionParsing` to check `option.IsRepeated`
2. If repeated:
   - Declare `List<string>` instead of `string?`
   - Don't break after finding first value
   - Continue collecting all occurrences
3. Modify `EmitOptionTypeConversion` to handle arrays:
   - For repeated options, convert each element with `.Select().ToArray()`
4. Test with various repeated option patterns

## Key Code Locations

- `route-matcher-emitter.cs:486-553` - `EmitValueOptionParsing` - needs to handle `IsRepeated`
- `route-matcher-emitter.cs:560-621` - `EmitOptionTypeConversion` - needs array conversion

## Checklist

- [ ] Add repeated option detection in `EmitValueOptionParsing`
- [ ] Emit `List<string>` collection for repeated options
- [ ] Don't break early - collect all values
- [ ] Add array type conversion in `EmitOptionTypeConversion`
- [ ] Support all primitive types: int, double, bool, DateTime, byte, short, long, float, decimal, Guid
- [ ] Clear caches and run CI tests: `ganda runfile cache --clear`
- [ ] Verify routing tests compile and pass
