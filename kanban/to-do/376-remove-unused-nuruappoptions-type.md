# Remove unused NuruAppOptions type

## Description

The `NuruAppOptions` class and its `ConfigureRepl` property are dead code. The source generator only recognizes `ReplOptions` (mirrored as `ReplModel`), set via the fluent `.AddRepl(Action<ReplOptions>)` API. The `NuruAppOptions` parameter in `NuruApp.CreateBuilder` is discarded (`_ = nuruAppOptions`) and never used.

## Checklist

- [ ] Remove `NuruAppOptions` class from `nuru-app-options.cs`
- [ ] Remove `NuruAppOptions` parameter from `NuruApp.CreateBuilder` signature
- [ ] Update `NuruApp.CreateBuilder` documentation to remove `NuruAppOptions` example
- [ ] Update `NuruApp` class documentation to remove `NuruAppOptions` example
- [ ] Search for any other references and remove
- [ ] Verify build succeeds

## Notes

The correct API for REPL configuration is:
```csharp
NuruApp.CreateBuilder(args)
  .AddRepl(options => { /* configure */ })
  .Build();
```

The old (unused) API was:
```csharp
NuruApp.CreateBuilder(args, new NuruAppOptions { ConfigureRepl = options => { /* configure */ } })
  .Build();
```
