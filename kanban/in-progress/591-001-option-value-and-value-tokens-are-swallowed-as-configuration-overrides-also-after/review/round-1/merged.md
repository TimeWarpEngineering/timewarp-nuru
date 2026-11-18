# Round 1 — merged findings
**Date:** 2026-11-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/configuration-emitter.cs:105
- Description: With `AddConfiguration()`, arguments before `--` still go to `AddCommandLine`, including a declared `--name:value`. The route list rewrites `:` to `=` (`json-args-emitter.cs`); the configuration array does not. `CommandLineConfigurationProvider.Load` (Microsoft.Extensions.Configuration.CommandLine 11.0.0) treats a `--` token with no `=` as a key and takes the next argument as its value. `report --format:json --Database:Host=prod` stores `format:json` = `--Database:Host=prod` and does not set `Database:Host`, while the route binds `format` to `json` and exits 0. `--name=value` does not consume the next token. The restatement says the two forms bind the same way. The `__IsDeclaredLongOption` comment says these forms are not configuration. No routing-42 test enables configuration for the colon form. Targeted check: `dnx ilspycmd --disable-updatecheck -t Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationProvider` on the 11.0.0 net11.0 assembly, to confirm the no-`=` parse. No build or test run.
- Suggestion: Exclude declared `--name=value` and `--name:value` tokens from the `AddCommandLine` arguments, or normalize a declared `--name:value` to `--name=value` before that call. Cover `report --format:json --Database:Host=prod` with `AddConfiguration()` in routing-42: option value `json`, configuration `Database:Host` = `prod`.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: documentation/user/guides/migrating-from-system-commandline.md:227
- Description: The new paragraph qualifies `/name=value` and `/name:value` with "whose name is not a long option", and says short options with `=` (`-o=x`) are not bound. The filter drops every such slash token regardless of declared names, and the matcher binds `-o=value` for a short value option (`route-matcher-emitter.option-parsing.cs`).
- Suggestion: State that slash `=` / `:` forms stay configuration overrides even when the name is declared, and that `-o=value` binds a declared short value option. Do not fold the task 497 `AddCommandLine` crash on `-v=true` into this sentence.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. No overlaps.
