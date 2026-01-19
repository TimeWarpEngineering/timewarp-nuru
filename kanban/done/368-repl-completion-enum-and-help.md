# REPL Completion Missing Enum Values and --help Option

## Status: Complete (15/15 tests passing)

## Description

The generated `GetCompletions()` method in `GeneratedReplRouteProvider` was incomplete.

**Fixed:**
1. **Enum parameter values** ✅ - After typing "deploy ", now shows Dev, Staging, Prod
2. **Built-in --help option** ✅ - Now always offered as a completion
3. **Position-aware enum completions** ✅ - Uses Roslyn to extract enum values at compile time
4. **Context-aware route options** ✅ - Shows route-specific options like `--verbose` for `build`
5. **Completion deduplication** ✅ - Fixed in #371, commands with shared prefixes no longer duplicate

## Test Status (15/15 passing)

All tests passing:
- ✓ `Should_show_enum_values_in_completions_after_deploy_space`
- ✓ `Should_show_help_option_in_completions_after_deploy_space`
- ✓ `Should_filter_enum_completions_with_partial_p`
- ✓ `Should_filter_enum_completions_with_partial_s`
- ✓ `Should_filter_enum_completions_with_partial_d`
- ✓ `Should_show_available_completions_on_first_tab`
- ✓ `Should_cycle_to_first_completion_on_second_tab`
- ✓ `Should_show_build_options_on_tab`
- ✓ `Should_show_option_after_git_commit_space`
- ✓ `Should_show_completions_and_autocomplete_unique_match`
- ✓ Plus 5 other tests

## Root Cause

In `repl-emitter.cs`, the `EmitGetCompletionsMethod()` function:
- Emits command prefix completions ✓
- Emits option completions (only when starting with `-`) ✓
- Does NOT emit enum value completions ✗
- Does NOT emit --help as a completion ✗
- Does NOT emit completions based on current parameter position ✗

## Implementation Plan

### Files to Modify

| File | Changes |
|------|---------|
| `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` | Combine model with compilation in pipeline |
| `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` | Add compilation param, pass to ReplEmitter |
| `source/timewarp-nuru-analyzers/generators/emitters/repl-emitter.cs` | Add --help, enum completions, position-aware logic |

### 1. Add --help Completion (Trivial)

In `EmitGetCompletionsMethod()` at line ~138 (after command completions):

```csharp
// --help is always available
sb.AppendLine("      if (string.IsNullOrEmpty(currentInput) || \"--help\".StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase))");
sb.AppendLine("        yield return new global::TimeWarp.Nuru.CompletionCandidate(\"--help\", \"Show help\", global::TimeWarp.Nuru.CompletionType.Option);");
```

### 2. Extract Enum Info from Handler Parameters

Add to `ReplEmitter`:
```csharp
record EnumParameterInfo(string CommandPrefix, int Position, string ParameterName, ImmutableArray<string> Values);

private static List<EnumParameterInfo> ExtractEnumParameters(
    IEnumerable<RouteDefinition> routes,
    Compilation compilation)
{
    var result = new List<EnumParameterInfo>();
    foreach (var route in routes)
    {
        if (route.Handler is null) continue;
        string cmdPrefix = string.Join(" ", route.Literals.Select(l => l.Value));

        int position = 0;
        foreach (var param in route.Handler.Parameters.Where(p => p.Source == BindingSource.Parameter))
        {
            // Look up type symbol from ParameterTypeName
            var typeSymbol = compilation.GetTypeByMetadataName(
                param.ParameterTypeName.Replace("global::", ""));

            if (typeSymbol?.TypeKind == TypeKind.Enum)
            {
                var values = typeSymbol.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => f.HasConstantValue)
                    .Select(f => f.Name)
                    .ToImmutableArray();

                result.Add(new EnumParameterInfo(cmdPrefix, position, param.ParameterName, values));
            }
            position++;
        }
    }
    return result;
}
```

### 3. Emit Position-Aware Enum Completions

In `EmitGetCompletionsMethod()`, after --help, add enum completion code:

```csharp
// For each route with enum parameters
foreach (var enumParam in enumParameters)
{
    sb.AppendLine($"      // Enum completions for '{enumParam.CommandPrefix}' parameter {enumParam.Position}");
    sb.AppendLine($"      if (prefix == \"{enumParam.CommandPrefix}\" || prefix.StartsWith(\"{enumParam.CommandPrefix} \"))");
    sb.AppendLine("      {");
    sb.AppendLine($"        int paramPos = args.Length - (hasTrailingSpace ? 0 : 1) - {enumParam.CommandPrefix.Split(' ').Length};");
    sb.AppendLine($"        if (paramPos == {enumParam.Position})");
    sb.AppendLine("        {");
    foreach (var value in enumParam.Values)
    {
        sb.AppendLine($"          if (\"{value}\".StartsWith(currentInput, global::System.StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine($"            yield return new global::TimeWarp.Nuru.CompletionCandidate(\"{value}\", null, global::TimeWarp.Nuru.CompletionType.Enum);");
    }
    sb.AppendLine("        }");
    sb.AppendLine("      }");
}
```

### 4. Pass Compilation Through Pipeline

**nuru-generator.cs** - Combine model with compilation:
```csharp
// Step 8: Combine with compilation for type resolution
context.RegisterSourceOutput(
    generatorModelWithDiagnostics.Combine(context.CompilationProvider),
    static (ctx, data) =>
    {
        var (modelWithDiags, compilation) = data;
        // ... existing diagnostic reporting ...
        string source = InterceptorEmitter.Emit(modelWithDiags.Model, compilation);
        ctx.AddSource("NuruGenerated.g.cs", source);
    });
```

**interceptor-emitter.cs** - Update signature and call:
```csharp
// Add compilation parameter
public static string Emit(GeneratorModel model, Compilation compilation)

// In EmitAppRouteMatcher(), pass to ReplEmitter:
ReplEmitter.Emit(sb, enrichedApp, methodSuffix, model.AttributedRoutes, compilation);
```

**repl-emitter.cs** - Update signature:
```csharp
public static void Emit(StringBuilder sb, AppModel app, string methodSuffix,
    ImmutableArray<RouteDefinition> attributedRoutes, Compilation compilation)
```

## Test Verification

```bash
dotnet run tests/timewarp-nuru-tests/repl/repl-17-sample-validation.cs
```

## Priority

Medium - Core REPL functionality works, but tab completion is less helpful
