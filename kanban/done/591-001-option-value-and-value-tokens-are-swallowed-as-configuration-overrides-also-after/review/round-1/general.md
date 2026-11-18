# Round 1 — general
**Date:** 2026-11-19
**Scope reviewed:** branch `task/591-001-option-value-and-value-tokens-are-swallowed-as` vs `master`. Emitters (`json-args-emitter.cs`, `configuration-emitter.cs`, `interceptor-emitter.method-body.cs`), routing-24 / routing-41 / routing-42, and the System.CommandLine migration guide and skill. Checked against `## Goal (implementer restatement)` and the original task description.

## Summary

The change does what the task asks for on the route side. Tokens after the first `--` stay on the route and are cut out of `AddCommandLine`. A declared long option's `--name=value` reaches the matcher, `--name:value` is rewritten to `=` so the matcher binds it, and `--flag=value` fails as an unknown option. The skipped enum test is running again, and the guide and skill drop the old "always configuration" difference. The recorded build, audit, and targeted test runs cover that route behavior.

The hole is configuration. `AddCommandLine` still receives the original `--name:value` token. That form has no `=`, so the configuration provider treats the next argument as its value and a following `--Section:Key=value` override disappears. The new tests never turn configuration on for the colon form. The guide also misstates short-option `=` binding and slash forms.

No misread. The restatement matches the original task: both `--name=value` and `--name:value` are in the defect, tokens after `--` stay, and a flag's `=value` form takes the "at least errors" branch.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/configuration-emitter.cs:105
- Description: With `AddConfiguration()`, the generated builder passes every argument before `--` to `AddCommandLine`, including a declared option written `--name:value`. `RouteTokenOrConfig` rewrites that token to `--name=value` only in the route-argument list (`json-args-emitter.cs` around the `arg[sepIdx] == ':'` return). The configuration array keeps the original token. `CommandLineConfigurationProvider.Load` in Microsoft.Extensions.Configuration.CommandLine 11.0.0, when a `--` token contains no `=`, uses the whole remainder as the key and takes the next argument as the value. `report --format:json --Database:Host=prod` therefore stores key `format:json` with value `--Database:Host=prod` and never sets `Database:Host`. The route still binds `format` to `json` and drops the override as an undeclared config-shaped token, so the process can exit 0 with the override gone. `--format=json` does not do this, because that token contains `=`. The restatement and the migration guide say the two forms bind the same way. The comment on `__IsDeclaredLongOption` (`json-args-emitter.cs` near the predicate) says these forms are not configuration; the builder still receives them. routing-42's colon test does not call `AddConfiguration()`. configuration-02 covers `--Section:Key=value`, which contains `=` and is not a declared option name. Checked with `dnx ilspycmd --disable-updatecheck -t Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationProvider` on the 11.0.0 net11.0 assembly, to confirm how a `--` token with no `=` is parsed. That was the one targeted check. No build or test run.
- Suggestion: Keep a declared long option's `--name=value` and `--name:value` tokens out of the `AddCommandLine` arguments, or normalize a declared `--name:value` to `--name=value` before that call so it cannot consume the next argument. Add a routing-42 case with `AddConfiguration()`: `report --format:json --Database:Host=prod` should bind `format` to `json` and leave `Database:Host` as `prod`.
- Status: open

### Issue 2 — Severity: suggestion
- File: documentation/user/guides/migrating-from-system-commandline.md:227
- Description: The new paragraph says a token shaped like `/name=value` or `/name:value` is a configuration override when its name is not a declared long option, and that short options with `=` (`-o=x`) are not bound. `RouteTokenOrConfig` drops every `/` token whose second character is a letter and that contains `=` or `:` past index 1, whether or not the name is declared. The matcher has no `/name=` form. The same matcher does bind a declared short value option written `-o=value` (`route-matcher-emitter.option-parsing.cs`, the `-{ShortForm}=` branch). The skill text is narrower and does not make these two claims.
- Suggestion: Say that `/name=value` and `/name:value` stay configuration overrides even when the name matches a long option, and that `-o=value` binds a declared short value option. Leave the existing AddConfiguration `FormatException` on `-v=true` (task 497, skipped in routing-41) out of this sentence.
- Status: open
