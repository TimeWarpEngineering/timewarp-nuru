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

- [ ] Diagnostic span covers full modifier construct
- [ ] Dead code removed (OptionAliases, unused locals, PeekNext)
- [ ] CI tests green
