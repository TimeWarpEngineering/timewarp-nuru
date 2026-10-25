# Round 1 — merged findings
**Date:** 2026-10-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: documentation/user/features/logging.md:178
- Description: "Nuru's Own Log Messages" says the parser, lexer, and compiler messages are written in the source generator at build time. The generator parses with `PatternParser.TryParse` and `new Parser()`, and `TryParse` constructs `Parser` and `Compiler` with no logger. Those log calls are compiled out of the analyzer (`#if !ANALYZER_BUILD`). A build does not write them. They are written only when a non-analyzer build calls `PatternParser.Parse(pattern, loggerFactory)`. The same sentence is in `documentation/developer/guides/logging.md:178`. The sample under the sentence is that call.
- Suggestion: Say the generator parses with no logger, so neither the build nor a running app writes these messages, and keep the `PatternParser.Parse(pattern, loggerFactory)` sample as the way to see them. Update both logging.md copies.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
