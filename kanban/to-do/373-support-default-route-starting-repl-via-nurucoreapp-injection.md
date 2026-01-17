# Support Default Route Starting REPL Via NuruCoreApp Injection

## Description

Enable users to define a default route `Map("")` that starts the REPL when no arguments are provided. The ideal API would be:

```csharp
.Map("")
  .WithHandler(async (NuruCoreApp app) => await app.RunReplAsync())
  .WithDescription("Start interactive REPL mode")
  .AsCommand()
  .Done()
```

## Current Behavior

When attempting this pattern, the generator produces broken code:

```csharp
// Generated (BROKEN):
async Task __handler_0(global::TimeWarp.Nuru.NuruCoreApp nuruApp) => await await nuruApp.RunReplAsync();
await __handler_0(nuruApp);  // ERROR: 'nuruApp' doesn't exist in scope
```

## Issues Found

1. **Double await**: The generator emits `await await nuruApp.RunReplAsync()` instead of `await nuruApp.RunReplAsync()`

2. **Variable scope mismatch**: The generated local function uses parameter name `nuruApp`, but the invocation also uses `nuruApp` instead of the actual variable `app` that's in scope

## Root Cause

The handler invoker emitter (`handler-invoker-emitter.cs`) doesn't correctly handle:
- Service parameters (like `NuruCoreApp`) when generating local function invocations
- The async/await inference when the handler already has `await` in the body

## Relevant Files

- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` - Emits handler invocations
- `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs` - Has special case for NuruCoreApp (line 57-64)
- `source/timewarp-nuru-analyzers/generators/extractors/handler-extractor.cs` - Extracts handler parameters

## Workaround

Until fixed, users can use if/else at the end of their program:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .AddRepl(...)
  .Map("command1").WithHandler(...).Done()
  .Build();

if (args.Length == 0)
{
  await app.RunReplAsync();
  return 0;
}
return await app.RunAsync(args);
```

## Checklist

- [ ] Fix handler-invoker-emitter to use correct variable name for service parameters
- [ ] Fix double-await issue when handler body already contains await
- [ ] Add support for if statements in DSL interpreter (so RunReplAsync inside if blocks is intercepted)
- [ ] Add test case for `Map("")` with `NuruCoreApp` service injection
- [ ] Add test case for handler calling `RunReplAsync()`
- [ ] Update repl-basic-demo.cs to use the clean route-based approach

## Additional Finding: DSL Interpreter Ignores If Statements

The interpreter in `dsl-interpreter.cs` (lines 296-299) explicitly ignores if statements:

```csharp
// Ignore other statement types (if, etc.)
default:
  break;
```

This means `RunReplAsync()` calls inside if blocks are never discovered:

```csharp
// This RunReplAsync call is NOT intercepted:
if (args.Length == 0)
{
  await app.RunReplAsync();  // Invisible to interpreter!
}
```

## Workaround Used in repl-basic-demo.cs

Instead of calling `RunReplAsync()` conditionally, modify args before passing to `RunAsync()`:

```csharp
// Convert empty args to "-i" flag which is handled by RunAsync
string[] effectiveArgs = args.Length == 0 ? ["-i"] : args;
await app.RunAsync(effectiveArgs);
```

## Notes

The `NuruCoreApp` injection is already supported in `service-resolver-emitter.cs` (lines 57-64):

```csharp
// Special case: NuruCoreApp - the app instance is directly available
if (IsNuruCoreAppType(typeName))
{
  sb.AppendLine($"{indent}{typeName} {varName} = app;");
  return;
}
```

The issue is in how the handler invoker emitter passes this resolved value to the local function.
