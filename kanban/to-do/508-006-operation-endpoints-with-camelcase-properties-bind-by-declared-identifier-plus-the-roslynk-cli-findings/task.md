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

- [ ] Declared property identifier carried on the binding and emitted verbatim (argv, `--json-args`, `TryBind`)
- [ ] Generator test: `[Operation]` request with camelCase `required` and defaulted properties builds and binds
- [ ] SYSLIB1031 collision fixed for same-named nested `Response` types
- [ ] Decide findings 2-6 here or file them as their own tasks (ids in Results)
- [ ] Release a 4.0.0 prerelease carrying the fix; note the version in Roslynk task 003

## Session

- Created: 2568965 (2026-10-18). Filed from the Roslynk 003 implementer walk (2026-10-19), which stops as
  Blocked until a Nuru release carries the camelCase fix.

## Notes

- Repro: Roslynk branch `task/003-nuru-cli-over-the-typed-operations-roslynk-command`, project
  `Source/App/Morris.Roslynk.Cli` (links `Source/App/Morris.Roslynk/Features/**/*.cs` minus `*Tool.cs`).
  `dotnet build Source/App/Morris.Roslynk.Cli` with `EmitCompilerGeneratedFiles` shows the initializers in
  `obj/Debug/net10.0/generated/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs`.
- Related: 508 (parent), 508-003 (json-args binder), 508-004 (McpHost reads the same group path), 506.
