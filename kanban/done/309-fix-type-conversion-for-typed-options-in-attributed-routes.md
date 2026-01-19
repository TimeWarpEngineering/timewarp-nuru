# Fix type conversion for typed options in attributed routes

## Summary

When an attributed route has an `[Option]` with a non-string property type (e.g., `int`, `double`, `bool`, `Guid`), the generator must emit type conversion code. Currently it assigns the raw string value directly, causing CS0029 compile errors.

## Reproduction

**Source file:** `samples/attributed-routes/messages/commands/deploy-command.cs`

```csharp
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter(Description = "Target environment")]
  public string Env { get; set; } = string.Empty;

  [Option("force", "f")]
  public bool Force { get; set; }

  [Option("config", "c")]
  public string? ConfigFile { get; set; }

  [Option("replicas", "r", Description = "Number of replicas")]
  public int Replicas { get; set; } = 1;  // <-- int type
  
  // ...
}
```

**Generated code (broken):**
```csharp
string? replicas = null;
for (int __idx = 0; __idx < args.Length - 1; __idx++)
{
  if (args[__idx] == "--replicas" || args[__idx] == "-r")
  {
    replicas = args[__idx + 1];
    break;
  }
}
// ...
global::AttributedRoutes.Messages.DeployCommand __command = new()
{
  Env = env,
  Force = force,
  ConfigFile = configfile,
  Replicas = replicas,  // ERROR: Cannot convert string to int
};
```

**Error:**
```
error CS0029: Cannot implicitly convert type 'string' to 'int'
```

## Root Cause

The `handler-invoker-emitter.cs` `EmitCommandInvocation()` method assigns route parameters and options directly to command properties without type conversion:

```csharp
foreach (ParameterBinding param in handler.RouteParameters)
{
  string propName = ToPascalCase(param.ParameterName);
  string varName = param.ParameterName.ToLowerInvariant();
  sb.AppendLine($"{indent}  {propName} = {varName},");  // No conversion!
}
```

For **route parameters** with type constraints (e.g., `{n:int}`), conversion is handled by `route-matcher-emitter.cs` `EmitTypeConversions()`. But for **options**, there's no equivalent - the option value is extracted as a string and never converted.

## Solution

### Option 1: Extend EmitCommandInvocation to handle typed options

1. In `attributed-route-extractor.cs`, extract the property type for each `[Option]`:
   ```csharp
   public record OptionBinding(
     string OptionName,
     string? ShortName,
     string PropertyName,
     string PropertyTypeName,  // NEW: "int", "bool", "double", etc.
     bool IsFlag,
     string? DefaultValue);
   ```

2. In `handler-invoker-emitter.cs`, emit type conversion when assigning to command properties:
   ```csharp
   string value = propertyType switch
   {
     "int" => $"int.Parse({varName}, System.Globalization.CultureInfo.InvariantCulture)",
     "double" => $"double.Parse({varName}, System.Globalization.CultureInfo.InvariantCulture)",
     "bool" => varName,  // Already bool from flag detection
     "string" or "string?" => varName,
     _ => varName  // Unknown type, pass through
   };
   sb.AppendLine($"{indent}  {propName} = {value},");
   ```

### Option 2: Emit type conversion at option extraction time

Similar to how `EmitTypeConversions` works for route parameters, add type conversion immediately after extracting option values in `route-matcher-emitter.cs`.

## Files to Modify

| File | Changes |
|------|---------|
| `generators/extractors/attributed-route-extractor.cs` | Extract property type for options |
| `generators/models/option-definition.cs` or `parameter-binding.cs` | Add property type field |
| `generators/emitters/handler-invoker-emitter.cs` | Emit type conversion for typed options |
| `generators/emitters/route-matcher-emitter.cs` | Alternative: emit conversion at extraction |

## Checklist

- [ ] Extract property type for `[Option]` attributes in extractor
- [ ] Store property type in option/binding model
- [ ] Emit type conversion code for non-string option types
- [ ] Handle nullable types (`int?`, `double?`)
- [ ] Handle default values when option not provided
- [ ] Test with `int`, `double`, `bool`, `Guid`, `DateTime` option types
- [ ] Verify `deploy-command.cs` compiles and runs

## Test Cases

```csharp
[Option("count", "c")]
public int Count { get; set; }  // Should parse to int

[Option("threshold", "t")]
public double Threshold { get; set; }  // Should parse to double

[Option("id")]
public Guid? Id { get; set; }  // Should parse to Guid, null if not provided

[Option("since")]
public DateTime? Since { get; set; }  // Should parse to DateTime
```

## Notes

- Discovered during Task #304 Phase 12 while updating `attributed-routes` sample
- This only affects attributed routes (`[NuruRoute]` + `[Option]`), not fluent DSL routes
- The fluent DSL handles type conversion in the route pattern (e.g., `--count {count:int}`)
- Blocks Task #304 completion
