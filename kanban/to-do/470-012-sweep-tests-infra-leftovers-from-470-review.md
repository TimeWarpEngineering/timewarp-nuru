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

- [ ] M16 bench-nuru path
- [ ] M25 recursive globs
- [ ] M26 IVT regenerate
- [ ] M27 legacy scripts
- [ ] M28 engine-01
- [ ] M42 M43 nits

## Notes

Evidence: parent 470 `review/round-1/merged.md` M16, M25–M28, M42, M43.
