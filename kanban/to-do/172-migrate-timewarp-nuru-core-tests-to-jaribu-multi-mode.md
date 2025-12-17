# Migrate timewarp-nuru-core-tests to Jaribu multi-mode

## Description

Migrate the remaining test files in `tests/timewarp-nuru-core-tests/` to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner. The lexer tests are already migrated; this task covers the remaining 25 test files.

## Files to Migrate

### Already Migrated (lexer/ folder - 16 files)
- `lexer/*.cs` - Already in CI via glob pattern

### Test Files to Migrate (25 files)

#### ANSI String Utils (2 files)
- [ ] `ansi-string-utils-01-basic.cs`
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
- [ ] `nuru-route-registry-01-basic.cs`
- [ ] `route-builder-01-basic.cs`
- [ ] `test-terminal-context-01-basic.cs`

#### Helper/Utility (2 files)
- [ ] `compiled-route-test-helper.cs` - May just need namespace, check if it has tests
- [ ] `show-help-colors.cs` - Check if this is a test or utility

## Checklist

- [ ] For each test file:
  - Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block
  - Wrap types in namespace block (e.g., `TimeWarp.Nuru.Tests.Core.AnsiStringUtils`)
  - Add `[ModuleInitializer]` registration method
  - Remove `[ClearRunfileCache]` attribute if present
  - Remove `using TimeWarp.Nuru;` if present (already global)
  
- [ ] Update `tests/ci-tests/Directory.Build.props`:
  - Replace `<Compile Include="../timewarp-nuru-core-tests/lexer/*.cs" />` with
  - `<Compile Include="../timewarp-nuru-core-tests/**/*.cs" Exclude="...obj/**;...bin/**" />`

- [ ] Test standalone mode for each file
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Commit changes

## Notes

- Current CI test count: 679 tests
- Lexer tests already use namespace `TimeWarp.Nuru.Tests.LexerTests`
- After migration, use recursive glob pattern like REPL tests
- Helper files (like `compiled-route-test-helper.cs`) may just need namespace wrapping without test registration

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
