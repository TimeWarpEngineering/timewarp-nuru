# Review framework — task 069

**Date:** 2026-09-30
**Host task:** kanban/to-do/069-global-user-key-binding-profiles/
**Diff scope:** branch `task/069-global-user-key-binding-profiles` vs `origin/master` (`edef26ce..5b5b7220`)
**Plan / brief:** Load a global REPL key binding profile from JSON when `KeyBindingProfile` is unset and `KeyBindingProfileName` is still `"Default"`. Search `NURU_KEYBINDINGS`, then `./.nuru/keybindings.json`, then `~/.nuru/keybindings.json`. Map existing handlers through an action registry, parse key strings, and document the format plus samples.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
