# Round 4 — general
**Date:** 2026-11-20
**Scope reviewed:** the restart pass, `8fceec27..61bb293f`: merge `06f35a7a` of
`origin/feature/591-system-commandline-gaps` (`9c12c549`) and the kanban land `61bb293f`. Read with
`git show --remerge-diff 06f35a7a`, plus the call sites that the merge touches. Effort 3, roster general.
Prior findings M1–M6 carried as fixed.

## Summary

The restart did two things: it took the binding diagnostic runfile out of the CI multi assembly, and it merged
591-004 through 591-007. Both are correct. The runfile is renamed to `generator-79`, gets a `Gen79` namespace,
is in `CiTestExcludes`, and is on the standalone list. The three conflicts are resolved by keeping both sides.
In `endpoint-extractor.properties.cs`, the feature branch's `FileInfo`/`DirectoryInfo`/`IPAddress` constraint
arms now live in `endpoint-extractor.option-attributes.cs`, where this branch had moved the method. The
migration guide items 3–13 and the skill bullets keep the binding rules and the 591-004..007 rules, and the
stale beta.14 lines are gone.

The merge also changed `routing-09-complex-integration.cs` from `--tag A run.sh` to `--tag=A run.sh`. That
existing test had not run on this branch before, because CI failed at compile time. It shows that the greedy
repeated-option rule (item 3) breaks a repeated option followed by a required positional. The design docs and
a sample describe that pattern as supported, and the test plan still expects it. That is M7. M8 is a duplicate
file number.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.option-parsing.cs:234
- Description: A space-separated repeated option takes every following non-option token, and nothing is
  reserved for a required positional that comes after it. That applies to fluent `*` routes as well as
  endpoint collections. So these routes no longer match input they used to match:
  - `process --id {id:int}* --tag {t}* {script}` with `process --id 1 --id 2 --tag A run.sh` takes
    `run.sh` as a tag, finds `script` missing, and reports `Unknown command`. This is the
    `routing-09-complex-integration.cs` case. The merge changed its input to `--tag=A` so the test passes.
    `tests/timewarp-nuru-tests/routing/routing-test-plan.md:443-447` still specifies `--tag A run.sh` →
    `script = "run.sh"`.
  - `docker build --build-arg {args}* {path}` with `--build-arg K1=v1 --build-arg K2=v2 .`, the intercept
    example in `documentation/developer/design/parser/syntax-rules.md:393`, which calls repeated options
    "the most common pattern in real CLIs". `.` becomes a build-arg and the route fails. The same holds for
    `docker build --build-arg {args}* --tag {tags}* {path}` in
    `documentation/developer/design/resolver/specificity-algorithm.md:25`.
  - `fluent-docker-run --env {e}* -p {port:int}* {image} {*cmd}` in
    `samples/fluent/03-syntax/fluent-syntax-examples.cs:147`: `-p 80 nginx echo hi` takes `nginx`, `echo`, and
    `hi` as ports. The int conversion fails and `image` is never bound.

  Original item 3 calls `--env A B` binding `A` and giving `B` to an *optional* `[Parameter]` a leak. It does
  not ask for a value meant for a *required* positional to be swallowed. The restatement added that ("A
  positional after those values is swallowed"), and rounds 1–3 accepted it. None of them saw these call sites,
  because the routing-09 test never ran in CI. Changing the routing-09 input fits the test to the new behaviour
  while the test plan, two design docs, and a sample still state the old contract. Under the review rules that
  is weakening the check: the merge commit gives a reason, but the spec it changes was not updated.
- Suggestion: After a greedy run, give back enough trailing values to fill the route's required positionals
  that are still unbound. A simple form: count the required non-catch-all positionals left after option
  parsing, and if some are unfilled, move that many tokens from the end of the last greedy run back to
  unconsumed, as long as each repeated option that requires a value keeps at least one. Then `--env A B` with
  `{name?}` still binds both values (an optional parameter reserves nothing), and `--tag A run.sh` with
  `{script}` binds `script = run.sh`. Restore the routing-09 input to `--tag A run.sh`. Add routing-43
  binding-strictness cases for a required positional after a greedy run, both fluent and endpoint. Update
  the migration guide line 226 and the skill line 322 to say a *required* positional is kept and only an
  optional one is swallowed. If the operator would rather keep pure greedy consumption, the alternative is to
  update the test plan, both design docs, and the sample to `--opt=value` or a repeated flag per value, and
  record that as a decided contract change.
- Status: open

### Issue 2 — Severity: nit
- File: tests/timewarp-nuru-tests/routing/routing-43-binding-strictness.cs:1
- Description: `routing-43-cli-failures.cs` (591-005, from the feature branch) and
  `routing-43-binding-strictness.cs` (this task) now share number 43. The class names differ
  (`CliFailureTests`, `BindingStrictnessTests`), so nothing breaks. The restart renumbered the generator
  runfile to avoid exactly this kind of collision, but left the routing one.
- Suggestion: Rename `routing-43-binding-strictness.cs` to the next free number (`routing-45-…`) and update
  the path in task.md `## Results`.
- Status: open

## Verified without finding

- `tests/ci-tests/Directory.Build.props`: `generator-79` is in `CiTestExcludes`, with a comment in the same
  style as 76 and 78. `tests/ci-standalone/run-standalone-tests.cs` lists it, and the header range
  comment is updated.
- `generator-79-binding-attribute-diagnostics.cs`: a pure rename plus the namespace change from `Gen76` to
  `Gen79`.
- The extractor conflict: `IsCollectionInterface` and `ExtractOptionLongForm` exist once each in
  `endpoint-extractor.option-attributes.cs`, and the callers in `operation-endpoint-extractor.bindings.cs` and
  `endpoint-extractor.groups.cs` still resolve. The feature side changed only the three constraint arms in
  that region, and those were carried over.
- The migration guide's checklist lines now say that `RunAsync` returns 1 and that `IOptions<T>` works. The
  numbered list has no duplicates and no lost items.
- The recorded gates cover the restart: a build, generator-79, routing-09, routing-44, CI multi plus
  standalone, and an audit. No gate was run in this review.
