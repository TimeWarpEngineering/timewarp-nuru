# Round 1 — general
**Date:** 2026-09-30
**Scope reviewed:** branch `task/069-global-user-key-binding-profiles` vs `origin/master` — config loader, combo parser, action registry, REPL startup, samples, and user docs.

## Summary

`ReplConsoleReader` calls `KeyBindingConfigLoader.ResolveStartupProfile`. An `IKeyBindingProfile` instance wins. Any `KeyBindingProfileName` other than ordinal `"Default"` goes to `KeyBindingProfileFactory` and does not read files. Otherwise the first existing file wins: `NURU_KEYBINDINGS`, then `./.nuru/keybindings.json` under the current directory, then `~/.nuru/keybindings.json`. A missing file is skipped. An invalid file throws `KeyBindingConfigException` and does not fall through. A directory in `NURU_KEYBINDINGS` fails immediately. The loaded path is logged at Debug.

The registry lists 67 canonical names for the handlers that exist, including `DigitArgument:0` through `DigitArgument:9` with a per-digit capture. `TabComplete:reverse` is an alias and is not listed twice. `CharacterWithOverwrite` is absent. Config application order is removals, then overrides, then additions, then `exitKeys`. Additions and overrides both call `Override`, so an addition replaces the same key. `MarkExitKey` does not change the action. `Remove` clears exit-key status. Unknown actions, unknown keys, and unknown base profiles include a line number. Unknown JSON properties are rejected. Comments and trailing commas are accepted. The three samples match the documented names. `repl-key-bindings.md` no longer treats `NURU_KEYBINDINGS` as a profile name. No bugs, suggestions, or nits.

## Issues

<!-- none -->
