# Fix catch-all parameter variable name collision with args

## Summary

When a catch-all parameter property is named `Args`, the generated variable name `args` shadows the interceptor method's parameter, causing CS0841 and CS0136 compile errors.

## Reproduction

**Source file:** `samples/attributed-routes/messages/commands/exec-command.cs`

```csharp
[NuruRoute("exec", Description = "Execute a command with arguments")]
public sealed class ExecCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Command and arguments")]
  public string[] Args { get; set; } = [];  // <-- Property named "Args"
  
  // ...
}
```

**Generated code (broken):**
```csharp
public static async Task<int> RunAsync_Intercepted(
  this NuruCoreApp app,
  string[] args)  // Method parameter named 'args'
{
  // ...
  
  // Route: exec
  if (args.Length >= 1)
  {
    if (args[0] != "exec") goto route_skip_8;
    string[] args = args[1..];  // ERROR: Shadows parameter AND uses itself before declaration
    
    global::AttributedRoutes.Messages.ExecCommand __command = new()
    {
      Args = args,
    };
    // ...
  }
}
```

**Errors:**
```
error CS0841: Cannot use local variable 'args' before it is declared
error CS0136: A local or parameter named 'args' cannot be declared in this scope because that name is used in an enclosing local scope
```

## Root Cause

The generator derives the local variable name from the property name:

**In `route-matcher-emitter.cs`:**
```csharp
// For catch-all parameters
string varName = param.CamelCaseName;  // "Args" → "args"
sb.AppendLine($"string[] {varName} = args[{paramIndex}..];");
```

When the property is named `Args`, the derived variable name `args` collides with the interceptor method's `args` parameter.

## Solution

### Option 1: Use prefixed variable names for all catch-all parameters

Always use a unique prefix for catch-all variables:

```csharp
// Instead of:
string[] args = args[1..];

// Generate:
string[] __catchAll_args = args[1..];
// or
string[] __args_0 = args[1..];
```

Then in property binding:
```csharp
Args = __catchAll_args,
```

### Option 2: Check for reserved names and prefix only when needed

```csharp
string varName = param.CamelCaseName;
if (varName == "args" || varName == "app" || /* other reserved names */)
{
  varName = $"__{varName}";
}
```

### Option 3: Always use route-indexed variable names (consistent with other params)

The generator already uses `__varName_routeIndex` for other parameters to avoid conflicts. Apply the same pattern to catch-all:

```csharp
string varName = $"__{param.CamelCaseName}_{routeIndex}";
// Generates: string[] __args_8 = args[1..];
```

## Files to Modify

| File | Changes |
|------|---------|
| `generators/emitters/route-matcher-emitter.cs` | Use unique variable names for catch-all |
| `generators/emitters/handler-invoker-emitter.cs` | Use matching variable names in property binding |

## Reserved Names to Avoid

These names are used in the generated interceptor and should never be used as local variables:

- `args` - method parameter
- `app` - method parameter
- `configuration` - local variable for IConfiguration
- `__handler` - handler instance
- `__command` - command instance
- `result` - handler result

## Checklist

- [ ] Identify all reserved/conflicting variable names in generated code
- [ ] Update catch-all variable naming to use unique prefix or route index
- [ ] Update property binding to use matching variable names
- [ ] Test with property named `Args`
- [ ] Test with property named `App` (if that could also conflict)
- [ ] Verify `exec-command.cs` compiles and runs

## Test Cases

```csharp
// Should not conflict with method parameter
[Parameter(IsCatchAll = true)]
public string[] Args { get; set; } = [];

// Other potential conflicts to test
[Parameter(IsCatchAll = true)]
public string[] Arguments { get; set; } = [];  // "arguments" - OK

[Parameter]
public string App { get; set; } = "";  // "app" - potential conflict!
```

## Notes

- Discovered during Task #304 Phase 12 while updating `attributed-routes` sample
- This affects any property whose camelCase name matches a reserved identifier
- The fix should use consistent naming with other generated variables (route-indexed)
- Blocks Task #304 completion
