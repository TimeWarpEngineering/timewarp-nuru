# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commits `314951fc` and `4605c20a` (S-3 executable path on index rebuild)

## Summary

`clis.cli_path` is nullable. New databases create it with the table. An index that predates the column gets `ALTER TABLE clis ADD COLUMN cli_path TEXT` from `EnsureCliPathColumnAsync`, and those rows stay null. `index rebuild --cli` stores `Path.GetFullPath` when the argument is an existing file, otherwise `PathResolver.ResolveExecutable`, which returns a full path for a PATH name and null when the name is missing. `index rebuild --all` runs `CliPath` and falls back to the capabilities name only when the path is null. `search --cli` still filters with `e.cli_name = $cliName`. Auto-index stores the same resolved PATH path it already executes. `search-06-index-rebuild-path.cs` passed 3/3: a `fake-tool-482-011` script whose capabilities name is `mycli` is stored by full path, `--all` re-runs that path and moves the version from 1.0.0 to 2.0.0, `SearchAsync("deploy", "mycli")` matches, and `SearchAsync("deploy", "fake-tool-482-011")` does not.

Checked and not filed: `CREATE TABLE` puts `cli_path` third and `ALTER TABLE` appends it. `ListClisAsync` and `IndexCliAsync` name the column, so the two physical orders do not change reads. A legacy null path still falls back to the capabilities name, which is the documented migration. `ResolveExecutable` throws on whitespace; empty `--cli` is already rejected before resolve. The optional `cliPath` parameter sits before `CancellationToken`; current call sites omit the token or pass the path in that position. The search project reports 0 compiler errors and 0 warnings.

## Issues

No issues.
