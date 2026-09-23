# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** local uncommitted diff for 470-011 (M17 file modes + M32 ignore patterns)

## Summary

`ReplHistory` now writes history via `WriteOwnerOnlyLines` (Unix `0600` create mode plus post-write `SetUnixFileMode` to harden truncate-in-place replaces) and creates missing parent segments with `EnsureOwnerOnlyDirectory` (segment walk so `~/.nuru` and `history` both get `0700`, matching the known leaf-only `CreateDirectory(path, mode)` pitfall). Default ignore patterns cover the M32 shapes; docs mark ignores as best-effort and document Windows profile ACLs. `repl-03b` asserts both create and harden-on-replace; `repl-03b` (15) and `repl-04` (8) all passed on this host. Risk is low and localized; no correctness or contract gaps found against the task requirements.

## Issues

<!-- none -->
