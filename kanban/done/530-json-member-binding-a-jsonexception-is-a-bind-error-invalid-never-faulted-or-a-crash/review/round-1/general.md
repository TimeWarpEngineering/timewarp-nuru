# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** `git diff origin/master...HEAD` for the task 530 product paths (`json-args-emitter.assign.cs`, `json-args-emitter.validation.cs`, `nuru-bind-error.cs`, `documentation/user/features/agent-invocation.md`, `tests/timewarp-nuru-mcphost-tests/mcphost-06-json-member-bind-errors.cs`), checked against `task.md` `## Goal (implementer restatement)`, `## Requirements`, and `## Results` / `### How to validate`. Surrounding call sites: `EmitFill` / `EmitBindMethod`, `NuruProblems.FromBindError`, `NuruApp.WriteBindErrorAsync`, `NuruMcpToolInvoker.CallAsync`, and `NuruDaemonServer` invoke.

## Summary

The generated `ValueShape.Json` assign wraps `JsonSerializer.Deserialize` in `catch (Exception) when (NuruBindError.IsJsonMemberFailure)`. That filter is `JsonException`, `NotSupportedException`, `InvalidOperationException`, or `FormatException`. The catch sets `__ok = false` and records `NuruBindError.ForJsonMember` (`InvalidValue`) with `??=`, so the first failure wins. `EmitBindMethod` then returns false and `EmitBindCase` calls `WriteBindErrorAsync` and returns 1 without invoking the handler. `NuruProblems.Invalid` maps every bind failure to problem type `Invalid`. MCP forwards before local `TryBind`; the CLI `--json-args` path forwards the raw object after route selection and before `EmitBindCase`. The daemon's invoke path calls `TryBind` and renders `FromBindError`; its `RunCallAsync` catch still maps an escaped handler exception to `Faulted`.

`ForJsonMember` names the member and, when `JsonException.Path` is non-empty, the path (`Steps: $[0].Tool`). It uses `exception.Message` and strips the serializer's ` Path: … | LineNumber: …` suffix. There is no stack trace. The route pattern passed in is the already-escaped literal from `EmitBindMethod`.

The new runfile covers the three required cases (unknown enum in an array item, nested type mismatch, unparsable nested Guid) through `TryBind`, in-process `--json-args`, `UseProblemRendererForErrors` with `--no-daemon`, McpHost, and a real `NuruDaemonServer` for both McpHost and the CLI. A handler exception stays `Faulted` over McpHost (local and forwarded) and still escapes the in-process CLI.

The restatement matches the task. `FormatException` is an addition on top of the named `JsonException` / `NotSupportedException` / `InvalidOperationException` list; it covers a typed-ID converter that throws `FormatException` directly, which the task's examples include. The Nuru beta cut stays unchecked, with the Roslynk follow-up written under `### Follow-ups`. That matches the task's "when bumping" follow-up and the rule that cutting a release waits for an explicit ask.

`## Results` records `./bin/dev build` (0 warnings), `./bin/dev test` (CI multi-mode 2190, 2184 passed, 6 skipped, 0 failed; standalone 0 failed), and `ganda repo audit` (44 passed, 0 failed). `### How to validate` names the runfile and expects 5 tests. This round did not re-run those gates.

## Issues

No issues.
