# Error handling: parse errors on stdout, unhandled handler exceptions abort, Ctrl+C does not cancel

## Goal (implementer restatement)

All three defects are accepted fixes.

A CLI `RunAsync` writes parse and argument errors to stderr. Today the plain path of `WriteArgumentError` uses stdout; that path moves to stderr. The problem-renderer path already writes stderr and stays as it is.

A CLI `RunAsync` no longer lets a handler exception abort the process. It writes the exception message to stderr and returns 1. `OptionsValidationException` still propagates, because configuration validation is not a handler failure and existing generator tests require the throw. `ExecuteAsync` and `InvokeAsync` still let exceptions propagate, so in-process hosts keep their current contract. An `ArgumentException` from `--generate-completion` with an unknown shell becomes exit 1 and a stderr message, the same as other CLI failures.

Ctrl+C in a CLI run cancels the handler's `CancellationToken`. If the handler does not finish within `ProcessTerminationTimeout` (default two seconds, null turns the signal handling off), the run returns 130 for SIGINT or 143 for SIGTERM. The REPL keeps its own Ctrl+C: it cancels the current command and the session continues. The CLI registration does not cancel the outer token or start the timeout while a REPL session owns the signal.

Acceptance: a test covers each accepted item; the migration guide and the migrate skill say the new behavior and note what 4.0.0-beta.14 did; `./bin/dev build`, the touched Jaribu runfiles, and one `ganda repo audit` are recorded under Results.

## Description

Three defects in how a CLI run ends when something goes wrong.

1. **Some parse errors go to stdout** (for example
   `Error: Invalid value 'bogus' for option '--databasetype'`). Errors belong on stderr.
2. **Unhandled handler exceptions abort the process** (SIGABRT, exit 134, raw stack trace). Consider a default
   handler that prints the message and returns 1.
3. **Ctrl+C does not cancel the handler's `CancellationToken`** in a CLI run (`RunAsync` passes `default`).
   System.CommandLine cancels the token and waits `ProcessTerminationTimeout`.

## Checklist

- [x] Decide per item: fix all three. None kept.
- [x] Fix with tests for each accepted item
- [x] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [x] `ganda runfile cache --clear` (628 entries). Touched Jaribu runfiles pass. The full suite is the pull request's CI.

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-17), split from 591 during groom
- Implementation: 01a1f411-8e3a-72a3-bd0a-68465bddf245 (2026-11-19)

## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.

## Results

All three defects are fixed on CLI `RunAsync`.

Plain `WriteArgumentError` now uses `Terminal.WriteErrorLine`. The problem renderer already wrote stderr and still does. Invalid option values, including the valid names in that message, are on stderr.

A handler exception during `RunAsync` writes `exception.Message` (or the type name when the message is empty) to stderr and returns 1. The stack is not printed. `OptionsValidationException` still propagates. `ExecuteAsync` and `InvokeAsync` are not wrapped, so a host still sees the throw. The failure is read from the completed task instead of `catch (Exception)`, so this boundary does not add a CA1031 suppression. The first `ganda repo audit` failed on that pragma; the rewrite removed it. An unknown `--generate-completion` shell is the same exit 1 path.

Ctrl+C and Posix SIGINT cancel the handler token and return 130 if the handler is still running when `ProcessTerminationTimeout` elapses (default two seconds). SIGTERM uses 143. Null skips signal registration and passes `CancellationToken.None`. A negative timeout throws `ArgumentOutOfRangeException`. The REPL calls `EnterReplCancellation` / `ExitReplCancellation`, so a CLI registration does not cancel the session or start the timeout while the session owns the signal.

`ganda repo audit --fix` wrote the gitignored `.vscode/settings.json` window title. Nothing from that fix is committed.

### Gates

- `./bin/dev build`: pass (0 warnings, 0 errors) after the task-exception rewrite.
- `ganda runfile cache --clear`: cleared 628 entries, before the first routing-43 run.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-43-cli-failures.cs`: 7/7 pass. Standalone runfile (`#if !JARIBU_MULTI`). Rerun after the rewrite: 7/7 pass.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-36-problem-errors.cs`: pass (exit 0). Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-16-typed-catch-all.cs`: pass (exit 0). Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-24-enum-option-parameters.cs`: pass (exit 0) after the valid names moved to stderr assertions. Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-25-nullable-value-type-options.cs`: pass (exit 0). Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-29-enum-undefined-values.cs`: pass (exit 0) after the valid names moved to stderr assertions. Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-30-invariant-culture-binding.cs`: pass (exit 0). Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-32-enum-repeated-options.cs`: pass (exit 0) after the valid names moved to stderr assertions. Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-02-parameter-binding.cs`: pass (exit 0). Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/auto/endpoint-nullable-option-01.cs`: pass (exit 0). Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-26-optional-int-skip.cs`: pass (exit 0). Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs`: pass (exit 0, 1000 cases). The skipped `-v=true` test stays skipped. Standalone runfile.
- `dotnet run --file tests/timewarp-nuru-tests/completion/completion-27-endpoint-protocol.cs`: 13/13 pass. Standalone runfile. Handlers use the route parameter so the standalone compile stays clean.
- `dotnet run --file tests/timewarp-nuru-tests/repl/repl-42-ctrl-c-cancellation.cs`: pass (exit 0). Standalone runfile. Not edited. Covers the REPL keeping Ctrl+C.
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-14-options-validation.cs`: 5/5 pass. Standalone runfile. Not edited. Rerun after the rewrite: 5/5 pass.
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-77-endpoint-handler-ioptions.cs`: 5/5 pass. Standalone runfile. Not edited. Rerun after the rewrite: 5/5 pass.
- `ganda repo audit --fix`: pass. 45 passed, 0 failed, 1 skipped. "Repository passes all audit checks." The earlier audit failed `analyzer-suppressions` on the CA1031 pragma (removed) and warned `vscode-window-icon` (fixed in the gitignored settings file).

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run --file tests/timewarp-nuru-tests/routing/routing-43-cli-failures.cs
dotnet run --file tests/timewarp-nuru-tests/generator/generator-14-options-validation.cs
```

Expect: routing-43 reports 7 passed. An invalid option value is on stderr and stdout does not contain `Error:`. A throwing handler prints `handler failed` on stderr, does not print `at TimeWarp`, and returns 1. `ExecuteAsync` of that handler still throws `InvalidOperationException`. Ctrl+C returns 130 and the handler token is cancelled. A handler that ignores the token returns 130 after the timeout. `ProcessTerminationTimeout = null` does not cancel. generator-14 reports 5 passed, and an invalid port still throws `OptionsValidationException` from `RunAsync`.
