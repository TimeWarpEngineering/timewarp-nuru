# Migrate command-line-parser tests to Jaribu multi-mode

## Description

Migrate the 2 test files in `tests/timewarp-nuru-repl-tests/command-line-parser/` to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner.

## Files to Migrate

- [ ] `parser-01-basic-parsing.cs` (8 tests)
- [ ] `parser-02-quoted-strings.cs` (11 tests)

## Checklist

- [ ] Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block at top
- [ ] Wrap types in namespace block (e.g., `TimeWarp.Nuru.Tests.CommandLineParser.BasicParsing`)
- [ ] Add `[ModuleInitializer]` registration method to test class
- [ ] Add files to `tests/ci-tests/Directory.Build.props`
- [ ] Test standalone mode: `dotnet tests/timewarp-nuru-repl-tests/command-line-parser/parser-01-basic-parsing.cs`
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Commit changes

## Implementation Pattern

**Before:**
```csharp
#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Test CommandLineParser basic parsing functionality
return await RunTests<CommandLineParserBasicTests>();

[TestTag("CommandLineParser")]
public class CommandLineParserBasicTests
{
  // ...
}
```

**After:**
```csharp
#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Test CommandLineParser basic parsing functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.CommandLineParser.BasicParsing
{

[TestTag("CommandLineParser")]
public class CommandLineParserBasicTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CommandLineParserBasicTests>();
  // ...
}

} // namespace TimeWarp.Nuru.Tests.CommandLineParser.BasicParsing
```

## Notes

- These tests reference only `timewarp-nuru-repl.csproj`, which is already included in ci-tests
- The tests use the `CommandLineParser` type from the REPL project
- Current CI test count: 527 tests. After migration: ~546 tests (adding 19 tests)
