# Release 4.0.0-beta.6

## Description

Bump `<Version>` in `source/Directory.Build.props` from 4.0.0-beta.5 to 4.0.0-beta.6, merge,
wait for green master CI, then cut the release with `dev release` from the master worktree
(tw-release convention).

Ships everything merged since v4.0.0-beta.5: 508-008 (#345), 509 (#328), 515 (#346), 517 (#347),
521 (#348), 522 (#349), 524 (#350), 519 (#351), 518 (#352, #353), 523 (#354), 528 (#355),
527 (#356), 541 (#358), 546 (#359), 545 (#360), 544 (#361), 547 (#362).

Release-note callouts:
- BREAKING: `UseLogging`, `UseConsoleLogging`, `UseDebugLogging` and `ConfigureLogging` removed; use `ConfigureServices(s => s.AddLogging(...))` and inject `ILogger<T>` (527).
- BREAKING: `TimeWarp.Nuru.DestructiveAttribute` / `OpenWorldAttribute` removed; use the `TimeWarp.Contracts` attributes (518).
- `NURU_H007` now warns for endpoint and service constructors that inject `ILogger<T>` with no logging configured; breaks warnings-as-errors builds until `AddLogging` is added (545).
- `ILogger<T>` now works in delegate handlers (541) and registered services (545); parameterless `AddLogging()` counts as logging setup (546); each app in a compilation uses its own logger factory (544).
- `RunAsync` is intercepted in any expression context; an untraceable call gets the new `NURU_I001` warning instead of failing at runtime (547).
- `AddCheckUpdatesRoute` compiles in consumer projects (528). New `NURU_A003` warns on unfiltered `DiscoverEndpoints()` across multiple app files (522).

## Checklist

- [x] Bump version to 4.0.0-beta.6
- [ ] Bump PR merged, master CI green
- [ ] `dev release --dry-run` clean
- [ ] `dev release` cut; release pipeline pushed all packages
- [ ] Consumer check (`skills/tw-nuru-verify`) against the published beta.6

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-26)
- Release steps run from the master worktree after this PR merges; the outcome is reported in chat.

## Notes
