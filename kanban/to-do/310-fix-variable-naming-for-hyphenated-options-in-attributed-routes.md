# Fix variable naming for hyphenated options in attributed routes

## Summary

Options with hyphens in their names (e.g., `--no-cache`) generate inconsistent variable names in the generated code. The flag parsing declares `noCache` (camelCase) but the property binding references `nocache` (all lowercase), causing CS0103 compile errors.

## Reproduction

**Source file:** `samples/attributed-routes/messages/docker/commands/docker-build-command.cs`

```csharp
[NuruRoute("build", Description = "Build an image from a Dockerfile")]
public sealed class DockerBuildCommand : DockerGroupBase, ICommand<Unit>
{
  [Parameter(Description = "Path to Dockerfile")]
  public string Path { get; set; } = string.Empty;

  [Option("tag", "t")]
  public string? Tag { get; set; }

  [Option("no-cache", null, Description = "Do not use cache")]  // <-- hyphenated
  public bool NoCache { get; set; }
  
  // ...
}
```

**Generated code (broken):**
```csharp
// Route: docker build
if (args.Length >= 2)
{
  if (args[0] != "build") goto route_skip_3;
  string path = args[1];
  string? tag = null;
  // ... tag parsing ...
  bool noCache = Array.Exists(args, a => a == "--no-cache");  // Declares 'noCache'
  
  global::AttributedRoutes.Messages.DockerBuildCommand __command = new()
  {
    Path = path,
    Tag = tag,
    NoCache = nocache,  // ERROR: References 'nocache' (lowercase)
  };
  // ...
}
```

**Error:**
```
error CS0103: The name 'nocache' does not exist in the current context
```

## Root Cause

There's an inconsistency between two parts of the generator:

1. **Flag parsing in `route-matcher-emitter.cs`:** Converts `no-cache` → `noCache` (proper camelCase with hyphen removed)
   ```csharp
   bool noCache = Array.Exists(args, a => a == "--no-cache");
   ```

2. **Property binding in `handler-invoker-emitter.cs`:** Converts property name to lowercase without considering hyphens
   ```csharp
   string varName = param.ParameterName.ToLowerInvariant();  // "NoCache" → "nocache"
   sb.AppendLine($"{indent}  {propName} = {varName},");
   ```

The variable naming conversion is inconsistent - one uses camelCase, the other uses lowercase.

## Solution

Ensure consistent variable naming throughout:

### Option 1: Standardize on camelCase (Preferred)

Update `handler-invoker-emitter.cs` to use proper camelCase conversion:

```csharp
// Instead of:
string varName = param.ParameterName.ToLowerInvariant();

// Use:
string varName = ToCamelCase(param.ParameterName);  // "NoCache" → "noCache"
```

Where `ToCamelCase` handles:
- `NoCache` → `noCache`
- `no-cache` → `noCache` (if using option name directly)

### Option 2: Store the variable name in the binding

When extracting options in `attributed-route-extractor.cs`, compute and store the variable name that will be used, ensuring consistency.

## Files to Modify

| File | Changes |
|------|---------|
| `generators/emitters/handler-invoker-emitter.cs` | Fix variable name conversion to match route-matcher |
| `generators/emitters/route-matcher-emitter.cs` | Verify/document variable naming convention |

## Checklist

- [ ] Identify the canonical variable naming convention (camelCase)
- [ ] Fix `handler-invoker-emitter.cs` to use consistent naming
- [ ] Add/use shared helper method for option name → variable name conversion
- [ ] Test options with hyphens: `--no-cache`, `--dry-run`, `--no-verify`
- [ ] Test options without hyphens: `--force`, `--verbose`
- [ ] Verify `docker-build-command.cs` compiles and runs

## Test Cases

```csharp
// Hyphenated options
[Option("no-cache", null)]
public bool NoCache { get; set; }  // Variable: noCache

[Option("dry-run", "n")]
public bool DryRun { get; set; }  // Variable: dryRun

[Option("no-verify", null)]
public bool NoVerify { get; set; }  // Variable: noVerify

// Non-hyphenated options (should still work)
[Option("force", "f")]
public bool Force { get; set; }  // Variable: force

[Option("verbose", "v")]
public bool Verbose { get; set; }  // Variable: verbose
```

## Notes

- Discovered during Task #304 Phase 12 while updating `attributed-routes` sample
- This is a variable naming consistency issue, not a logic issue
- The fix should be straightforward once we identify the canonical naming convention
- Blocks Task #304 completion
