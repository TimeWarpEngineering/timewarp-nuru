# Sweep REPL And Completion Low Severity Findings

Parent: 454 (2026-07-06 full code review). Severity: LOW (batch).

## Description

Low-severity REPL/completion findings in `source/timewarp-nuru/`:

1. `completion/completion/install-completion-handler.cs:90-138` — private `DetectShell()`
   is never referenced anywhere; shell auto-detection is unreachable dead code. Wire it
   up or delete it.
2. `repl/repl-history.cs:119,155` — Load/Save use ReadAllLines/WriteAllLines with no
   lock/merge; two REPL instances of the same app share
   `~/.nuru/history/<appName>` and the last writer clobbers the other. Also `Load` never
   clears `Items` (:124), so a second Load duplicates entries.
3. Shell completion templates:
   `completion/completion/templates/pwsh-completion-dynamic.ps1:6,10` — `-split ' '` /
   `-join ' '` with no quoting corrupts completion when a preceding token contains
   spaces. `fish-completion-dynamic.fish:17` — filters `^0$`, silently dropping a
   legitimate `0` candidate.
4. Systemic: cursor/index math is UTF-16 char-based; transpose/delete/word operations
   around an astral char (emoji surrogate pair) split the pair. Representative:
   `repl/input/repl-console-reader.word-operations.cs` swap-characters. Decide scope
   (full grapheme handling vs documented limitation).
5. `repl/repl-session.cs:34,101,111` — `CurrentSession` is a mutable static (nested/
   parallel sessions clobber it), and `ExecuteCommandAsync` (:260-267) only catches
   InvalidOperationException/ArgumentException — any other command exception tears down
   the whole REPL. Broaden the catch; reconsider the static.

## Checklist

- [x] DetectShell wired or deleted (deleted — installer installs all shells by design)
- [x] History: merge-on-save or lock; Load clears before loading (both: clear-on-load + merge-on-save)
- [x] pwsh/fish template quoting + `0` candidate fixes (AST tokenize + `^:`-only filter)
- [x] Surrogate-pair decision recorded (documented UTF-16 limitation; full grapheme deferred)
- [x] REPL survives unexpected handler exceptions (broadened catch, rethrow cancellation)
- [x] CI tests green (1395 / 1388 passed / 7 skipped / 0 failed)

## Implementation Plan (2026-08-01)

Grounded in the current source. Decisions per item:

1. **DetectShell — DELETE.** `install-completion-handler.cs:90` `DetectShell()` has exactly one
   match (its definition); the installer installs ALL shells by design ("All shell completions
   installed"). Wiring auto-detect would change install UX — out of scope for a dead-code sweep.
2. **History — clear-on-Load + merge-on-Save.** `Load` gets `Items.Clear()` at the top (second
   Load no longer duplicates). `Save` re-reads the current file and writes the union
   (file entries first, then in-memory items not already present), capped to `MaxHistorySize` —
   a concurrent instance's entries survive instead of being clobbered.
3. **Templates.** `__complete` stdout is candidates + a single `:N` directive line; NO numeric
   exit-code line is printed (the `0` is the process exit code). So the `^0$` (fish) / `^\d+$`
   (pwsh) filters strip nothing real and drop legitimate numeric candidates ("0", "42") — fix
   both to strip only `^:`. pwsh also replaces `$commandAst.ToString() -split ' '` /
   `Arguments = "... $($words -join ' ')"` with `$commandAst.CommandElements` +
   `$psi.ArgumentList` so quoted args with spaces are not corrupted.
4. **Surrogate pairs — DOCUMENT.** Cursor/index math is UTF-16 code-unit based; full grapheme
   handling across every cursor/word/transpose/delete op is a large, risky change unjustified at
   LOW severity. Record the limitation in a `<remarks>` note on the representative
   word-operations file and in Results.
5. **Catch + static.** Broaden `ExecuteCommandAsync`'s catch to `Exception` so an unexpected
   handler exception no longer tears down the REPL — rethrow `OperationCanceledException` first
   (preserve cancellation), `#pragma warning disable CA1031` with justification (clipboard.cs
   convention). `CurrentSession` has no external readers; run/dispose a LOCAL session instance
   and mirror it to the static, nulling the static only if it still points at our instance —
   removes the concurrent dispose-wrong-session hazard without a breaking public-API change.

**Tests** — extend completion + repl coverage:
- History: double-Load no-duplicate; Save merges a concurrent writer's entries.
- Templates: generated pwsh contains `ArgumentList` and NOT `-split ' '`; fish filter is `^:`
  only (no `^0$`); pwsh script parses under the real pwsh runtime (syntax check).
- Item 5: a handler throwing a non-Argument/InvalidOperation exception with ContinueOnError=true
  is caught and the REPL survives to run the next command.
- Item 1/4 verified by compile + inspection (no behavioral test).

## Results (2026-08-01)

All 5 findings resolved; fully automated coverage. Commits `91da42cf` (sweep) +
`8b900ff2` (review fix).

**Item 1 — dead code.** Deleted the unreferenced `DetectShell()` from
`install-completion-handler.cs`. The installer installs ALL shells by design, so
auto-detect was orphaned; wiring it would change install UX (out of scope).

**Item 2 — history.** `Load` clears before loading (a second Load no longer duplicates);
`Save` re-reads the current file and writes the union (file entries first, then in-memory
items not already present, dedup by exact line, capped to `MaxHistorySize` from the front so
newest survive) — a concurrent instance's entries are no longer clobbered. Still best-effort
last-writer-wins (documented), but strictly better than the prior unconditional clobber.

**Item 3 — completion templates.** pwsh now tokenizes via `$commandAst.CommandElements`
(unquoting string constants) and re-quotes each token into `$psi.Arguments`, so quoted
arguments containing spaces are no longer corrupted by split/join. Both pwsh and fish strip
only the `:` directive line — the old `^0$`/`^\d+$` filters wrongly dropped legitimate
numeric candidates ("0"), and `dynamic-completion-handler.cs` confirms no numeric exit-code
line is printed to stdout (candidates + one `:N` line only).

**Item 4 — surrogate pairs (DECISION: document).** Cursor/index math is UTF-16 code-unit
based; astral characters (emoji surrogate pairs) can be split by character-level editing ops.
Full grapheme-cluster handling across every op is a large, risky change unjustified at LOW
severity — recorded as a documented limitation in a `<remarks>` note on the reader's
word-operations partial. Deferred to a potential future Unicode-editing task.

**Item 5 — catch + static.** `ExecuteCommandAsync` now catches `Exception` (rethrowing
`OperationCanceledException` first so cancellation is preserved; `#pragma CA1031` per the
clipboard.cs convention) so an unexpected user-command exception no longer tears down the
REPL. `RunAsync` runs/disposes a LOCAL `ReplSession` and nulls the `CurrentSession` static
only if it still points at that instance — removes the concurrent dispose-wrong-session
hazard without a breaking public-API change (nothing external reads the static).

**Tests** — `tests/timewarp-nuru-tests/repl/repl-41-lowsev-sweep.cs` (5): history
clear-on-load + merge-on-save; pwsh AST tokenization / no numeric-candidate drop; REPL
survives an unexpected handler exception. pwsh template additionally syntax-validated under
the real pwsh runtime. Items 1 & 4 verified by compile + inspection (no behavioral test).

**Verification** — multi-mode CI green: **1395 total / 1388 passed / 7 skipped / 0 failed**
(+5 from repl-41).

**Phase 4b review** — 1 round, single independent reviewer (general-purpose), effort 1.
**1 finding (LOW-MEDIUM), fixed:** the initial pwsh fix used `$psi.ArgumentList`, which does
not exist on Windows PowerShell 5.1 (.NET Framework) — the installer targets 5.1 via the
WindowsPowerShell profile fallback, so completion would have thrown there. Reworked to quote
into `$psi.Arguments` (works on 5.1 and Core), keeping the AST-tokenization correctness.
Re-verified green. Disposition: **CLEAN** (0 open findings). The reviewer independently
confirmed all five items correct, the merge trim keeps newest entries, and all 5 tests are
genuine regression guards.
