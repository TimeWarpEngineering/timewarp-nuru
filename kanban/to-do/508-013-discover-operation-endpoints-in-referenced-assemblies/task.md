# Discover [Operation] endpoints in referenced assemblies

## Description

Filed from 508-006 (Roslynk CLI finding 6).

Endpoints are discovered only in the compiling project's syntax. So Roslynk compiles its operation files into its CLI as linked sources.

Discovering `[Operation]` request types in referenced assemblies (metadata symbols) would let a CLI reference a contracts assembly instead.

Concerns to cover:

- Property initializer defaults are not visible from metadata (no syntax).
- Nested handler visibility.
- The JSON context MSBuild task (`source/timewarp-nuru-build/GenerateNuruJsonContextTask.cs`) parses only source files.
- Opt-in vs automatic discovery (performance, surprise routes).

## Goal (implementer restatement)

A CLI should be able to reference a contracts assembly (a compiled DLL, not linked source files)
and get Nuru endpoints for the `[Operation]` request types in it. Today the generator only finds
endpoint types declared in the compiling project's own syntax.

Acceptance criteria:

- An explicit opt-in exists and is documented. It covers two shapes:
  - `Map<T>()` where `T` lives in a referenced assembly. That app gets the endpoint, and no other app does.
  - An assembly attribute that names a contracts assembly with `typeof(...)`. It adds every
    accessible `[Operation]` / `[NuruRoute]` type in that assembly to the discovery pool, so
    `DiscoverEndpoints()`, `DiscoverEndpoints(typeof(Group))`, and `Map<T>()` all see them.
  - Without either opt-in, nothing changes. A referenced assembly adds no routes on its own, so
    there are no surprise routes.
- Bindings, the handler, the response shape, groups, and hint attributes come from metadata
  symbols through the same extractor code that source types use.
- Defaults: metadata has no initializer syntax. A `[DefaultValue]` attribute or a matching
  constructor-parameter default is used when present. Otherwise the default is "unknown", the
  object initializer leaves the member out, and the contract's compiled initializer runs.
  Capabilities show no default for it. `required` and nullability still come from metadata.
- An inaccessible member or type (a non-public nested `Handler`, an internal type or constructor
  in another assembly) is never emitted into the CLI.
- The JSON context MSBuild task also registers JSON types for metadata endpoints, so AOT
  serialization covers them.
- Generator tests compile a real referenced library and run the generator against it. Docs
  describe the opt-in and the defaults rule.

## Checklist

- [x] Decide the opt-in shape: `[assembly: NuruEndpointAssembly(typeof(X))]` for a whole assembly, plus `Map<T>()` on a referenced type for one endpoint in one app
- [x] Extract bindings from metadata symbols (the extractor now runs on `INamedTypeSymbol`; the syntax overload delegates)
- [x] Defaults strategy when no initializer syntax is available
- [x] JSON context coverage
- [x] Tests
- [x] Docs

## Session

- Created: 4119648 (2026-10-19)
- 2026-10-21 implementer-claude (headless `ganda task work`): implemented the opt-ins, the metadata
  extraction, the defaults rule, the JSON task coverage, tests, docs, and sample 17.
- 2026-10-21: the first CI run had 1 environmental failure, `packable-projects-02-derivation-fixture`.
  The cause was a stale `/tmp/Directory.Build.props` and a dangling `/tmp/Directory.Packages.props`
  symlink left by the 508-007 session (they pointed at its garbage-collected worktree). Any MSBuild
  fixture under /tmp imported them. I removed both, and the test passed. No repo change was needed.
- `ganda repo audit`: 30/30 pass. Nothing to fix.

## Notes

Today's workaround is linked sources in the CLI project.

## Results

### What changed

- **Opt-in** (`source/timewarp-nuru/attributes/nuru-endpoint-assembly-attribute.cs`):
  `[assembly: NuruEndpointAssembly(typeof(AnyTypeInTheAssembly))]`, AllowMultiple. It adds every
  public `[Operation]` / `[NuruRoute]` class or record class in that assembly to the discovery
  pool, like a source type. `Map<T>()` on a referenced endpoint type works without the attribute.
  The route is marked `RouteDefinition.IsExplicitOnly`, so only the mapping app gets it, and
  `DiscoverEndpoints()` in another app skips it. Without an opt-in, nothing changes.
- **Locator** (`generators/locators/referenced-endpoint-locator.cs`): the generator and the JSON
  task share it. It skips types declared in the compilation itself, and types the compilation cannot
  access. `Map<T>()` of a type whose assembly is already named by the attribute is not extracted twice.
- **Generator** (`nuru-generator.cs`): two new providers. `ForAttributeWithMetadataName` on the
  assembly attribute (a CompilationUnit target), and a `Map<T>()` syntax provider. Their results are
  appended after the source endpoints, so `DistinctBy` keeps the discoverable copy of a type found
  both ways. A diagnostic points at the opt-in site, because metadata has no attribute syntax.
- **Extractor**: `EndpointExtractor.Extract(INamedTypeSymbol, Compilation, Location, ct)` is the
  core, and the syntax overload calls `GetDeclaredSymbol` once and delegates. Every helper now takes
  the symbol. Behavior for source types is unchanged: the CI suite is green.
- **Visibility**: for a referenced assembly, a nested `Handler` and the constructors count only when
  `Compilation.IsSymbolAccessibleWithin` allows them. An `internal` nested Handler is ignored, which
  gives NURU_H008 and the mediator path, never CS0122 in the generated code.
- **Defaults**: a property with no syntax in this compilation returns `default!` (the existing
  "cannot copy" sentinel). The object initializer leaves it out until the caller supplies a value,
  so the compiled initializer runs. `[DefaultValue]` sets the capabilities display only. A
  `[DefaultValue(true)]` bool is a value option, not a flag. A positional record's parameter default
  is real metadata and is copied. `required` and nullability come from metadata, so a non-nullable
  member without `required` is optional. This also fixes a latent crash path: syntax from another
  compilation (an IDE project reference) is no longer handed to `GetSemanticModel`.
- **JSON task**: `GenerateNuruJsonContextTask` also extracts referenced endpoints, with the same two
  opt-ins.
- **Docs**: `documentation/user/features/neutral-contracts.md` has a new section, "Contracts in a
  referenced assembly". It also records that the contracts assembly needs
  `[assembly: TimeWarp.Mediator.MediatorAssembly]` for the CLI's generated mediator to dispatch to
  its handlers. The end-to-end sample found that requirement. Also updated: the changelog, the
  samples readme, and the tw-nuru skill.
- **Sample**: `samples/endpoints/17-referenced-contracts/`. `contracts/` is a library that depends
  only on TimeWarp.Contracts and TimeWarp.Mediator.Contracts, and `referenced-contracts.cs` is the CLI.

### Tests

- `generator-59-referenced-operation-endpoints.cs` (7 tests). It emits a real contracts DLL and
  generates against it: `Map<T>()` extraction, initializer-preserving defaults with
  `[DefaultValue]` capabilities, the record parameter default, the inaccessible nested handler
  (NURU_H008 and no `.Handler` reference), assembly-attribute discovery (internal and
  non-mediator types excluded), no discovery without the opt-in, and a mapped endpoint kept out of
  another app's discovery. Each case also checks that the generated code compiles. Mutation check:
  setting `isExplicitOnly: false` fails the last test.
- `generator-60-json-context-referenced-endpoints.cs` (3 tests) runs the MSBuild task in-process
  against a DLL on disk.
- Both are CI multi-mode excludes and are listed as standalone files in `run-ci-tests.cs`.
- `dotnet run tests/ci-tests/run-ci-tests.cs`: exit 0. Multi-mode 2056 tests; standalone 34 files, 0 failed.
- `dotnet build timewarp-nuru.slnx`: 0 warnings, 0 errors.

### Limits

- Metadata cannot show a non-`required` member's initializer. Such a member is optional, and
  capabilities show no default unless `[DefaultValue]` is present. A plain bool flag shows no
  `false` default.
- An app that calls both `DiscoverEndpoints()` and `Map<T>()` keeps the existing rule: the
  `Map<T>()` filter wins. This is unchanged from source endpoints.
- Samples do not import the JSON task, so the sample's JSON output does not go through the
  generated context. gen-60 covers the task.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run samples/endpoints/17-referenced-contracts/referenced-contracts.cs -- greet --Name Ada
dotnet run samples/endpoints/17-referenced-contracts/referenced-contracts.cs -- greet --Name Ada --Greeting Hi --Shout
dotnet run samples/endpoints/17-referenced-contracts/referenced-contracts.cs -- sum --Values 2 --Values 3
dotnet run samples/endpoints/17-referenced-contracts/referenced-contracts.cs -- --capabilities
(cd tests/timewarp-nuru-tests/generator && dotnet run generator-59-referenced-operation-endpoints.cs && dotnet run generator-60-json-context-referenced-endpoints.cs)
```

Expect:

- `Hello, Ada!` (the compiled `Greeting` initializer runs when the option is omitted).
- `HI, ADA!`.
- `{"count":2,"total":5}`.
- `--capabilities` lists `greet` and `sum`, which are declared only in `contracts/`. `Name` has
  `"required": true`, and `Greeting` has `"defaultValue": "Hello"`.
- generator-59: 7/7 pass. generator-60: 3/3 pass.
- Remove `[assembly: NuruEndpointAssembly(...)]` from `referenced-contracts.cs`, and `greet`
  becomes `Unknown command`.
