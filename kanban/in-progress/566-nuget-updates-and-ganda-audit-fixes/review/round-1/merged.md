# Round 1 — merged findings
**Date:** 2026-11-01
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: changelog.md:59
- Description: The Unreleased .NET 11 bullet still says the latest dependency move is `TimeWarp.Contracts` 2.0.0-beta.29 and that `TimeWarp.Mediator` stays on 14.0.0 (481). The new Mediator 15 bullet above it, `Directory.Packages.props`, and the Neutral contracts bullet all say Contracts 2.0.0-beta.31 and Mediator 15.0.0-beta.4. Unreleased is the note for the next package, and this repo already updates that section when a pin moves (481's review did the same for the Neutral contracts sentence). A reader of the .NET 11 bullet gets the old pins.
- Suggestion: Rewrite the tail of that bullet so the current pins are Contracts 2.0.0-beta.31 and Mediator 15.0.0-beta.4, and keep 481's hold as history (beta.29 and Mediator 14 before 566). Leave the new Mediator 15 bullet as the breaking-change entry.
- Source: general
- Disposition notes: Fixed in implement-fix-2 (2026-11-01). The .NET 11 bullet now lists TimeWarp.Contracts 2.0.0-beta.31 and TimeWarp.Mediator 15.0.0-beta.4 as the current pins, keeps 481's hold (beta.29, Mediator 14.0.0) as history, and points to the Mediator 15 entry.

## Duplicates / conflicts

- One reviewer. Nothing to collapse.
