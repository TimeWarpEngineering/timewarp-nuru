# Disposition — task 467

**Date:** 2026-09-02
**Outcome:** clean
**Rounds:** 3
**Final open count:** 0

## Summary

Effort-1 general review of the Aspire 13.5 `WithTerminal` sample change. AppHost wiring (`Sdk@13.5.3`, `.WithArgs("--", "--interactive").WithTerminal()`, experimental suppressions) was sound. Round 1 raised two doc bugs and four suggestions (stale `_aspire-host-otel` paths, standalone extra `--` that skipped REPL, shebang/launch-profile claims, invented `.aspire/settings.json`, overstated `terminalCommandsEnabled`, restating comment). Those were fixed on this task id. Round 2 confirmed M1–M6 and found two leftover doc bugs (bare intro `./nuru-client.cs`, cwd vs nested `dotnet run` path). Those were fixed as M7–M8. Round 3 confirmed M1–M8 with no new findings. No wontfix, no escalation.

## Exception log (if accepted-exceptions)

None.

## Escalations

- None
