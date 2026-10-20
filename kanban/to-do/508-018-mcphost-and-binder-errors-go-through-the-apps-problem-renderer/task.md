# McpHost and binder errors go through the app's problem renderer

## Description

Found by the independent review of Roslynk PR #9 (task 005) on TimeWarp.Nuru 4.0.0-beta.4.

McpHost's `tools/call` binding failure bypasses the app's problem renderer.
`source/timewarp-nuru-mcphost/nuru-mcp-tool-invoker.cs` (~line 72-74): when `App.TryBind` fails it returns
`ErrorResult(error.Message)`, plain text such as `Error: Missing required value 'symbolName' for 'get_symbol'.`

An app that registered `IResponseRenderer<SharedProblemDetails>` (508-011) renders every other failure through
it. Roslynk's renderer emits a header outline (`error=Invalid` plus `errorMessage=...`), and its MCP server
instructions tell models failures look like that. Before PR #9, Roslynk mapped binding errors to `error=Invalid`.
Now a model sees two error formats depending on where the failure happened.

## Requirements

- When a problem renderer is registered, a bind failure becomes a `SharedProblemDetails` (Type from
  `NuruBindError.Kind`, or a single documented type such as `Invalid`; Detail = the message; Status 400 or the
  documented usage code) and is rendered through it, on both the in-process and forwarded paths.
- Same for the CLI `--json-args` and argv binding errors when the app opts in (keep today's default when no
  problem renderer is registered).
- Exception escapes (`Error: {exception.Message}`) get the same treatment (Type `Faulted` or documented).
- Tests for both paths; docs on the McpHost page and `neutral-contracts.md`.

## Goal (implementer restatement)

An app that has an `IResponseRenderer<SharedProblemDetails>` should show a model (or a user who opted in) one
error format. Today a binding failure or an escaped exception in McpHost (and in the daemon server that a
forwarded call runs on) prints plain `Error: ...` text instead of going through that renderer.

Acceptance criteria:

1. Nuru gives one documented mapping from failures to `SharedProblemDetails`:
   - bind failure → Type `Invalid`, Status 400, Detail = the bind message without the CLI `Error: ` prefix,
     with `kind` and `key` as extension members;
   - escaped exception → Type `Faulted`, Status 500, Detail = the exception message.
2. The generator attaches the app's problem renderer to `NuruApp` (runtime DI, a registered source-gen service,
   or the one visible renderer). `NuruApp.TryRenderProblem` renders through it and returns false when there is none.
3. McpHost in-process `tools/call`: a bind failure and an escaped handler exception are rendered through the
   problem renderer when one exists; otherwise the text stays the same as today.
4. Forwarded path: the daemon server renders an invoke-request bind failure and an escaped exception the same
   way, so a forwarded McpHost call returns the same text as an in-process one.
5. CLI: a new builder opt-in (`UseProblemRendererForErrors()`) sends `--json-args` bind failures and every argv
   binding error through the renderer, to stderr. Argv binding errors include conversion errors and the JSON-member
   errors (a JSON member passed on argv, a required JSON member left out). Each problem carries the right `kind`
   (`MissingValue` for a missing value). Without the opt-in, or with no renderer, output is unchanged.
6. Tests cover the in-process and forwarded McpHost paths, the CLI opt-in, and the default fallback. The McpHost
   readme/page and `neutral-contracts.md` document the mapping.

## Checklist

- [x] Bind failure through the problem renderer (in-process and forwarded)
- [x] CLI binding errors opt-in
- [x] Escaped exceptions
- [x] Tests and docs

## Session

- Created: 2532391 (2026-10-21) by the Roslynk cockpit from the PR #9 review.
- 2026-10-21 implementer (claude, headless `ganda task work`): implemented on the task branch. Design:
  one `NuruProblems` mapping, a generated `NuruApp.ProblemRenderer` hook, and a CLI opt-in.
  The hosts (McpHost, the daemon invoke path) use the renderer whenever the app has one. The CLI uses it only after
  `UseProblemRendererForErrors()`. Note: `/tmp/nuget.config` is a dangling symlink to the removed 508-007 worktree,
  and it breaks `dotnet run` of runfiles under `/tmp`. I left it in place (not this task's). The smoke ran under the
  gitignored `artifacts/`.
- Audit (boy-scout): the pre-push audit flagged `runfile-executable` on the two new test runfiles
  (mcphost-05, routing-36). `ganda repo audit --fix` set their executable bit.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-20T19:58:11Z
- 2026-10-21 review round 1 (grok, session `01a15ae4-96a0-7680-862d-bcce4c9319e0`): effort 3, roster general. Open findings M1 (bug) and M2 (suggestion). Disposition is waiting on a fix pass. See `review/round-1/merged.md`.
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 200 — 2026-10-20T20:06:27Z
- 2026-10-21 fix pass for round 1 (claude, headless `ganda task work`, implement-fix-2):
  - M1 fixed: `EmitJsonMemberAfterMatch` now writes both JSON-member argv errors through `app.WriteBindErrorAsync`
    (`InvalidValue` for the rejected flag, `MissingValue` for the omitted required member, key = the long form).
    The plain text and stderr stay the same without the opt-in.
  - M2 fixed: new `NuruApp.WriteArgumentError(message, kind, key)` overload. The FileInfo and DirectoryInfo
    missing-option sites pass `MissingValue`. The two-argument form still means `InvalidValue`.
  - routing-36 gained 2 tests (JSON member argv errors opted in, an argument error keeps its kind).
    `neutral-contracts.md` names the JSON-member errors and the `MissingValue` kind.
  - Audit (boy-scout): `workflow-file` failed (`pull_request.branches` lacked `'feature/**'`).
    `ganda repo audit --fix` added it to `.github/workflows/workflow.yml`; the audit now passes.

## Results

### What changed

- **Mapping** (`source/timewarp-nuru/invocation/nuru-problems.cs`): new public `NuruProblems`.
  - `FromBindError` / `Invalid`: Type `Invalid`, Status 400. Detail is the message without `Error: `. Extensions
    `kind` (the `NuruBindErrorKind` name) and `key`.
  - `FromException`: Type `Faulted`, Status 500. Detail is the exception message.
  - The type and status values are public constants.
- **NuruApp** (`nuru-app.cs`):
  - generated hook `ProblemRenderer` (`Func<NuruApp, SharedProblemDetails, string?>`), flags
    `ProblemRendererForErrors` and `VerbatimRendererOutput` (all EditorBrowsable Never);
  - public `TryRenderProblem(problem, out text)`;
  - generated-code helpers `WriteArgumentError` and `WriteBindErrorAsync`. They keep today's text and stream unless
    the app opted in and has a renderer.
- **Builder**: new `UseProblemRendererForErrors()`. It is a generator-interpreted stub, wired through the
  interpreter, `IIrAppBuilder`, and `AppModel.ProblemRendererForErrors`.
- **Generator**:
  - `GeneratorModel.ProblemRendererTypeName` is the one visible `IResponseRenderer<SharedProblemDetails>` in the
    compilation, found with the same rule as a problem pair (`EndpointExtractor.FindProblemRenderer`).
  - `__RenderProblem{suffix}` resolves the renderer in this order: runtime DI `GetService`, then a registered
    source-gen service (read as a process-lifetime service), then the visible renderer. It returns null when
    there is none.
  - The Build() interceptor attaches the hook and flags. ExecuteRouteAsync does the same for a Build() that was
    not intercepted.
  - The 31 argv conversion error sites and the 2 JSON-member argv errors (`route-matcher-emitter.cs`) and
    12 `--json-args` error sites (`json-args-emitter.cs`) now call the helpers. Missing values (FileInfo and
    DirectoryInfo options, an omitted required JSON member) use kind `MissingValue`.
- **McpHost** (`nuru-mcp-tool-invoker.cs`): an in-process bind failure and an escaped handler exception go
  through `RenderProblemOr`. The plain text stays when the app has no renderer or the renderer throws.
- **Daemon server** (`nuru-daemon-server.cs`): an invoke's bind failure and escaped exception render the same
  way, so a forwarded McpHost call returns the same text as an in-process one. A run (argv) renders an escaped
  exception only after the opt-in.
- **Docs**:
  - `mcp-hosting.md`: new section "Errors through the problem renderer" with the mapping table;
  - `neutral-contracts.md`: the mapping and `UseProblemRendererForErrors()`;
  - `served-apps.md`: the forwarded bind and exception text.

### Tests

- New `tests/timewarp-nuru-tests/routing/routing-36-problem-errors.cs` (8 tests):
  - the mapping;
  - an argv conversion error, a `--json-args` bind failure, and a `--json-args` input error, each rendered when
    opted in;
  - a JSON member passed on argv (`kind=InvalidValue`) and left out (`kind=MissingValue`), rendered when opted in;
  - `WriteArgumentError` with `MissingValue` keeps that kind;
  - plain text without the opt-in;
  - plain text with no renderer.
- New `tests/timewarp-nuru-mcphost-tests/mcphost-05-problem-errors.cs` (5 tests):
  - in-process bind failure → `error=Invalid`;
  - escaped exception → `error=Faulted`;
  - plain text without a renderer;
  - fallback when the renderer throws;
  - forwarded to a real `NuruDaemonServer` → same text as in-process.
- `generator-56-problem-renderer.cs` (Roslyn-hosted, CI-excluded), 5 new cases:
  - visible renderer attach;
  - opt-in and verbatim flags;
  - registered renderer preferred;
  - runtime DI `GetService`;
  - nothing attached when there is no renderer.
- Results:
  - `dotnet run tests/ci-tests/run-ci-tests.cs`: multi-mode 2063 passed, 6 skipped, 0 failed (after the
    round 1 fix pass); standalone 0 failed;
  - generator-56: 8/8;
  - `./bin/dev verify-samples`: 68/68;
  - `./bin/dev build`: green;
  - `ganda repo audit`: all checks pass.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-05-problem-errors.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-36-problem-errors.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-56-problem-renderer.cs
```

End to end, with a real visible renderer and no hooks attached by hand: create a runfile under a gitignored
folder such as `artifacts/smoke/`. It needs a `Directory.Build.props` that adds the analyzer project as
`OutputItemType=Analyzer` and sets `InterceptorsNamespaces=$(InterceptorsNamespaces);TimeWarp.Nuru.Generated`.
The app:

- calls `.UseProblemRendererForErrors()`;
- maps `delay {ms:int}`;
- declares a public `IResponseRenderer<SharedProblemDetails>` that writes `error={Type}\nerrorMessage={Detail}`;
- runs `delay abc`, `delay --json-args '{"ms":1,"nope":2}'`, and a `NuruMcpToolInvoker.CallAsync` with
  `{"ms":"x"}`.

Expect:

- mcphost-05: 5/5 pass. routing-36: 8/8 pass. generator-56: 8/8 pass.
- Smoke app: `delay abc` writes `error=Invalid` and `errorMessage=Invalid value 'abc' for parameter 'ms'.
  Expected: int` to stderr, exit 1.
- `--json-args` with an unknown key writes `error=Invalid` and `errorMessage=Unknown key 'nope' for
  'delay {ms:int}'. Known names: ms.`, exit 1.
- The MCP call returns `isError=True` with `error=Invalid` and `errorMessage=Key 'ms' for 'delay {ms:int}' has
  the wrong JSON type. Expected int.`. A throwing handler gives `error=Faulted`.
- With the opt-in, `pe-jsonreq --Body nope` (routing-36) renders `errorMessage=--Body is JSON. Pass it with
  --json-args.` and `kind=InvalidValue`; `pe-jsonreq` alone renders `errorMessage=pass --Body via --json-args.`
  and `kind=MissingValue`. Both on stderr, exit 1.
- Without `.UseProblemRendererForErrors()`, the CLI prints today's `Error: ...` text (argv errors on stdout,
  JSON-member errors on stderr; `operation-endpoint-derivation.cs` still checks that text).

## Review

- Round 1: Blocked — open M1, M2

## Notes

- Related: 508-003 (binder), 508-004 (McpHost), 508-011 (problem renderer), Roslynk 005.
- Review round 1: `review/review-framework.md`, `review/round-1/merged.md`. Disposition is not written while M1 and M2 are open.

## Learnings

- Misread: the original requirement says CLI `--json-args` and argv binding errors go through the problem renderer when the app opts in. The implementer restatement narrowed "argv binding errors" to "argv conversion errors". `EmitJsonMemberAfterMatch` still prints the JSON-member argv binding errors as plain `Error:` text after `UseProblemRendererForErrors()`. That gap is M1.
