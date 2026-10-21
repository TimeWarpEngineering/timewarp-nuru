# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** Delta `7b872eb..72cf0f08` (M1 fix). Matcher guard deferral in `route-matcher-emitter.cs`, the six new routing-37 tests, and the changelog Fixed entries. Re-checked M1 against that diff and the goal. The implementer restatement matches the original checklist.

## Summary

The fix stops option guards from skipping during parsing. On a route with a leading literal, a close typo of a required value option or an unbound discriminator flag is recorded by the prefix check and suggested. `routing-37` is 22 of 22 passed, and the alias guard test returns instead of looping. The same typo on a route with no leading literal still becomes `Unknown command`: the new guard skip runs before the only recording site for that shape.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:193
- Description: `EmitOptionGuardSkip` runs immediately after `EmitUnknownOptionPrefixCheck`. That prefix check returns without looking at the token when `GetLiteralPrefix` is empty (`route-help-emitter.cs` stops at the first parameter or option). The empty-prefix recording site, `EmitUnknownOptionMatchCheck`, is later (line 270), after the guard skip. A missing required option or unbound discriminator flag therefore jumps to `route_skip_N` before the token is recorded. The handler does not run, but the no-match fallback writes `Unknown command` and does not suggest the declared form. Probed: `Map("--check-updates")` with `--check-update` exits 1 with `Unknown command. Use --help for usage.` `Map("{file} --output {output}")` with `readme --outpt out` does the same. The same typo is suggested when the option is optional (`{file} --output? {output?}`) or the route has a leading literal (`tool {file} --output {output}`, `commit --amend`, `kanban create {title} --folder {folder}`). `EmitAliasMatch` has the same order (guard at line 365, empty-alias match check at line 417). A non-empty alias still goes through the prefix check, which is why `t515-mk` passes.
- Suggestion: For an empty literal prefix, record the unknown option when the rest of the route matches (positional length and literals), then apply the option-guard skip, and only then emit option conversions. A missing value must still skip rather than raise a conversion error, and an extra positional must stay `Unknown command`. Cover `--check-updates` with `--check-update`, and `{file} --output {output}` with `readme --outpt out`. Both should exit 1, name the token, suggest the declared form, and not run the handler.
- Status: open
