# Test consumer override of --version route

## Summary

Add a test to verify behavior when a consumer of Nuru maps their own `--version` route, overriding the built-in one registered by `CreateBuilder()`.

## Todo List

- [ ] Add test verifying consumer can override `--version` route
- [ ] Verify warning message is displayed for duplicate route
- [ ] Verify consumer's handler is used (not the built-in one)
- [ ] Document expected behavior in user docs

## Notes

Current behavior in `EndpointCollection.Add()`:
- Duplicate routes print a warning to stderr
- The new handler overrides the previous one (consumer wins)

Example scenario:
```csharp
NuruApp.CreateBuilder(args)  // registers --version via UseAllExtensions
  .Map("--version", () => Console.WriteLine("My custom version"))  // overrides it
```

Consider whether this is the desired behavior or if consumers should use `DisableVersionRoute = true` first.

## Blocked

Test infrastructure issue: New test files in `tests/timewarp-nuru-tests/` fail to compile because `Directory.Build.props` includes `lexer-test-helper.cs` which references internal types (`Lexer`, `Token`). Existing cached tests work but new tests fail. Need to fix the test infrastructure before adding these tests.

Attempted test file: `routing-20-version-route-override.cs` (removed due to build failures)
