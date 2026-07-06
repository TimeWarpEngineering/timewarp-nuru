# Resolve Multi Char Single Dash Option Support

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Description

The lexer and parser disagree about multi-char single-dash options (`-bl`, `-verbosity`):

- `source/timewarp-nuru-parsing/lexer/lexer.cs:141-144` documents and tokenizes them,
  with a dedicated test (lexer-05-multi-char-short-options).
- `source/timewarp-nuru-parsing/parser/parser.segments.cs:175-185` — `ParseOptionForms`
  unconditionally emits InvalidOptionFormatError when `shortForm.Length > 1`.

Verified: `PatternParser.Parse("build -bl")` fails with "Invalid option format '-bl' -
options must start with '--' or '-'" — a self-contradictory message (`-bl` DOES start
with `-`; the real rule being enforced is "single-dash options must be one char").

The feature is half-implemented. MSBuild-style tools genuinely use `-bl`, `-verbosity`.

## Requirements

- DECIDE: either support multi-char single-dash options end-to-end (parser, validator,
  compiler, runtime matcher, completion), or reject them consistently (remove lexer
  support + test, fix the misleading error message).
- If rejecting, error message must state the actual rule.

## Checklist

- [ ] Decision recorded here (support vs reject)
- [ ] Parser/lexer aligned with the decision
- [ ] Error message accurate
- [ ] Tests updated (lexer-05 and parser tests agree)
- [ ] `ganda runfile cache --clear` + run CI tests
