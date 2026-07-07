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

- [ ] Deduplicate BannedSymbols ItemGroup
- [ ] Fix warnings-as-errors comment; document the source-vs-tests split
- [ ] Merge docs/ orphan into documentation/, delete docs/
- [x] Regenerate internals-visible-to.g.cs (done in 454-024/454-025)
- [ ] Build green
