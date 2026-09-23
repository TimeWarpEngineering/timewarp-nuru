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

- [x] M18 Shift+Enter on all profiles
- [x] M19 bash quoting
- [x] M37 zsh dead strip
- [x] M44 pwsh APP_PATH (fix or wontfix)

## Notes

Evidence: parent 470 `review/round-1/merged.md` M18, M19, M37, M44.

## Results

All four 470 leftovers closed with product fixes (no wontfix).

**M18 — Shift+Enter on all profiles.** Bound `(ConsoleKey.Enter, ConsoleModifiers.Shift)` → `HandleAddLineAsync` on Emacs, Vi, and VSCode (Default already had it). Profile remarks document the chord.

**M19 — bash quoting.** Replaced `COMPREPLY=($(compgen -W "${suggestions[*]}" -- "$cur"))` with a quoted prefix loop (`COMPREPLY+=("$s")`) so spaced/glob candidates are not IFS-split or glob-expanded. Empty suggestions + file-comp allowed still calls `_filedir`.

**M37 — zsh dead strip.** Removed the trailing `^[0-9]+$` exit-code strip; kept `:directive` handling only (same rationale as 454-030 pwsh/fish).

**M44 — pwsh APP_PATH.** Template now uses a single-quoted `$psi.FileName = '…'`; generator doubles embedded `'` via `EscapePowerShellSingleQuoted` before substitution.

**Files**

- `source/timewarp-nuru/repl/key-bindings/{emacs,vi,vscode}-key-binding-profile.cs`
- `source/timewarp-nuru/completion/completion/templates/bash-completion-dynamic.sh`
- `source/timewarp-nuru/completion/completion/templates/zsh-completion-dynamic.zsh`
- `source/timewarp-nuru/completion/completion/templates/pwsh-completion-dynamic.ps1`
- `source/timewarp-nuru/completion/completion/dynamic-completion-script-generator.cs`
- `tests/timewarp-nuru-tests/repl/repl-23-key-binding-profiles.cs` (+4 Shift+Enter profile cases)
- `tests/timewarp-nuru-tests/completion/completion-20-dynamic-script-gen.cs` (+3 template regressions)
- `tests/timewarp-nuru-tests/repl/repl-41-lowsev-sweep.cs` (zsh numeric-strip assertion)

**Tests** — `completion-20`: **15/15 passed**. `repl-23`: **11 passed / 4 skipped** (pre-existing help skips; 4 new Shift+Enter cases green). `repl-32`: **9/9**. `repl-41`: **5/5**.

**Review disposition:** `clean` (effort 1, roster `general`, round 1, 0 open / 0 wontfix). Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`. Re-verified gates during review: completion-20 15/15, repl-23 11 passed / 4 skipped, repl-32 9/9, repl-41 5/5.

### How to validate

**Smoke**

```bash
dotnet run tests/timewarp-nuru-tests/completion/completion-20-dynamic-script-gen.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-23-key-binding-profiles.cs
```

**Expect**

- `completion-20` grand total **15 passed / 0 failed**. New cases: `Bash_template_avoids_unquoted_compgen_wordlist` (no `compgen -W "${suggestions[*]}"` / no `COMPREPLY=($(compgen`); `Zsh_template_does_not_strip_numeric_exit_code_line` (no `^[0-9]+$`); `Pwsh_app_path_is_single_quoted_and_apostrophes_escaped` (`$psi.FileName = '…'`, `O'Brien` → `O''Brien`).
- `repl-23` — `Emacs_profile_should_support_shift_enter_add_line`, `Vi_profile_should_support_shift_enter_add_line`, `VSCode_profile_should_support_shift_enter_add_line`, `Default_profile_should_support_shift_enter_add_line` each print `SHIFT-ENTER-<Profile>` (0 failed among non-skipped).

**Automated gate**

```bash
dotnet run tests/timewarp-nuru-tests/completion/completion-20-dynamic-script-gen.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-23-key-binding-profiles.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-32-multiline-editing.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-41-lowsev-sweep.cs
```

Expect each file's jaribu summary to report all passed / 0 failed (repl-23 may still skip the four pre-existing help-format tests).
