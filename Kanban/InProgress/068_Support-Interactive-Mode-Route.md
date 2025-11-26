# Support Interactive Mode Route

## Description

Enable applications to support both CLI and REPL modes by adding a route that triggers interactive mode. Users can define a route like `--interactive` or `-i` that will enter REPL mode while using the same app configuration.

The implementation adds an `AddInteractiveRoute` extension method that registers a route to start REPL mode, allowing apps to be run normally from CLI but also support being run as a REPL.

## Requirements

- Add `AddInteractiveRoute` extension method to `NuruAppBuilder`
- The method should accept customizable route patterns (default: `--interactive,-i`)
- The route handler must properly initialize REPL with the existing app configuration
- Must work with both DI and non-DI app configurations
- Must integrate with existing `AddReplSupport` configuration

## Checklist

### Implementation
- [x] Rename `LoggerServiceProvider` to `LightweightServiceProvider`
- [ ] Extend `LightweightServiceProvider` to include `NuruApp` reference
- [ ] Modify `NuruApp` non-DI constructor to pass `this` to provider
- [ ] For DI path: register `NuruApp` via holder pattern in `Build()`
- [ ] Add `AddInteractiveRoute` extension method to `NuruAppExtensions.cs`
- [ ] Add static `StartInteractiveModeAsync(NuruApp app)` handler
- [ ] Support custom route patterns

### Documentation
- [ ] Add sample demonstrating CLI + interactive mode
- [ ] Update existing REPL demo to show the pattern

## Notes

### Implementation Decision

The route handler needs access to `NuruApp` to call `RunReplAsync()`. The solution leverages existing delegate parameter injection via `IServiceProvider`:

1. **Non-DI path**: Rename `LoggerServiceProvider` to `LightweightServiceProvider` and extend it to also resolve `NuruApp`. The constructor passes `this` to the provider.

2. **DI path**: Use `NuruAppHolder` pattern - register holder in DI, populate it after `Build()` creates the app.

3. **Handler**: A static method `StartInteractiveModeAsync(NuruApp app)` receives the app via injection and calls `app.RunReplAsync()`.

This follows the existing pattern where `DelegateExecutor.BindParameters` resolves service parameters via `serviceProvider.GetService(param.ParameterType)`.

### Example usage:
```csharp
var app = new NuruAppBuilder()
  .AddRoute("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  .AddRoute("status", () => Console.WriteLine("OK"))
  .AddReplSupport(options => options.Prompt = "myapp> ")
  .AddInteractiveRoute() // Adds --interactive,-i route
  .Build();

return await app.RunAsync(args);
```

When user runs:
- `myapp greet Alice` - executes greeting command
- `myapp --interactive` - enters REPL mode
- `myapp -i` - enters REPL mode (short form)
