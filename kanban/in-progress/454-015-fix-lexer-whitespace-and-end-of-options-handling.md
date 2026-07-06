# Fix Lexer Whitespace And End Of Options Handling

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M12, M14).

## Description

Two lexer whitespace defects in `source/timewarp-nuru-parsing/lexer/lexer.cs`:

- **M12** (line ~130): end-of-options `--` is recognized only when followed by end or an
  ASCII space (`IsAtEnd() || Peek() == ' '`). Verified divergence: `-- x` lexes as
  EndOfOptions + literal (and correctly errors "must be followed by a catch-all"), but
  `--\tx` lexes as long option `--x` and parses successfully. Tab/newline/CR after `--`
  silently change parse semantics. Use a whitespace class, not `' '`.
- **M14** (lines ~94-99): whitespace is fully discarded with no boundary token, so
  `greet {a}{b}` produces a token stream identical to `greet {a} {b}` — both verified to
  parse successfully. Adjacent parameters with no separator are almost certainly a typo
  and should be a diagnostic (or at minimum a documented, deliberate behavior).

## Checklist

- [ ] `--` detection uses whitespace class (tab/CR/LF)
- [ ] Adjacent segments without whitespace produce a diagnostic (or documented decision)
- [ ] Lexer/parser tests for `--\tx` and `{a}{b}`
- [ ] `ganda runfile cache --clear` + run CI tests

## Notes

Parser robustness was fuzz-verified (200k malformed patterns, no throws) — preserve that:
new paths must produce diagnostics, never exceptions.
