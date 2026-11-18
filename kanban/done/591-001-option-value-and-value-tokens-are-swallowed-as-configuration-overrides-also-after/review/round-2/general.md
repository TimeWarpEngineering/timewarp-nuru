# Round 2 — general
**Date:** 2026-11-19
**Scope reviewed:** Delta since round 1 base `16de6357` (commit `587436ba`). Emitters (`configuration-emitter.cs`, `interceptor-emitter.method-body.cs`, `json-args-emitter.cs`), the new routing-42 case, and the System.CommandLine migration guide and skill. Checked against round 1 M1 and M2, `## Goal (implementer restatement)`, and the original task.

## Summary

The fix pass does what M1 and M2 asked. `AddCommandLine` now receives only the tokens `RouteTokenOrConfig` drops, and that list is filled before configuration is built. A declared `--format:json` stays on the route, so it cannot take the next argument as a configuration value. The new routing-42 test covers `report --format:json --Database:Host=prod` with `AddConfiguration()`. The guide and skill now say every slash `=` / `:` form is configuration, and that `-o=value` binds a declared short value option. The matcher has a `-{ShortForm}=` branch and no colon branch, so the guide's extra sentence that `-o:value` does not bind matches the code.

The recorded fix-pass build, audit, and targeted runs cover the emitter and test edits. No new defect on this delta. No misread: the restatement still matches the original task.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/configuration-emitter.cs:106
- Description: Round 1 M1. Re-checked the post-fix diff. `JsonArgsEmitter.EmitPrepare` appends a token to `__configArgList` only when `RouteTokenOrConfig` returns null and the app has configuration (`json-args-emitter.cs` around the `__ri` loop). `InterceptorEmitter.EmitMethodBody` emits `ConfigurationEmitter` after that loop, and `AddCommandLine` spreads `__configArgList`. A declared `--name:value` is rewritten to `--name=value` and kept as a route token, so it is not in the list. Tokens at or after the first `--` are passed through and are not in the list. `Should_keep_override_after_declared_colon_option_with_configuration` asserts `format:json`, `Database:Host` = `prod`, and no `format:json` key. The fix-pass record says that file is 9/9 and the configuration and json-args runfiles that share this path passed. Nothing after the early `--json-args` returns uses the configuration local.
- Suggestion: None. The M1 change holds.
- Status: fixed

### Issue 2 — Severity: suggestion
- File: documentation/user/guides/migrating-from-system-commandline.md:235
- Description: Round 1 M2. The guide now says every `/name=value` and `/name:value` is configuration even when the name is declared, that configuration reads only removed overrides, that `-o=value` binds a declared short value option, and that `-o:value` does not bind. The skill bullet says the same about slash forms and `-o=value`. `RouteTokenOrConfig` returns null for every slash token whose second character is a letter and that contains `=` or `:` past index 1. `route-matcher-emitter.option-parsing.cs` binds `-{ShortForm}=` and has no `-{ShortForm}:` branch. The task 497 `-v=true` crash is not folded into either sentence.
- Suggestion: None. The M2 wording matches the filter and the matcher.
- Status: fixed
