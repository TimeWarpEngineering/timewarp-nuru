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

## Where the stopgap lives (from 508-015)

- `source/timewarp-nuru/attributes/destructive-attribute.cs`, `open-world-attribute.cs`
- `source/timewarp-nuru/capabilities/capabilities-response.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs`,
  `operation-endpoint-extractor.cs`, `locators/referenced-endpoint-locator.cs`, `nuru-generator.cs`
- `source/timewarp-nuru-mcphost/nuru-mcp-host-options.cs`, `nuru-mcp-tool-catalog.cs`

## Checklist

- [ ] TimeWarp.Contracts with the hints is published; bump the pin
- [ ] Contract endpoints read Title, Destructive, and OpenWorld from TimeWarp.Contracts
- [ ] Decide route-endpoint hints and the `[DisplayName]` title source; record why
- [ ] Remove Nuru's `[Destructive]` / `[OpenWorld]` (or record why one stays) and update samples and docs
- [ ] Tests: contract with Contracts hints publishes them in `--capabilities` and the MCP tool list; host
      override still wins; a file importing both namespaces compiles
- [ ] Release note: breaking for anyone using `TimeWarp.Nuru.Destructive` / `OpenWorld`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22) from the timewarp-architecture 391 merge review.

## Notes

- timewarp-architecture 391 Design regions: `source/foundation/timewarp-contracts/destructive-attribute.cs`,
  `open-world-attribute.cs`, `operation-attribute.cs`, and the package readme's "Tool hints" section.
- Why separate attributes and not `[Operation]` named bools: a generator can see an omitted named argument,
  but reflection fills an omitted bool property with false. Attribute presence works for both readers.
