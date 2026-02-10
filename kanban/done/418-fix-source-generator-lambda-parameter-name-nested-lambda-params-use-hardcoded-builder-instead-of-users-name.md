# Fix source generator lambda parameter name: nested lambda params use hardcoded 'builder' instead of user's name

## Description

The source generator emits `LoggerFactory.Create(builder => { ... })` for AddLogging configuration, but uses a hardcoded `builder` parameter name instead of preserving the user's lambda parameter name. When the user writes `b => b.AddConsole()`, the generated code produces `builder => { b.AddConsole(); }` — `b` is undefined in the generated scope, causing CS0103.

### Reproduction

`samples/fluent/05-pipeline/fluent-pipeline-filtered-auth.cs` uses the short form:
```csharp
.ConfigureServices(s => s.AddLogging(b => b.AddConsole()))
```

This produces generated code at line 72-74:
```csharp
private static readonly ILoggerFactory __loggerFactory =
  LoggerFactory.Create(builder =>
  {
    b.AddConsole();  // CS0103: 'b' does not exist
  });
```

The generator extracts the lambda body (`b.AddConsole()`) but wraps it in a new lambda with hardcoded parameter name `builder`.

## Checklist

- [ ] Locate the source generator code that handles AddLogging lambda extraction
- [ ] Identify where the hardcoded 'builder' parameter name is emitted
- [ ] Modify the generator to preserve the user's lambda parameter name
- [ ] OR modify the generator to rewrite references in the body to match the emitted parameter name
- [ ] Verify `samples/fluent/05-pipeline/fluent-pipeline-filtered-auth.cs` builds cleanly
- [ ] Test with various parameter names (b, x, loggingBuilder, builder, etc.)

## Notes

- Workaround: use `builder` as the parameter name to match the generator's hardcoded name
- The bug is in the logging/service configuration code emission, not in route handler emission
- Look at how the generator extracts and re-emits the AddLogging lambda
- This affects any nested lambda in service configuration where parameter names differ
