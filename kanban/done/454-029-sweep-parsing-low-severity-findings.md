# Sweep Parsing Low Severity Findings

Parent: 454 (2026-07-06 full code review). Severity: LOW (batch).

## Description

Low-severity findings in `source/timewarp-nuru-parsing/`:

1. `parser/parser.segments.cs:68-74` — InvalidModifierCombinationError span is
   `nameToken.EndPosition - startPos`, excluding the offending `?`/`*`/`}`; the
   diagnostic underlines less than the construct (e.g. `{*name?}`).
2. ~~Dead code: `validation/validation-context.cs:16` — `OptionAliases` write-only~~
   DONE in 454-016 (commit 589c6d52): the dict and its population were removed.
3. Dead code: unused locals `firstLocation`/`secondLocation` in
   `ValidateDuplicateParameters` (`semantic-validator.cs:108-120`).
4. Dead code: `lexer/lexer.cs:273-276` — `Lexer.PeekNext()` never called.

## Checklist

- [x] Diagnostic span covers the offending modifier (through the `?`) (#1)
- [x] Dead code removed (unused firstLocation/secondLocation #3; Lexer.PeekNext #4; OptionAliases was already done in 454-016)
- [x] CI tests green (1386/1379/0)

## Resolution (2026-07-14)

- **#1** `parser.segments.cs` — `InvalidModifierCombinationError` span now runs to
  `Previous().EndPosition` (the just-matched `?`), so `{*name?}` underlines the offending
  modifier instead of stopping at the name. Left at the `?` (the `}` is not yet consumed at
  the validation point; extending further would require deferring the check, a behavior change).
- **#3** `semantic-validator.cs` — removed the write-only `firstLocation`/`secondLocation`
  locals (and the now-unused `first`) in `ValidateDuplicateParameters`.
- **#4** `lexer.cs` — removed the never-called `PeekNext()`.
- **#2** (OptionAliases) was already removed in 454-016 (commit 589c6d52).
