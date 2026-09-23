# Sweep tests-infra leftovers from 470 review

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M16) plus suggestions/nits M25–M28, M42, M43.

## Description

M16: `benchmarks/aot-benchmarks/run-benchmark.sh:13` runs `publish/bench-nuru-full/bench-nuru-full`; on-disk project is `bench-nuru`.

M25: search/mcp test globs are non-recursive `*.cs` while the main tree is `**/*.cs`.

M26: committed `internals-visible-to.g.cs` lists miss 24 current test stems (including standalone generator-39/40/42 and newer devcli/*). Generator banner still says `scripts/generate-internals-visible-to.cs` (actual `runfiles/`).

M27: legacy `tests/scripts/` hand-lists point at deleted dirs / omit mcp-06/07 / still run mcp-02. Official CI is `run-ci-tests.cs`.

M28: `engine-01-input-tokenizer.cs` references removed ParsedInput/InputTokenizer (#360) and does not compile. 454-001 already said delete or rewrite.

M42: samples Directory.Build.props still says TreatWarningsAsErrors is “Temporarily disabled … #365”.

M43: `Microsoft.Extensions.Logging` GeneratePathProperty is unused (packing uses Abstractions).

CI inclusion of generator-17 / check-version-04 is **470-010**, not this task.

## Requirements

- Fix the AOT harness binary path (M16).
- Switch search/mcp globs to `**/*.cs` (M25).
- Re-run `dotnet run runfiles/generate-internals-visible-to.cs` and commit the three .g.cs files; fix banner (M26).
- Delete or rewrite tests/scripts to delegate to run-ci-tests (M27).
- Delete engine-01 and its CiTestExcludes entry, or rewrite (M28).
- Comment / unused GeneratePathProperty (M42, M43).

## Checklist

- [x] M16 bench-nuru path
- [x] M25 recursive globs
- [x] M26 IVT regenerate
- [x] M27 legacy scripts
- [x] M28 engine-01
- [x] M42 M43 nits

## Notes

Evidence: parent 470 `review/round-1/merged.md` M16, M25–M28, M42, M43.

Review: effort 1 (general). Artifacts under `review/` (framework, round-1, disposition). Outcome: clean.

## Results

Swept tests-infra leftovers from the 470 review (M16, M25–M28, M42, M43).

- **M16:** `run-benchmark.sh` now runs `publish/bench-nuru/bench-nuru` (label `Nuru`).
- **M25:** search/mcp `Compile` globs are `**/*.cs` with bin/obj excludes (mcp-02 still excluded).
- **M26:** Fixed generator banner to `runfiles/generate-internals-visible-to.cs`; regenerated the three committed `.g.cs` files (242 stems; includes generator-39/40/42 and newer devcli; dropped deleted `engine-01-input-tokenizer`).
- **M27:** Rewrote `run-all-tests`, `run-nuru-tests`, `run-mcp-tests`, `run-repl-tests`, and `run-tests-sequential` as thin delegates to `tests/ci-tests/run-ci-tests.cs`. Left `test-mcp-server.cs` and `benchmark-aot-invocation.cs` (not hand-list runners).
- **M28:** Deleted `engine-01-input-tokenizer.cs` and its `CiTestExcludes` entry (empty `completion/engine/` removed).
- **M42:** Samples `TreatWarningsAsErrors` comment matches the permanent root rationale (no more “temporarily / #365”).
- **M43:** Dropped unused `GeneratePathProperty` from `Microsoft.Extensions.Logging` (Abstractions keeps it for packing).

### How to validate

**Smoke**

```bash
rg -n "bench-nuru" benchmarks/aot-benchmarks/run-benchmark.sh
rg -n 'search-tests/\*\*/\*\.cs|mcp-tests/\*\*/\*\.cs' tests/ci-tests/Directory.Build.props
head -3 source/timewarp-nuru/internals-visible-to.g.cs
rg -n 'engine-01' tests/ci-tests/Directory.Build.props || true
test ! -f tests/timewarp-nuru-tests/completion/engine/engine-01-input-tokenizer.cs
rg -n 'Temporarily disabled|#365' samples/Directory.Build.props || true
rg -n 'Microsoft.Extensions.Logging"' source/timewarp-nuru/timewarp-nuru.csproj
dotnet build source/timewarp-nuru/timewarp-nuru.csproj -c Release
ganda runfile cache --clear && dotnet run tests/ci-tests/run-ci-tests.cs
```

**Expect**

- `run-benchmark.sh` line uses `publish/bench-nuru/bench-nuru` (not `bench-nuru-full`)
- search/mcp globs are recursive `**/*.cs`
- IVT banner says `runfiles/generate-internals-visible-to.cs`; stems include `generator-39` / `generator-40` / `generator-42`; no `engine-01-input-tokenizer`
- no `engine-01` in `CiTestExcludes`; engine-01 file absent
- samples props has no “Temporarily disabled … #365”
- Logging PackageReference has no `GeneratePathProperty`; Abstractions still has it; Release build succeeds
- CI: exit 0; multi-mode Grand Total ~1701 passed, 7 skipped, 0 failed; standalone phase completes

### Review disposition

- **Outcome:** clean (0 open)
- **Effort / roster:** 1 — general
- **Rounds:** 1
- **Final counts:** bug/suggestion/nit all 0 open, 0 fixed, 0 wontfix
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
- Re-verified smoke checks above and `dotnet build source/timewarp-nuru/timewarp-nuru.csproj -c Release` (0 warnings / 0 errors) during review.
