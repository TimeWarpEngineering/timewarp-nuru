# Operation endpoints with camelCase properties: bind by declared identifier, plus the Roslynk CLI findings

## Description

Roslynk task 003 (fork issue TimeWarpEngineering/Roslynk#4) builds a Nuru 4.0.0-beta.1 CLI over the 29
`[Operation]` request classes from Roslynk 002. Those request properties are camelCase on purpose: they are
the MCP parameter names (`solutionId`, `symbolName`, `checkOnly`, ...), and
`documentation/user/features/neutral-contracts.md` says the option name is "the property identifier as
declared". Option names do come out as declared. The generated request construction does not compile,
though. Roslynk 003 is **blocked** on this task and needs a release (4.0.0-beta.2 or later).

### Blocking defect: the request initializer re-spells the property

The extractors store the binding name lowercased (`ParameterName: property.Name.ToLowerInvariant()` in
`generators/extractors/operation-endpoint-extractor.cs` and `endpoint-extractor.cs`), and
`generators/emitters/command-construction-emitter.cs` rebuilds the member name with `ToPascalCase(...)`.
For a camelCase property the result is a member that does not exist:

```csharp
// generated for: public required string solutionId { get; set; }  public required string symbolName { get; set; }
global::Morris.Roslynk.Features.Symbols.GetSymbol.GetSymbol.Query __command = new()
{
  SolutionId = solutionid,   // CS0117: 'Query' does not contain a definition for 'SolutionId'
  SymbolName = symbolname,   // CS9035: required member 'symbolName' must be set
};
```

Roslynk's build: 834 CS0117 and 414 CS9035, across the argv path, the `--json-args` path and the
`TryBind` path (each request is constructed in three places). The same `ToPascalCase` re-spelling exists in
`command-class-emitter.cs` and `behavior-emitter.cs`; check whether those paths can see an `[Operation]`
property too.

Fix: carry the declared property identifier on the binding (a `PropertyName` beside `ParameterName`) and
emit it verbatim (with `@` escaping for keywords) everywhere the generator writes a member access or an
initializer. Keep the lowercase local names if they are only locals.

### Other findings from the same build (please cover or split)

1. **SYSLIB1031 (warning; error under TreatWarningsAsErrors).** `GenerateNuruJsonContextTask` adds
   `[JsonSerializable(typeof(OneOf<X.Response, SharedProblemDetails>))]` for every operation. When several
   operations nest a type with the same simple name (`GetSymbol.Response`, `GetMembers.Response`, ...), STJ
   names them all `OneOfResponseSharedProblemDetails` and reports 58 collisions. Set
   `TypeInfoPropertyName` from the full name, or skip a return type whose success value has an
   `IResponseRenderer<T>` (it is never JSON-serialized).
2. **SYSLIB1034.** A JSON member type annotated `[JsonConverter(typeof(JsonStringEnumConverter))]`
   (Roslynk's `MultiQueryOp`) trips the AOT warning inside `NuruJsonMemberContext`. Either document that
   contracts must use `JsonStringEnumConverter<TEnum>` or suppress it for the generated context.
3. **Rendered output is not verbatim.** A renderer's text is written with `Terminal.WriteLine`, which adds a
   newline, and a `SharedProblemDetails` is written as `"{Type} {Title}"` plus `Detail`. Roslynk needs the
   success text byte-identical to the MCP text and the problem text to be its `error=` outline. Offer a
   verbatim mode (renderer output written with `Write`), and an `IProblemRenderer` (or
   `IResponseRenderer<SharedProblemDetails>`) that replaces the default problem lines while keeping the
   `Status` exit code.
4. **Groups from outside the declaration.** `groupPath` comes only from a base `[NuruRouteGroup]` or a
   `[NuruCliBinding(Group = ...)]` on the request itself, and the group prefix is prepended to the pattern
   (`symbols get_symbol`). A CLI that compiles contracts it does not own (linked sources) cannot group them,
   and grouping would change the route literal away from the MCP tool name. Wanted: a group assignment the
   app can declare (builder call or assembly attribute keyed by request type) that sets `groupPath` for
   `--group-filter` without prefixing the pattern, or `Group` on `TimeWarp.Contracts.CliOperationBinding`.
5. **Nested JSON member names.** `NuruJsonMemberContext` uses declared names, case-sensitive, so
   `multi_query --json-args '{"operations":[{"tool":...}]}'` (the MCP schema's camelCase) silently leaves
   `Tool` at its default; only `{"Tool":...,"Arguments":...}` binds. Consider
   `PropertyNameCaseInsensitive = true` for nested members, or report unmapped members as an error.
6. **Referenced-assembly discovery.** Endpoints are discovered only in the compiling project's syntax, so
   Roslynk compiles its operation files as linked sources. Discovering `[Operation]` types in referenced
   assemblies (metadata) would let the CLI reference a contracts assembly instead.

## Goal (implementer restatement)

Make an `[Operation]` (or `[NuruRoute]`) request whose properties are declared in camelCase
(`solutionId`, `checkOnly`) compile and bind. The generator must write the property identifier exactly as
declared (escaped with `@` for keywords) in every initializer and member assignment it emits for the
request: the argv path, the `--json-args` path and the `TryBind` path. The SYSLIB1031 collision from
`[JsonSerializable(typeof(OneOf<X.Response, SharedProblemDetails>))]` on same-named nested `Response`
types must go away. Findings 2-6 are each either fixed here or filed as their own task (ids in Results).
The release is a later, separate step: the release itself is a human-gated `dev release` after merge, so
this walk notes that and leaves the version for the release.

Acceptance:
- A generator test with a camelCase `required` property, a camelCase defaulted property (settable and
  init-only) and a keyword-named property builds and binds over argv, `--json-args` and `TryBind`.
- Two operations whose responses are both nested `Response` types build without SYSLIB1031.
- `ganda repo audit` is clean; the CI test suite passes.

## Checklist

- [x] Declared property identifier carried on the binding and emitted verbatim (argv, `--json-args`, `TryBind`)
- [x] Generator test: `[Operation]` request with camelCase `required` and defaulted properties builds and binds
- [x] SYSLIB1031 collision fixed for same-named nested `Response` types
- [x] Decide findings 2-6 here or file them as their own tasks (ids in Results)
- [ ] Release a 4.0.0 prerelease carrying the fix; note the version in Roslynk task 003 (version bumped to `4.0.0-beta.2` on this branch; `dev release` and the Roslynk 003 note come after merge)

## Session

- Created: 2568965 (2026-10-18). Filed from the Roslynk 003 implementer walk (2026-10-19), which stops as
  Blocked until a Nuru release carries the camelCase fix.
- 2026-10-19 implementer (claude, ganda task work): the binding already held the declared identifier
  (`ParameterBinding.ParameterName = property.Name`); only `CommandConstructionEmitter` re-spelled it with
  `ToPascalCase`. `command-class-emitter.cs` and the delegate branch of `behavior-emitter.cs` name members
  of the generated `__NuruCommand_N` class on both sides, so they never see an `[Operation]` property and
  were left alone. Removed the unused `ToPascalCase` in `handler-invoker-emitter.cs`.
- Boy-scout audit fix: the new runfile test lacked the executable bit (`runfile-executable`); set it.
- MSBuild node reuse kept a stale `TimeWarp.Nuru.Build.dll` loaded between runs. After changing the task,
  run `dotnet build-server shutdown` before re-testing.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-19T04:04:45Z
- Review oracle: reviewed by grok (model grok-4.7), session `01a15255-516b-7a92-8d0d-869c54c346d5`, effort 2, roster general, round 1, clean — 2026-10-19
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 120 — 2026-10-19T04:17:33Z

## Results

### What changed

- **Blocking defect (camelCase members).** `generators/emitters/command-construction-emitter.cs` writes each
  request member as declared, `@`-escaped for keywords, instead of `ToPascalCase(...)`. Every request
  construction (argv, `--json-args`, `TryBind`, and the behavior path) goes through this emitter, so one
  change covers all of them. Locals stay lowercase because the route matcher declares them that way.
- **SYSLIB1031.** `source/timewarp-nuru-build/GenerateNuruJsonContextTask.cs` registers a problem pair's
  success type instead of `OneOf<...>`, because only `AsT0` is ever serialized. It registers nothing when the
  endpoint has a renderer. New `json-context-type-names.cs` resolves each registered type to a symbol, walks
  the public member graph, groups types by the STJ default metadata name, and gives each type in a colliding
  group a `TypeInfoPropertyName` built from its full name. A reached type that collides gets its own
  registration. Types that do not collide keep the default name.
- **Finding 2 (SYSLIB1034):** documented. `neutral-contracts.md` says to use
  `JsonStringEnumConverter<TEnum>` inside a JSON value. A generated suppression is not possible:
  `#pragma warning disable` in the generated file does not silence STJ generator diagnostics (seen here with
  SYSLIB1031).
- **Finding 5 (nested JSON names):** fixed here. `NuruJsonMemberContext` gets
  `PropertyNameCaseInsensitive = true`, so `{"operations":[{"tool":...}]}` binds `Tool`. Top-level
  `--json-args` keys still match exactly.
- **Findings 3, 4, 6:** filed as their own tasks:
  - 508-011 Verbatim renderer output and a problem renderer for operation endpoints (finding 3)
  - 508-012 Assign operation groups from the app without prefixing the route (finding 4)
  - 508-013 Discover [Operation] endpoints in referenced assemblies (finding 6)
- **Version:** `4.0.0-beta.2` in `source/Directory.Build.props`; changelog Fixed entries under Unreleased.
  After merge, cut it with `dev release`, then set Roslynk 003's Nuru reference to `4.0.0-beta.2`.

### Tests

- New `tests/timewarp-nuru-tests/auto/operation-camelcase-members.cs` (6 tests): camelCase `required`, a
  settable default, an init-only default the generator cannot copy (`T5086Seed.Next()`), a flag and a
  keyword member (`@class`), bound over argv, omitted, `--json-args` and `TryBind`/`InvokeAsync`. Two
  operations nesting `Response`, `Item` and `List<Item>` build with no SYSLIB1031 and serialize. Nested JSON
  member names in camelCase and PascalCase both bind.
- Before the fix, the same file failed to build with SYSLIB1031 for `OneOfResponseSharedProblemDetails`
  and `Response`, and then for `ListItem` and `Item`.
- `dotnet run tests/ci-tests/run-ci-tests.cs` (cache cleared): multi-mode 2002 total, 1996 passed,
  6 skipped, 0 failed; standalone phase 0 failed.
- `ganda repo audit`: passes.

### How to validate

Smoke:

```bash
dotnet build-server shutdown
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/auto/operation-camelcase-members.cs
```

Expect: `Passed: 6`, and no CS0117, CS9035 or SYSLIB1031 in the build output. The generated
`obj/debug/NuruUserTypesJsonContext.g.cs` under the runfile cache lists both `Response` types, both `Item`
types and both `List<Item>` types with `TypeInfoPropertyName = "TimeWarp_Nuru_Tests_Auto_CamelCase_..."`.

Full suite: `dotnet run tests/ci-tests/run-ci-tests.cs` reports 0 failed.

Roslynk repro (after `4.0.0-beta.2` ships): on Roslynk branch `task/003-nuru-cli-over-the-typed-operations-roslynk-command`,
`dotnet build Source/App/Morris.Roslynk.Cli` has no CS0117, CS9035 or SYSLIB1031.

### Review

Round 1 (effort 2, roster general) is in `review/round-1/merged.md`. No findings. Counts: bug 0, suggestion 0, nit 0 (open/fixed/wontfix all 0). Disposition: **clean** (`review/disposition.md`). Final open count: 0. Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

Re-ran `dotnet build-server shutdown`, `ganda runfile cache --clear`, and `dotnet run tests/timewarp-nuru-tests/auto/operation-camelcase-members.cs`: Total 6, Passed 6. Generated `NuruGenerated.g.cs` assigns `solutionId = solutionid` and `@class = @class`. `NuruUserTypesJsonContext.GetJsonTypeInfo` names both `Response` types, both `Item` types, and both `List<Item>` types from their full names. `NuruJsonMemberContext` sets `PropertyNameCaseInsensitive = true`.

## Learnings

Misread: `## Goal (implementer restatement)` says this walk leaves the version for a later human-gated `dev release`. The original checklist still says to release a 4.0.0 prerelease and note the version in Roslynk task 003. Task 510, already on this branch, assigns that bump and the cut to itself after 508-006 merges. The version commit on this branch sets `4.0.0-beta.2` and leaves the release item open. When this merges, 510 should treat the version as already `4.0.0-beta.2` and still do trusted publishing, `dev release`, and the Roslynk 003 note.

The task body says the extractors store `ParameterName` lowercased and that the fix is a new `PropertyName` field. `ParameterBinding.ParameterName` is already `property.Name`. `OptionDefinition.ParameterName` is the lowercased route local. The restatement's outcome, emit the declared identifier, matches the emitter change and the acceptance tests.

## Review

- Round 1: Done

## Notes

- Repro: Roslynk branch `task/003-nuru-cli-over-the-typed-operations-roslynk-command`, project
  `Source/App/Morris.Roslynk.Cli` (links `Source/App/Morris.Roslynk/Features/**/*.cs` minus `*Tool.cs`).
  `dotnet build Source/App/Morris.Roslynk.Cli` with `EmitCompilerGeneratedFiles` shows the initializers in
  `obj/Debug/net10.0/generated/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs`.
- Related: 508 (parent), 508-003 (json-args binder), 508-004 (McpHost reads the same group path), 506.
