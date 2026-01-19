# Jaribu Tests NOT Included in CI

**Report Date:** 2026-01-17

## Executive Summary

This report identifies all Jaribu test files in the `tests/` directory that are **not included** in the CI test run configuration (`tests/ci-tests/run-ci-tests.cs` via `Directory.Build.props`). The analysis reveals that approximately **120+ test files** across multiple test projects are excluded from CI, spanning entire test directories (analyzers, MCP, completion, factory) and numerous individual test files that are disabled, use old APIs, or serve as placeholders.

---

## Scope

**Analyzed Directories:**
- `tests/timewarp-nuru-tests/` - Core library tests
- `tests/timewarp-nuru-analyzers-tests/` - Source generator analyzer tests
- `tests/timewarp-nuru-mcp-tests/` - MCP server tests
- `tests/timewarp-nuru-repl-tests-reference-only/` - Reference-only REPL tests

**Reference Configuration:**
- `tests/ci-tests/Directory.Build.props` - Defines which tests are compiled into CI

---

## Tests INCLUDED in CI

The following test files are explicitly included in the CI build:

### Core Tests (87 files total)

| Category | Pattern | Files Count |
|----------|---------|-------------|
| Lexer | `lexer/*.cs` | 16 |
| Generator | `generator/generator-01,10,11,12,13,14,04.cs` | 7 |
| Message Type | `message-type/*.cs` | 1 |
| Parser | `parser/*.cs` | 15 |
| Routing | `routing/*.cs` (excludes dsl-example.cs) | 22 |
| Configuration | `configuration/*.cs` | 2 |
| Options | `options/*.cs` | 2 |
| Session | `session/*.cs` | 1 |
| Help | `help/*.cs` | 1 |
| Capabilities | `capabilities/*.cs` | 2 |
| Type Conversion | `type-conversion/*.cs` | 1 |
| REPL | `repl/*.cs` (37 specific files) | 37 |

### REPL Tests Included (37 files)

```
repl-01-session-lifecycle.cs
repl-02-command-parsing.cs
repl-03-history-management.cs
repl-03b-history-security.cs
repl-04-history-persistence.cs
repl-05-console-input.cs
repl-06-tab-completion-basic.cs
repl-07-tab-completion-advanced.cs
repl-08-syntax-highlighting.cs
repl-09-builtin-commands.cs
repl-10-error-handling.cs
repl-11-display-formatting.cs
repl-12-configuration.cs
repl-13-nuruapp-integration.cs
repl-14-performance.cs
repl-15-edge-cases.cs
repl-17-sample-validation.cs
repl-18-psreadline-keybindings.cs
repl-19-tab-cycling-bug.cs
repl-20-double-tab-bug.cs
repl-21-escape-clears-line.cs
repl-22-prompt-display-no-arrow-history.cs
repl-23-key-binding-profiles.cs
repl-24-custom-key-bindings.cs
repl-25-interactive-history-search.cs
repl-26-kill-ring.cs
repl-27-undo-redo.cs
repl-28-text-selection.cs
repl-29-word-operations.cs
repl-30-basic-editing-enhancement.cs
repl-31-multiline-buffer.cs
repl-33-yank-arguments.cs
command-line-parser/parser-01-basic-parsing.cs
command-line-parser/parser-02-quoted-strings.cs
```

---

## Tests NOT INCLUDED in CI

### 1. timewarp-nuru-analyzers-tests/ (16 files)

**Entire test project excluded from CI.**

| File | Category |
|------|----------|
| `auto/attributed-route-generator-01-basic.cs` | Auto-generated |
| `auto/attributed-route-generator-03-matching.cs` | Auto-generated |
| `auto/behavior-filtering-01-implements-extraction.cs` | Auto-generated |
| `auto/delegate-signature-01-models.cs` | Auto-generated |
| `auto/generator-15-ilogger-injection.cs` | Auto-generated |
| `auto/handler-analyzer-01-diagnostics.cs` | Auto-generated |
| `auto/nuru-invoker-generator-01-basic.cs` | Auto-generated |
| `auto/overlap-analyzer-01-basic.cs` | Auto-generated |
| `auto/pattern-errors-01-basic.cs` | Auto-generated |
| `interpreter/dsl-interpreter-fragmented-test.cs` | Interpreter |
| `interpreter/dsl-interpreter-group-test.cs` | Interpreter |
| `interpreter/dsl-interpreter-methods-test.cs` | Interpreter |
| `interpreter/dsl-interpreter-test.cs` | Interpreter |
| `manual/should-pass-map-non-generic.cs` | Manual |
| `manual/test-analyzer-patterns.cs` | Manual |

**Reason for exclusion:** Source generator analyzer tests have a different build requirement and are not compiled into the multi-mode JARIBU test runner.

---

### 2. timewarp-nuru-mcp-tests/ (6 files)

**Entire test project excluded from CI.**

| File | Purpose |
|------|---------|
| `mcp-01-example-retrieval.cs` | MCP example retrieval |
| `mcp-02-syntax-documentation.cs` | MCP syntax documentation |
| `mcp-03-route-validation.cs` | MCP route validation |
| `mcp-04-handler-generation.cs` | MCP handler generation |
| `mcp-05-error-documentation.cs` | MCP error documentation |
| `mcp-06-version-info.cs` | MCP version info |

**Reason for exclusion:** MCP tests require a running MCP server and are intended for separate test execution via `tests/scripts/run-mcp-tests.cs` or `tests/scripts/test-mcp-server.cs`.

---

### 3. completion/ (40+ files)

**Entire completion test directory excluded from CI.**

#### static/ (14 files)
```
completion-01-command-extraction.cs
completion-02-option-extraction.cs
completion-03-parameter-type-detection.cs
completion-04-cursor-context.cs
completion-05-bash-script-generation.cs
completion-06-zsh-script-generation.cs
completion-07-powershell-script-generation.cs
completion-08-fish-script-generation.cs
completion-09-integration-enablecompletion.cs
completion-10-route-analysis.cs
completion-11-enum-completion.cs
completion-12-edge-cases.cs
completion-13-template-loading.cs
```

#### dynamic/ (13 files)
```
completion-14-dynamic-handler.cs
completion-15-completion-registry.cs
completion-16-default-source.cs
completion-17-enum-source.cs
completion-18-parameter-detection.cs
completion-19-endpoint-matching.cs
completion-20-dynamic-script-gen.cs
completion-21-integration-enabledynamic.cs
completion-22-callback-protocol.cs
completion-23-custom-sources.cs
completion-24-context-aware.cs
completion-25-output-format.cs
completion-26-enum-partial-filtering.cs
```

#### engine/ (3 files)
```
engine-01-input-tokenizer.cs
engine-02-route-match-engine.cs
engine-03-candidate-generator.cs
```

**Reason for exclusion:** Completion tests use old APIs (HelpProvider.GetHelpText, CreateSlimBuilder) that are incompatible with the new source generator architecture.

---

### 4. timewarp-nuru-repl-tests-reference-only/ (57 files)

**Entire reference-only test directory excluded from CI.**

This directory contains duplicate/legacy REPL tests that are not maintained or run.

| File | Notes |
|------|-------|
| `repl-01-session-lifecycle.cs` | Duplicate of included test |
| `repl-02-command-parsing.cs` | Duplicate of included test |
| `repl-03-history-management.cs` | Duplicate of included test |
| `repl-03b-history-security.cs` | Duplicate of included test |
| `repl-04-history-persistence.cs` | Duplicate of included test |
| `repl-05-console-input.cs` | Duplicate of included test |
| `repl-06-tab-completion-basic.cs` | Duplicate of included test |
| `repl-07-tab-completion-advanced.cs` | Duplicate of included test |
| `repl-08-syntax-highlighting.cs` | Duplicate of included test |
| `repl-09-builtin-commands.cs` | Duplicate of included test |
| `repl-10-error-handling.cs` | Duplicate of included test |
| `repl-11-display-formatting.cs` | Duplicate of included test |
| `repl-12-configuration.cs` | Duplicate of included test |
| `repl-13-nuruapp-integration.cs` | Duplicate of included test |
| `repl-14-performance.cs` | Duplicate of included test |
| `repl-15-edge-cases.cs` | Duplicate of included test |
| `repl-16-enum-completion.cs` | Duplicate of skipped test |
| `repl-17-sample-validation.cs` | Duplicate of included test |
| `repl-18-psreadline-keybindings.cs` | Duplicate of included test |
| `repl-19-tab-cycling-bug.cs` | Duplicate of included test |
| `repl-20-double-tab-bug.cs` | Duplicate of included test |
| `repl-21-escape-clears-line.cs` | Duplicate of included test |
| `repl-22-prompt-display-no-arrow-history.cs` | Duplicate of included test |
| `repl-23-key-binding-profiles.cs` | Duplicate of included test |
| `repl-24-custom-key-bindings.cs` | Duplicate of included test |
| `repl-25-interactive-history-search.cs` | Duplicate of included test |
| `repl-26-kill-ring.cs` | Duplicate of included test |
| `repl-27-undo-redo.cs` | Duplicate of included test |
| `repl-28-text-selection.cs` | Duplicate of included test |
| `repl-29-word-operations.cs` | Duplicate of included test |
| `repl-30-basic-editing-enhancement.cs` | Duplicate of included test |
| `repl-31-multiline-buffer.cs` | Duplicate of included test |
| `repl-32-multiline-editing.cs` | Placeholder |
| `repl-33-yank-arguments.cs` | Duplicate of included test |
| `repl-34-interactive-route-alias.cs` | Placeholder |
| `repl-35-interactive-route-execution.cs` | Placeholder |
| `command-line-parser/parser-01-basic-parsing.cs` | - |
| `command-line-parser/parser-02-quoted-strings.cs` | - |
| `tab-completion/repl-20-tab-basic-commands.cs` | - |
| `tab-completion/repl-21-tab-subcommands.cs` | - |
| `tab-completion/repl-22-tab-enums.cs` | - |
| `tab-completion/repl-23-tab-options.cs` | - |
| `tab-completion/repl-24-tab-cycling.cs` | - |
| `tab-completion/repl-25-tab-state-management.cs` | - |
| `tab-completion/repl-26-tab-edge-cases.cs` | - |
| `tab-completion/repl-27-tab-help-option.cs` | - |

**Reason for exclusion:** Reference-only directory for legacy documentation purposes.

---

### 5. Individual Excluded Files in timewarp-nuru-tests/

#### generator/temp-iconfig-test.cs
```csharp
// Temporary test file for IOptions<T> testing
// Not included in CI
```

#### temp-test-chained.cs (at tests/ root)
```csharp
// Test for chained pattern (bug #295)
// Located at tests/temp-test-chained.cs
// Not compiled into CI
```

#### REPL Tests Marked as Placeholders (7 files)
| File | Status |
|------|--------|
| `repl/repl-16-enum-completion.cs` | Skipped - requires enum source generator support |
| `repl/repl-32-multiline-editing.cs` | Placeholder file |
| `repl/repl-34-interactive-route-alias.cs` | Placeholder file |
| `repl/repl-35-interactive-route-execution.cs` | Placeholder file |
| `repl/repl-36-run-repl-async-inline.cs` | Placeholder file |
| `repl/repl-37-quoted-strings.cs` | Placeholder file |

#### Factory Tests - Entirely Excluded
```
factory/*.cs
```
**Reason:** Factory tests excluded from multi-mode (comment in Directory.Build.props).

#### Help Provider Tests - Disabled (5 files)
```
help-provider-01-option-detection.cs
help-provider-02-filtering.cs
help-provider-03-session-context.cs
help-provider-04-default-route.cs
help-provider-05-color-output.cs
```
**Reason:** Use old APIs (HelpProvider.GetHelpText, CreateSlimBuilder, SessionContext, InvokerRegistry).

#### Other Disabled Tests
```
invoker-registry-01-basic.cs
message-type-02-help-output.cs
route-builder-01-basic.cs
test-terminal-context-01-basic.cs
compiled-route-test-helper.cs
completion/static/*.cs
completion/dynamic/*.cs
completion/engine/*.cs
```

---

## Summary Statistics

| Category | Count |
|----------|-------|
| Analyzers tests excluded | 16 |
| MCP tests excluded | 6 |
| Completion tests excluded | 40+ |
| Reference-only REPL tests excluded | 57 |
| Individual disabled/skipped tests | 15+ |
| **Total tests NOT in CI** | **~134+** |

---

## Recommendations

### High Priority
1. **Factory tests** - Investigate and migrate factory tests to be compatible with multi-mode JARIBU
2. **Help provider tests** - Modernize or remove legacy API tests
3. **Completion tests** - Either port to new architecture or formally deprecate

### Medium Priority
4. **Analyzers tests** - Consider adding separate CI job for source generator tests
5. **MCP tests** - Already have separate runfiles, document this is intentional
6. **Reference-only REPL tests** - Clean up duplicates or archive

### Low Priority
7. **Placeholder files** - Either implement or remove: repl-32, repl-34, repl-35, repl-36, repl-37
8. **temp-iconfig-test.cs and temp-test-chained.cs** - Either promote to real tests or delete

---

## References

- CI Configuration: `tests/ci-tests/Directory.Build.props`
- CI Entry Point: `tests/ci-tests/run-ci-tests.cs`
- Test Overview: `tests/timewarp-nuru-tests/test-plan-overview.md`
