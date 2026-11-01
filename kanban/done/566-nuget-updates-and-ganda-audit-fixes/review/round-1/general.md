# Round 1 — general
**Date:** 2026-11-01
**Scope reviewed:** branch `task/566-nuget-updates-and-ganda-audit-fixes` vs `master`. Focus: package pins, generator detection and registration, missing-handler dispatch, handler migration across source, samples, tests, tools, skills, and user docs, plus the changelog and migration guide. Mechanical handler rewrites were sampled and scanned for leftover Mediator 14 handler shapes.

## Summary

The branch does what the task asks. `TimeWarp.Mediator.Contracts` 14.0.0 is replaced by `TimeWarp.Mediator` 15.0.0-beta.4, Generators are 15.0.0-beta.4, Contracts is 2.0.0-beta.31, and the hold comment is gone. Endpoint handlers in source, DevCli content, Search, tools, samples, tests, and the Nuru skill implement `IRequestHandler<,>` and return `ValueTask<T>`. Message-kind classification still reads `ICommand<T>`, `IQuery<T>`, and `IIdempotentCommand<T>`. Generated registration matches Mediator 15's class name (`{namespace without dots}ServiceCollectionExtensions.AddGeneratedMediator` in `TimeWarpMediatorNamespace`). A handler that this compilation cannot see exits 1 with no `GetService` probe. The implementer restatement matches the task; there is no misread. The goal and the code agree, except one Unreleased changelog sentence that still describes the 481 pins as current.

## Issues

### Issue 1 — Severity: suggestion
- File: changelog.md:59
- Description: The Unreleased .NET 11 bullet still says the latest dependency move is `TimeWarp.Contracts` 2.0.0-beta.29 and that `TimeWarp.Mediator` stays on 14.0.0 (481). The new Mediator 15 bullet above it, `Directory.Packages.props`, and the Neutral contracts bullet all say Contracts 2.0.0-beta.31 and Mediator 15.0.0-beta.4. Unreleased is the note for the next package, and this repo already updates that section when a pin moves (481's review did the same for the Neutral contracts sentence). A reader of the .NET 11 bullet gets the old pins.
- Suggestion: Rewrite the tail of that bullet so the current pins are Contracts 2.0.0-beta.31 and Mediator 15.0.0-beta.4, and keep 481's hold as history (beta.29 and Mediator 14 before 566). Leave the new Mediator 15 bullet as the breaking-change entry.
- Status: open
