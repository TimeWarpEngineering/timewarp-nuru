# Fix help output to use assembly name when WithName() not called

## Description

Currently, if `.WithName()` is not called, the help header shows nothing for the app name. The `USAGE:` line falls back to "app", but the colored header at the top is missing.

This task implements a proper fallback that uses the assembly name from `Assembly.GetEntryAssembly()` when `model.Name` is not explicitly set.

## Current Behavior

```
USAGE: app [command] [options]          <- Falls back to "app"

COMMANDS
...
```

## Expected Behavior

```
MyApp v1.0.0                            <- Shows assembly name
Development CLI for MyApp

USAGE: MyApp [command] [options]       <- Shows actual name

COMMANDS
...
```

## Changes Required

### Update help-emitter.cs

Modify `EmitHeader()` to emit runtime code that tries multiple fallbacks:

```csharp
private static void EmitHeader(StringBuilder sb, AppModel model)
{
  // Emit code that gets app name with proper fallback
  sb.AppendLine("    string __appName = terminal.GetType().GetProperty(\"AppName\", BindingFlags.Public | BindingFlags.Instance)?.GetValue(terminal) as string ?? global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? \"app\";");

  // Emit version if available
  if (model.Version is not null)
  {
    sb.AppendLine("    terminal.WriteLine($\"  {__appName} v{model.Version}\".BrightCyan().Bold());");
  }
  else
  {
    sb.AppendLine("    terminal.WriteLine(__appName.BrightCyan().Bold());");
  }

  // App description in gray
  if (model.Description is not null)
  {
    sb.AppendLine($"    terminal.WriteLine(\"{EscapeString(model.Description)}\".Gray());");
  }

  sb.AppendLine("    terminal.WriteLine();");
}
```

Wait - this is getting complex. The better approach is to update the model to include the assembly name at compile time.

### Alternative: Set AssemblyName in AppModel at Build Time

The interceptor already has access to assembly information. We should pass it to the model:

1. **Update AppModel** to track whether Name was explicitly set
2. **Update interceptor** to:
   - Detect if `.WithName()` was called
   - If not, extract assembly name from the build
   - Set `model.Name` to assembly name at compile time

This is cleaner because the generated code doesn't need complex runtime logic.

## Files to Modify

| File | Changes |
|------|---------|
| `help-emitter.cs` | Update `EmitHeader()` to show app name (with version if available) |
| `nuru-generator.cs` or `interceptor-emitter.cs` | Pass assembly name to model when Name not explicitly set |

## Implementation Approach

### Step 1: Update AppModel
Add a flag to track if Name was explicitly set vs auto-detected:

```csharp
public sealed record AppModel(
  ...
  string? Name,
  bool NameIsExplicit,  // NEW: true if WithName() was called
  ...
)
```

### Step 2: Update nuru-generator.cs
Pass the assembly name when Name is not explicit:

```csharp
// In CreateModel or similar
string? name = model.Name;
bool nameIsExplicit = ...; // Track this during IR building

// If not explicit, try to get assembly name from compilation
if (!nameIsExplicit)
{
  name = compilation.AssemblyName ?? "app";
}

AppModel finalModel = model with
{
  Name = name,
  NameIsExplicit = nameIsExplicit
};
```

### Step 3: Update help-emitter.cs
Emit the header with proper name:

```csharp
private static void EmitHeader(StringBuilder sb, AppModel model)
{
  // App name with version in cyan bold
  string version = model.Version ?? "1.0.0";
  sb.AppendLine($"    terminal.WriteLine(\"{EscapeString(model.Name ?? "app")} v{version}\".BrightCyan().Bold());");

  // App description in gray
  if (model.Description is not null)
  {
    sb.AppendLine($"    terminal.WriteLine(\"{EscapeString(model.Description)}\".Gray());");
  }

  sb.AppendLine("    terminal.WriteLine();");
}
```

### Step 4: Update EmitUsage()
Use the same app name:

```csharp
private static void EmitUsage(StringBuilder sb, AppModel model)
{
  string appName = model.Name ?? "app";
  sb.AppendLine($"    terminal.WriteLine(\"USAGE: {EscapeString(appName)} [command] [options]\".Yellow());");
  sb.AppendLine("    terminal.WriteLine();");
}
```

## Checklist

- [ ] Add `NameIsExplicit` tracking to IR/AppModel
- [ ] Pass assembly name to model when Name not explicit
- [ ] Update `EmitHeader()` to show app name (not null check)
- [ ] Update `EmitUsage()` to use model.Name (consistent)
- [ ] Test with a sample that doesn't call `.WithName()`
- [ ] Verify existing tests still pass

## Testing

Test with samples that don't use `.WithName()`:
- `./samples/endpoints/03-syntax/syntax.cs --help`

Expected output should show assembly name instead of blank/nothing.

## Related Tasks

- #413 - Polish help output with colors (completed - introduced the issue)
- #400 - Use terminal.WriteTable (completed)
