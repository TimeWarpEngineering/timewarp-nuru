# REPL completion remaining 470 findings

Parent: 470 (2026-09-04 full-repo review). Suggestions/nits: M18, M19, M37, M44.

## Description

M18: `Shift+Enter` → `HandleAddLineAsync` is registered only on the Default key-binding profile. On Emacs/Vi/VSCode the chord is swallowed (`repl-console-reader.cs:141-165`).

M19: bash dynamic completion `COMPREPLY=($(compgen -W "${suggestions[*]}" -- "$cur"))` word-splits spaced/glob candidates. pwsh/fish were hardened in 454-030; bash was not.

M37: zsh template still strips a trailing bare numeric line as an “exit code”; `DynamicCompletionHandler` never emits one.

M44: `{{APP_PATH}}` is substituted unescaped inside a PowerShell string. Practical risk near-zero.

Do not duplicate 454-019 (wrapped-line redraw). HandleCharacter crash is **470-002**. CRLF paste is **470-003**. History mode is **470-011**.

## Requirements

- Bind Shift+Enter add-line on Emacs/Vi/VSCode (or document a substitute).
- Quoting-safe bash COMPREPLY assignment.
- Remove zsh numeric exit-code strip; keep :directive handling.
- Escape APP_PATH for pwsh (optional if documented as wontfix with rationale).

## Checklist

- [ ] M18 Shift+Enter on all profiles
- [ ] M19 bash quoting
- [ ] M37 zsh dead strip
- [ ] M44 pwsh APP_PATH (fix or wontfix)

## Notes

Evidence: parent 470 `review/round-1/merged.md` M18, M19, M37, M44.
