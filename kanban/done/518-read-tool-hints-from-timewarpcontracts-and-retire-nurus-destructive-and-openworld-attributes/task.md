# Read tool hints from TimeWarp.Contracts and retire Nuru's Destructive and OpenWorld attributes

## Description

508-015 shipped a stopgap so a contract could carry MCP tool hints: BCL `[DisplayName]` for the title,
and Nuru's own `[Destructive(bool isDestructive = true)]` and `[OpenWorld(bool isOpenWorld = true)]` in
`namespace TimeWarp.Nuru`. It filed timewarp-architecture 391 to give the hints a neutral home.

391 merged (timewarp-architecture PR #560). TimeWarp.Contracts now has:

- `OperationAttribute.Title` (string; null, empty, or whitespace means no title).
- `TimeWarp.Contracts.DestructiveAttribute(bool isDestructive = true)` and
  `TimeWarp.Contracts.OpenWorldAttribute(bool isOpenWorld = true)`. Tri-state by presence: absent is
  unset, bare is true, `(false)` is an explicit false.
- `OperationDescriptor.Title`, `DestructiveHint` (`bool?`), and `OpenWorldHint` (`bool?`).

Both packages now define `[Destructive]` and `[OpenWorld]` with the same short names and constructors.
A file that imports `TimeWarp.Contracts` and `TimeWarp.Nuru` gets CS0104 (ambiguous reference) on the
bare name. Recommended in the 391 merge review (Steve asked to file it, 2026-10-22): Nuru reads the Contracts attributes and retires its own pair,
so there is one attribute name and no clash.

## Dependency

TimeWarp.Contracts 2.0.0-beta.24 (or later) must be on nuget.org. It ships with the next
timewarp-architecture release. Nuru pins `TimeWarp.Contracts` 2.0.0-beta.23 in `Directory.Packages.props`.
Do not start before that package is published. Never pin backward or use a project reference to work around it.

## Goal

- Bump `TimeWarp.Contracts` to the release that carries the hints.
- Operation (contract) endpoints read `TimeWarp.Contracts.OperationAttribute.Title`,
  `TimeWarp.Contracts.DestructiveAttribute`, and `TimeWarp.Contracts.OpenWorldAttribute` by metadata name,
  with the same tri-state rule (absent unset, no argument or anything but `false` is true).
- Remove `TimeWarp.Nuru.DestructiveAttribute` and `TimeWarp.Nuru.OpenWorldAttribute`, unless delegate/route
  endpoints (not contract-based) still need a way to state the hints. If they do, decide in the task whether
  route endpoints use the Contracts attributes too (preferred: one name everywhere) and record why.
- Decide whether `[DisplayName]` stays as a title source for contract endpoints or is replaced by
  `[Operation(Title = …)]`. Recommended: Contracts `Title` only for contract endpoints; record the decision.
- Keep the host-override precedence from 508-015 (a host override wins over the contract).

## Goal (implementer restatement)

TimeWarp.Contracts 2.0.0-beta.24 is on nuget.org and is the home for tool hints. Nuru pins that package and reads those hints. It stops shipping a second `[Destructive]` and `[OpenWorld]`.

Acceptance:

- `Directory.Packages.props` pins `TimeWarp.Contracts` 2.0.0-beta.24 (or later). No project reference and no older pin.
- The generator matches these by metadata name. An operation's catalog title is `TimeWarp.Contracts.OperationAttribute.Title`; null, empty, or whitespace means no title. `TimeWarp.Contracts.DestructiveAttribute` and `TimeWarp.Contracts.OpenWorldAttribute` are tri-state: absent is unset, and no argument or any argument other than `false` is true.
- Route endpoints use the same Contracts attributes. A fluent `Map()` route still has no attribute target. One name everywhere: Nuru already depends on Contracts, and a second pair makes `using TimeWarp.Nuru` together with `using TimeWarp.Contracts` fail to compile (CS0104).
- An operation's title comes only from `Operation.Title`. `[DisplayName]` does not set it. A `[NuruRoute]` class with no `[Operation]` still uses `[DisplayName]`, because it has no operation attribute to carry a title.
- A host entry in `NuruMcpHostOptions.ToolAnnotations` still replaces each hint it sets. A contract's hints show up in `--capabilities` and in the MCP tool list.
- `TimeWarp.Nuru.DestructiveAttribute` and `TimeWarp.Nuru.OpenWorldAttribute` are gone. Samples and docs follow. A file that imports both namespaces compiles.
- The changelog records the break for callers of the Nuru attributes, and for an operation that took its title from `[DisplayName]`.

## Where the stopgap lives (from 508-015)

- `source/timewarp-nuru/attributes/destructive-attribute.cs`, `open-world-attribute.cs`
- `source/timewarp-nuru/capabilities/capabilities-response.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs`,
  `operation-endpoint-extractor.cs`, `locators/referenced-endpoint-locator.cs`, `nuru-generator.cs`
- `source/timewarp-nuru-mcphost/nuru-mcp-host-options.cs`, `nuru-mcp-tool-catalog.cs`

## Checklist

- [x] TimeWarp.Contracts with the hints is published; bump the pin
- [x] Contract endpoints read Title, Destructive, and OpenWorld from TimeWarp.Contracts
- [x] Decide route-endpoint hints and the `[DisplayName]` title source; record why
- [x] Remove Nuru's `[Destructive]` / `[OpenWorld]` (or record why one stays) and update samples and docs
- [x] Tests: contract with Contracts hints publishes them in `--capabilities` and the MCP tool list; host
      override still wins; a file importing both namespaces compiles
- [x] Release note: breaking for anyone using `TimeWarp.Nuru.Destructive` / `OpenWorld`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22) from the timewarp-architecture 391 merge review.
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-23T07:45:03Z
- Review oracle: general reviewer (claude, claude-opus-5-5), effort 2 (by-diff, 240 lines), session not reported, 1 round, accepted-exceptions — 2026-10-23
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-23T07:47:54Z
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-23T10:53:53Z
- Review oracle: general reviewer (cursor, grok-4.7), effort 1 (by-diff, 0 lines), session not reported, round 2, accepted-exceptions — 2026-10-23
- Review oracle: review by reviewer-cursor (cursor, model grok-4.7), session not reported, max-turns uncapped (no --max-turns flag; bound 80) — 2026-10-23T10:59:06Z
- Review oracle: review by reviewer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 80) — 2026-10-23T11:01:08Z

## Results

Route endpoints use `TimeWarp.Contracts.DestructiveAttribute` and `OpenWorldAttribute`. Fluent `Map()` routes still have no attribute target. One short name: Nuru already depends on Contracts, and a second pair makes a file that imports both namespaces fail with CS0104.

An operation's title is `OperationAttribute.Title` only. Blank or whitespace publishes nothing, and `[DisplayName]` does not set it. A `[NuruRoute]` class with no `[Operation]` still uses `[DisplayName]`, because it has no operation attribute to carry a title. `NuruMcpHostOptions.ToolAnnotations` still replaces each hint it sets.

`TimeWarp.Nuru.DestructiveAttribute` and `OpenWorldAttribute` are removed. The pin is `TimeWarp.Contracts` 2.0.0-beta.24. The changelog records the break.

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-04-schema-fidelity.cs
dotnet run samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- --capabilities
```

Expect:

- `mcphost-04` prints `Total: 6` and `Passed: 6`. `mf-purge` has `destructive` true, `openWorld` true, and `title` `Purge the store` from `[Operation(Title)]`. `mf-append` has both hints false and no title even with `[DisplayName]`. `mf-plain` has no title when `Title` is whitespace. A host override replaces the hints it sets and leaves the rest. The stdio call lists `calc_total` with title `Total numbers` and `openWorldHint` false.
- `--capabilities` for the calculator sample includes `"title": "Total numbers"` and `"openWorld": false` on `calc total`.
- `tests/timewarp-nuru-mcphost-tests/mcphost-01-tools.cs` and `mcphost-04-schema-fidelity.cs` both `using TimeWarp.Nuru` and `using TimeWarp.Contracts` and compile. `mh-wipe` is a route class: `[Destructive]` is true and `[DisplayName]` is the title.

Verified here: `dotnet build` of `timewarp-nuru` and `timewarp-nuru-mcphost` succeeded with 0 warnings. `mcphost-01` 16/16 and `mcphost-04` 6/6. `./bin/dev test` exited 0: multi-mode 2117 total, 2111 passed, 6 skipped, 0 failed; standalone 0 failed. `ganda repo audit` passed all checks.

### Review

- Rounds: 2. Round 1 roster: general (claude-opus-5-5), effort 2 (by-diff, 240 lines). Round 2 roster: general (grok-4.7), effort 1 (by-diff, 0 lines).
- Final counts (round 2): bug 0, suggestion 0, nit 1 (0 open, 0 fixed, 1 wontfix).
- Disposition: **accepted-exceptions**. M1 (nit) stays wontfix: the two-space indent on the rewritten `<param>` doc lines in `route-definition.cs` is whitespace only, and the build and format accept it. Fix it in passing. Round 2 found no new issues. The 518 product files are unchanged since `a58b48fc`.
- Round 1 re-ran these after `ganda runfile cache --clear`: `mcphost-04` 6/6, `mcphost-01` 16/16, and the calculator `--capabilities` (`openWorld` false, `title` "Total numbers"). Round 2 did not re-run them; those files have the same bytes.
- The restatement matches the task Goal. No misread. Round 2 checked again.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done
- Round 2: Done
## Notes

- timewarp-architecture 391 Design regions: `source/foundation/timewarp-contracts/destructive-attribute.cs`,
  `open-world-attribute.cs`, `operation-attribute.cs`, and the package readme's "Tool hints" section.
- Why separate attributes and not `[Operation]` named bools: a generator can see an omitted named argument,
  but reflection fills an omitted bool property with false. Attribute presence works for both readers.
