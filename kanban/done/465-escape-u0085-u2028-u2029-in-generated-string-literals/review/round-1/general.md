# Round 1 — general
**Date:** 2026-08-14
**Scope reviewed:** emitter-string-utils + help-09 tests + free-text emitter audit

## Summary

`EscapeForStringLiteral` correctly appends U+0085/U+2028/U+2029 after the existing escapes, with replacements that are the six-character sequences `\uXXXX` (not re-inserted raw chars), and backslash-first order preserved so the new sequences are not double-escaped. Free-text embeds in help, route-help, capabilities, completion/repl descriptions, version, telemetry, behavior, and route-matcher already call the shared helper; residual unescaped embeds are identifiers/forms (enum members, option long/short forms, param names/type constraints, config section keys) as planned out of scope. Regression tests exercise compile of fluent + Endpoint DSL descriptions/examples containing the three characters and assert raw-char presence in `--help` and deserialized `--capabilities` properties.

## Issues

<!-- none -->
