# Sweep Infrastructure Low Severity Findings

Parent: 454 (2026-07-06 full code review). Severity: LOW (batch).

## Description

Low-severity repo-infrastructure findings:

1. Root `Directory.Build.props:35-38,51-54,87-90` — the "Banned API Symbols" ItemGroup
   (`AdditionalFiles Include="BannedSymbols.txt"`) is copy-pasted three times. Collapse
   to one.
2. Root `Directory.Build.props:58-59` — comment "Treat all warnings as errors" sits
   directly above `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>`. The real
   policy (source/Directory.Build.props:6 re-enables `true` for the shipping library;
   tests/samples/benchmarks stay false) is reasonable — fix the misleading comment and
   document the split.
3. `docs/` vs `documentation/` — `docs/` holds a single orphan file
   (`docs/advanced/subset-publishing.md`) nothing references; `documentation/` (90 files)
   is the real tree. Move the orphan and remove `docs/`.
4. ~~Committed `source/**/internals-visible-to.g.cs` still reference removed scratch
   tests~~ DONE across 454-024/454-025: lists regenerated (stale entries dropped, the
   generator's crash on missing directories fixed, and the redundant nested
   `completion/internals-visible-to.g.cs` deleted).

## Checklist

- [x] Deduplicate BannedSymbols ItemGroup (#1 — 3 identical ItemGroups → 1, kept with the analyzers)
- [x] Fix warnings-as-errors comment; document the source-vs-tests split (#2)
- [x] Merge docs/ orphan into documentation/, delete docs/ (#3 — subset-publishing.md → documentation/user/guides/, indexed in overview.md)
- [x] Regenerate internals-visible-to.g.cs (done in 454-024/454-025)
- [x] Build green

## Resolution (2026-07-14)

- **#1** — Root `Directory.Build.props` had the "Banned API Symbols" `<AdditionalFiles>`
  ItemGroup three times (identical). Collapsed to one, kept alongside the "Code Analyzers"
  PackageReference group. Verified exactly one `BannedSymbols.txt` entry remains and the
  BannedApiAnalyzers still enforce (RS0030).
- **#2** — Corrected the misleading "Treat all warnings as errors" comment above
  `TreatWarningsAsErrors=false`. Documented the real split: OFF at root (tests/samples/
  benchmarks), re-enabled `true` in `source/Directory.Build.props` for the shipping library.
- **#3** — Moved the sole `docs/` orphan (`docs/advanced/subset-publishing.md`) to
  `documentation/user/guides/subset-publishing.md` (a user how-to about group-based subset
  editions), linked it from `guides/overview.md`, and removed the now-empty `docs/` tree.
  Nothing referenced the old path.
