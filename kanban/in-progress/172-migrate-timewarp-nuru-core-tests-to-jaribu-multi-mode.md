# Migrate timewarp-nuru-core-tests to Jaribu multi-mode

## Description

Migrate the remaining test files in `tests/timewarp-nuru-core-tests/` to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner. The lexer tests are already migrated; this task covers the remaining 25 test files.

## Current Status

**CI Test Count:** 696 tests (added 17 from ansi-string-utils-01)

## Files Status

### Already Migrated
- [x] `lexer/*.cs` (16 files) - Already in CI via glob pattern
- [x] `compiled-route-test-helper.cs` - Helper file (already has namespace)
- [x] `ansi-string-utils-01-basic.cs` - Migrated (17 tests)

### Excluded (not a test file)
- [x] `show-help-colors.cs` - Demo/utility script, excluded from CI

### Files Still Need Migration (22 files)

#### ANSI String Utils (1 file)
- [ ] `ansi-string-utils-02-wrap-text.cs`

#### Help Provider (5 files)
- [ ] `help-provider-01-option-detection.cs`
- [ ] `help-provider-02-filtering.cs`
- [ ] `help-provider-03-session-context.cs`
- [ ] `help-provider-04-default-route.cs`
- [ ] `help-provider-05-color-output.cs`

#### Widgets (10 files)
- [ ] `hyperlink-01-basic.cs`
- [ ] `panel-widget-01-basic.cs`
- [ ] `panel-widget-02-terminal-extensions.cs`
- [ ] `panel-widget-03-word-wrap.cs`
- [ ] `rule-widget-01-basic.cs`
- [ ] `rule-widget-02-terminal-extensions.cs`
- [ ] `table-widget-01-basic.cs`
- [ ] `table-widget-02-borders.cs`
- [ ] `table-widget-03-styling.cs`
- [ ] `table-widget-04-expand.cs`

#### Core Components (6 files)
- [ ] `invoker-registry-01-basic.cs`
- [ ] `message-type-01-fluent-api.cs`
- [ ] `message-type-02-help-output.cs`
- [ ] `nuru-route-registry-01-basic.cs` - **Special: Uses inline test runner, needs conversion to Jaribu**
- [ ] `route-builder-01-basic.cs`
- [ ] `test-terminal-context-01-basic.cs`

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block
2. Wrap types in namespace block
3. Add `[ModuleInitializer]` registration method
4. Remove `[ClearRunfileCache]` attribute
5. Remove `using TimeWarp.Nuru;` if present (already global)

### Example Migration (ansi-string-utils-01-basic.cs)

**Before:**
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

return await RunTests<AnsiStringUtilsTests>(clearCache: true);

[TestTag("Widgets")]
[ClearRunfileCache]
public class AnsiStringUtilsTests { ... }
```

**After:**
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.AnsiStringUtils
{

[TestTag("Widgets")]
public class AnsiStringUtilsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AnsiStringUtilsTests>();
  // ... tests ...
}

} // namespace
```

## Suggested Namespaces

| Category | Namespace |
|----------|-----------|
| ANSI Utils | `TimeWarp.Nuru.Tests.Core.AnsiStringUtils` |
| Help Provider | `TimeWarp.Nuru.Tests.Core.HelpProvider` |
| Hyperlink | `TimeWarp.Nuru.Tests.Core.Hyperlink` |
| Panel Widget | `TimeWarp.Nuru.Tests.Core.PanelWidget` |
| Rule Widget | `TimeWarp.Nuru.Tests.Core.RuleWidget` |
| Table Widget | `TimeWarp.Nuru.Tests.Core.TableWidget` |
| Invoker Registry | `TimeWarp.Nuru.Tests.Core.InvokerRegistry` |
| Message Type | `TimeWarp.Nuru.Tests.Core.MessageType` |
| Route Registry | `TimeWarp.Nuru.Tests.Core.RouteRegistry` |
| Route Builder | `TimeWarp.Nuru.Tests.Core.RouteBuilder` |
| Terminal Context | `TimeWarp.Nuru.Tests.Core.TerminalContext` |

## Special Considerations

### nuru-route-registry-01-basic.cs
This file uses an **inline test runner pattern** (not Jaribu). It needs to be fully converted to Jaribu TestRunner pattern, not just wrapped.

### AnsiStringUtils conflict
Since there's a class `TimeWarp.Nuru.AnsiStringUtils`, tests need to use fully-qualified name or alias to avoid ambiguity.

## Checklist

- [x] Analyze helper/utility files
- [x] Migrate ansi-string-utils-01-basic.cs (17 tests)
- [ ] Migrate remaining 22 files
- [ ] Update Directory.Build.props to use glob pattern
- [ ] Test standalone mode for each file
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Commit changes

## Notes

- Starting CI test count: 679 tests
- Current CI test count: 696 tests (+17 from ansi-string-utils-01)
- Lexer tests already use namespace `TimeWarp.Nuru.Tests.LexerTests`
- After all migrations, switch to recursive glob pattern
