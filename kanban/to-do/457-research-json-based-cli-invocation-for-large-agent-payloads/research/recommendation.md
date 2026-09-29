# Recommendation: `--json-args` for large agent payloads

**Decision:** Build framework support. The canonical agent invocation is **option B** (`--json-args`). Human argv stays. A file is never required. Do not build option C (`--invoke-json`). Option A (one fat field) and option D (app-only convention) are interim or later convenience, not the contract.

No product code was changed for this research. Feasibility is clear from the current source generator; a prototype is not required.

## Canonical shape

Route literals stay on argv. Every parameter and option value may be supplied from one JSON object. Keys are the `name` strings already emitted on `parameters[]` and `options[]` in `--capabilities`. There is no second schema.

```bash
roslynk --capabilities
roslynk apply_patch --json-args - <<'EOF'
{
  "solution": "/abs/path/to.sln",
  "patch": "--- a/Foo.cs\n+++ b/Foo.cs\n@@\n-old\n+new\n",
  "check-only": true
}
EOF
```

The same object can come from a file. The file is optional:

```bash
roslynk apply_patch --json-args @./apply-patch.json
```

A small object may also be an argv token (tests and tiny calls only; this form still counts against `ARG_MAX`):

```bash
roslynk greet --json-args '{"name":"Ada"}'
```

`--json-args` takes exactly one value. There is no short alias (do not reserve `-j`).

| Value | Meaning |
|-------|---------|
| `-` | Read stdin to EOF as UTF-8 JSON. Pipe and heredoc are both this. |
| `@path` | Read that filesystem path as UTF-8 JSON. `~/` may be expanded. No `file://`, no other URI scheme. |
| `{...}` | Inline JSON object (a separate argv token, not `--json-args=`). |
| anything else | Exit 1. |

The `@` prefix is special only as the value of `--json-args`. Other options are unchanged, so a legitimate value that starts with `@` is left alone.

## Why this shape

The large field is the problem. The route name is not. `apply_patch --json-args -` is a few dozen bytes on argv; the unified diff rides on stdin, so neither Linux `MAX_ARG_STRLEN` nor Windows `CreateProcess` nor nested shell quoting applies to the payload.

Putting the endpoint name inside the body (option C) would add a second router next to the generated matcher (literals, aliases, groups, optional positionals, catch-all) and would hide the command from the process list. MCP `tools/call` symmetry is not worth that. Discovery is already `--capabilities`. Invoke should reuse the matcher, not replace it.

One stdin can feed only one body. A per-option `--patch -` (option A) cannot fill `patch` and another large field in the same call. `--json-args` is that same stdin convention, applied once to the whole argument object, so several large strings travel together.

## What agents should do today

Nuru does not read a JSON argument object. Nothing in the framework treats `-` or `@path` as a payload. `TimeWarp.Nuru.Search` only runs the target with `--capabilities` (`source/timewarp-nuru-search/services/capabilities-client.cs`). Until the follow-up ships, these are the workarounds:

1. **Keep the value on argv** only when it is small and quoting is trivial. This fails for multi-KB diffs.
2. **App-level fat field** (option D, valid interim for Roslynk, not the recommended long-term contract). The handler, not Nuru, interprets the option value:

   ```csharp
   [Option("patch", Description = "Unified diff; '-' reads stdin, '@path' reads a file")]
   public string Patch { get; set; } = "";
   ```

   The handler reads stdin when `Patch` is `-`, or the path after `@`, or uses the literal. Callers pipe or use a heredoc. They do not have to write a temp file. Only one such option can use `-` per process. Document the sentinel in that option's description so `--capabilities` shows it. Each app invents its own errors and merge rules; `ganda` and `dev` will not match Roslynk.
3. **Call the warm HTTP daemon directly** when the app already has one. That bypasses the CLI contract.
4. **MCP `tools/call`** where the host already has an MCP session. That is host integration, not a Nuru feature. `TimeWarp.Nuru.Mcp` is an authoring server (route validation, handler snippets, syntax help). It does not expose an application's endpoints as tools.

Do not require a temp file in any of these paths.

## OS limits this removes

| Limit | Practical effect |
|-------|------------------|
| Linux `MAX_ARG_STRLEN` (`PAGE_SIZE * 32`, 131072 bytes per argument on 4K pages) | A single argv string larger than this fails `execve` even when total `ARG_MAX` is higher. |
| Linux `ARG_MAX` (often 2 MiB via `getconf ARG_MAX`, also capped by stack) | Total argv + env. Nested agent shells hit it sooner. |
| Windows `CreateProcess` command line | 32767 characters. |
| Shell quoting | Agent host → shell → CLI re-escapes quotes, backticks, and newlines. A payload can be unquotable before it is too long. |

Stdin and `@path` avoid all four. Inline JSON does not.

## Options compared

| | A. Fat field (`--patch -`) | B. `--json-args` (chosen) | C. `--invoke-json` | D. App-only |
|--|--|--|--|--|
| Who routes | Existing matcher | Existing matcher | A second dispatcher | The app |
| Large fields per call | One (only one stdin) | All of them, in one object | All of them | One, if that app bothers |
| Names | Whatever the app documents | Capability `name` | Capability `name`, plus an endpoint string | App-specific |
| Argv for humans | Unchanged | Unchanged | Unchanged | Unchanged |
| AOT | App code, or a small shared helper | Generated binder, no reflection | Generated second matcher | App code |
| Every Nuru CLI | Only if the framework grows A later | Yes | Yes, at higher cost | No |
| Verdict | Good human shortcut later. Conflicts with B if both read stdin. | Canonical agent contract | Reject. Reinvents routing and looks like a JSON-RPC server. | Interim for Roslynk only |

## Match and merge

`--json-args` and its value are peeled off the original `args` **before** route matching and **before** the config-arg filter.

That filter (`IsConfigArg` in `interceptor-emitter.cs`) treats `--key=value` and `--key:value` as configuration overrides and drops them from `routeArgs`. `--json-args=-` and `--json-args={...}` would vanish and the command would run with no payload. **The `=` form is an error**, not a synonym: tell the caller to use a separate token (`--json-args -`, `--json-args @path`, or `--json-args '{...}'`).

Matching then uses the remaining argv plus the already-parsed object:

1. A route is a candidate only when its literal segments match argv in order (including group prefixes and existing aliases).
2. A parameter slot may be the next argv token, or it may be omitted from argv when the JSON object contains that parameter's `name`.
3. Every JSON key must be a parameter or option `name` on that route. A short `alias` is not a JSON key. Unknown keys eliminate the candidate.
4. After argv ∪ JSON, every required parameter and required option is present.
5. Zero candidates → exit 1. If the literals matched one route and the only problem is an unknown key, name the key, the pattern, and the known names.
6. Several candidates → prefer the route with more matched literals. If still tied, exit 1 and list the patterns. Do not guess.

This is what makes `apply_patch --json-args -` work when `solution` and `patch` are not on argv, and what stops a zero-parameter `deploy` from stealing `{"env":"prod"}` that belongs to `deploy {env}`.

**Merge: argv overrides JSON** for the same `name`. A key only in JSON is used. A key only in argv is used. When both set a repeated option, the argv list replaces the JSON array (no element-wise merge). This matches AWS CLI `--cli-input-json` (command line wins) and lets a human flip one flag while the bulk stays in the pipe.

Absent keys leave the argv value or the existing default. JSON `null` is a type error for non-nullable values and clears a nullable reference.

Duplicate JSON keys: last one wins (System.Text.Json).

## Errors and exit codes

Failures happen before the handler. They use `ITerminal.WriteErrorLineAsync` and **exit 1**, the same code the matcher already returns for an invalid value or an unknown command (`route-matcher-emitter.cs`, `EmitNoMatch`). Handlers keep using `Environment.ExitCode` for their own failures. Stdout stays available for successful command output.

| Condition | Result |
|-----------|--------|
| Malformed JSON, JSON array, or JSON primitive | Exit 1, parse error, do not include the body |
| `--json-args` missing its value, duplicated, or using `=` | Exit 1 |
| `-` and stdin is a TTY (not redirected) | Exit 1. Do not block. |
| `-` and stdin is empty or whitespace | Exit 1. A forgotten pipe is not `{}`. An explicit `{}` is valid and adds no keys. |
| `@path` missing, unreadable, or a URI | Exit 1 |
| Unknown key | Exit 1, name the key and the known names |
| JSON type does not match the capability `type` | Exit 1, name the key and the expected type |
| Required value still missing after merge | Exit 1, same idea as today's missing-parameter error |
| `--help` / `-h` also present | Help wins. Do not read stdin. |

Error text names keys and types. It does not echo multi-KB string values (patches contain source).

## JSON types

Parse with `JsonDocument` (AOT-safe, no per-route DTO). Do **not** run the object through `CapabilitiesJsonSerializerContext`: that context applies camelCase, and capability names are already the wire names (`check-only`, `checkonly`, `solutionId` — whatever the catalog emitted). Match keys with ordinal comparison.

| Capability | JSON |
|------------|------|
| `string` and other string-converted scalars (`guid`, `datetime`, `timespan`, `uri`, …) | string, then the existing `TypeConversionMap` / `EnumTypeConverter` parse |
| `bool` (flags and bool options) | boolean, not `"true"` |
| `int`, `long`, `double`, and the other numeric constraints | number |
| enum (`AllowedValues`) | string. Accept the `AllowedValues` spelling; compare the way `EnumTypeConverter` already compares |
| repeated option (`IsRepeated`) | array of the element type. Presence of the key replaces the list |
| catch-all (`IsCatchAll`) | array of strings |

Do not accept a numeric string for an int, or a string `"true"` for a bool. Agents have the catalog and can emit real JSON types. Strict mismatches surface schema drift instead of a second quoting language.

## Capabilities surface

Always on, for every generated app, not per endpoint. An endpoint with no parameters accepts `--json-args '{}'` and rejects unknown keys.

Add an optional top-level object so an agent that only reads `--capabilities` learns the transport. Suggested shape (camelCase, same serializer policy as the rest of the document):

```json
"invocation": {
  "jsonArgs": "--json-args",
  "stdin": "-",
  "filePrefix": "@",
  "merge": "argvOverridesJson",
  "unknownKeys": "error"
}
```

`CapabilitiesJsonSerializerContext` uses `WhenWritingNull` and System.Text.Json ignores unknown properties, so older readers of `CapabilitiesResponse` keep working. The new property must not be C# `required`. `TimeWarp.Nuru.Search` copies a fixed set of fields and will ignore it until that package is updated in the same change. In-repo tests that assert the full document need updating; `ShouldContain` checks do not.

Do not emit a JSON Schema. `parameters[]` / `options[]` remain the schema. Root `--help` should list `--json-args` next to `--capabilities` (`help-emitter.cs`). Per-route help can add one line: values may come from `--json-args`.

Reserve the flag beside the other built-ins in `BuiltInFlags`. An analyzer diagnostic should fire if a user option's long form is `json-args`, the same way user routes must not take `--help` or `--capabilities`.

## AOT and source generation

Binding is already compile-time. `ParameterBinding` records `Source` (`Parameter`, `Option`, `Flag`, `CatchAll`), `SourceName`, and the CLR type. The matcher writes generated locals and command properties; delegate routes get `__Route_N_Command` from `command-class-emitter.cs`. Endpoint properties are the lowercased name or the option long form (`endpoint-extractor.cs`).

The follow-up should emit, per route, a `JsonDocument` walk that assigns those same locals and properties, then reuse `TypeConversionMap` and `EnumTypeConverter`. No `Type.GetProperty`, no reflection-based `JsonSerializer` of the command type. Strings come from `GetString()` so they are not argv-sized. Services and `CancellationToken` are not JSON keys.

Behaviors and the handler run after binding, as they do today. They see the command instance, not the raw object. No behavior API change.

## REPL, help, pipeline

`AutoStartWhenEmpty` runs the REPL only when `routeArgs.Length == 0` (`interceptor-emitter.cs`). `apply_patch --json-args -` has arguments, so the REPL does not start and stdin remains the payload. That interaction is safe.

Inside the REPL, the input line is stdin (`repl-session.cs` → `CommandLineParser`). `--json-args -` there would consume the session. **Reject `-` in the REPL** with exit 1 and tell the caller to use `@path` or a single-line `'{"…"}'`. `@path` and quoted inline JSON work because `CommandLineParser` already keeps quoted strings. v1 does not add a multiline REPL paste mode.

`--capabilities`, `--version`, `--help`, and the other built-ins are unchanged. `--json-args` is not a command and does not appear in the endpoint list except via the `invocation` object and help text.

## Stdin on Unix and Windows

Read `Console.OpenStandardInput()` with a UTF-8 `StreamReader` (honor a BOM if present). Do not use the ambient `Console.In` encoding. The body is text JSON. Raw binary is out of scope; a caller who must send bytes base64-encodes a string field.

`Console.IsInputRedirected` is true for a pipe and for `< file` on both Windows and Unix. If it is false and the value is `-`, fail immediately.

The framework reads stdin to EOF before the handler. A handler that also reads `Console.In` sees EOF. Apps that already use stdin for their own protocol should not also use `--json-args -`; `@path` is the escape hatch. There is no framework size cap. Process memory is the limit. Do not truncate.

## Security

The CLI already runs as the user. This adds no socket, no bind address, and no long-lived reader on stdin. `@path` is a local path the user can already open. The JSON is data bound to declared keys, not evaluated. Do not log the body.

Reject URI values so `@https://…` cannot become a fetch. That would be a new network surface.

## MCP, and task 142

| MCP | Nuru after this |
|-----|-----------------|
| `tools/list` | `--capabilities` (already) |
| `tools/call` `{ name, arguments }` | argv route + `--json-args` object, same property names |
| Host tool panel, `mcp add`, per-tool approval | Still host-integration-only |
| Process stays warm inside the host | Still out of scope. One CLI process per call. A daemon behind the CLI keeps warm state. |

What this closes for shell-native agents: large arguments, and a discovery-plus-invoke pair that does not depend on MCP.

What it leaves open: IDE tool panels, approval without a shell, and an in-host session.

Task **142** (WASI / MCP / capabilities vocabulary) is archived. It was about sandbox rights and a shared descriptor, and the board note says it targeted a frozen MCP server. `EndpointKind` (`query`, `command`, `idempotentCommand`) already carries the safety bit agents need. This transport does not reopen 142. The two are complementary: 142 is permissions vocabulary; this is how a large argument crosses the process boundary.

## Non-goals

- Replacing argv for short human commands.
- A JSON-RPC / MCP-stdio session that reads many messages from stdin.
- A new or redesigned `--capabilities` catalog.
- Per-endpoint JSON Schema files.
- Binary stdin, or framework-level `--patch -` in the same release as `--json-args` (one stdin; do both and they collide).
- Renaming existing option long forms. Authors who want `check-only` or `checkOnly` declare that long form. Agents copy `name`; they do not camelCase the C# property.

## Grounding in this repo

- Catalog types: `source/timewarp-nuru/capabilities/capabilities-response.cs`. Serializer is source-generated and camelCase: `capabilities-json-serializer-context.cs`.
- Endpoint option `name` is the `[Option]` long form, or `propertyName.ToLowerInvariant()` when omitted. Parameter `name` emitted for endpoint classes is `property.Name.ToLowerInvariant()` (`endpoint-extractor.cs`). `ParameterAttribute.Name` is documented as camelCase and is not what the extractor writes today. That drift is pre-existing and out of scope. The wire contract is the emitted `name`.
- Fluent `{name}` segments keep the spelling from the pattern, which may be camelCase. Again, copy the catalog.
- Built-ins live in `built-in-flags.cs` and are special-cased in `interceptor-emitter.cs` before user routes.
- Parse failures already return 1. `RunAsync` does not treat the handler's return value as the exit code (`nuru-app.cs`).

## Open questions

These have recommended answers; the implementer can deviate only with a reason recorded on that task.

- **No framework body cap.** Apps validate if they need a limit.
- **No short alias** for `--json-args`.
- **Per-option `-` / `@path`** as a human convenience after B exists. Reject the combination when both would read stdin. Not part of the first implementation.
- **`~/` expansion** on `@path`: yes. Symlinks: follow them (normal file open).
- **Case folding of JSON keys:** no. Ordinal match to `name`.

## Proposed follow-up tasks

The cockpit files these after review. Do not create them from the research walk.

### 1. Bind `--json-args` in the Nuru source generator

Reserve `--json-args` as a built-in with no short form, and diagnose user options that take that long form. On the original `args` array, before `IsConfigArg`, reject the `=` form and peel a single `-`, `@path`, or inline object. Parse with `JsonDocument`. Extend route selection so literal segments still match argv while parameter slots may be satisfied by JSON keys, using the candidate rules above (argv overrides JSON, unknown keys and type mismatches exit 1, TTY or empty stdin with `-` exits 1). Assign the same generated locals and command properties the matcher already assigns, via `TypeConversionMap` and `EnumTypeConverter`, with no reflection. Reject `-` inside the REPL; allow `@path` and quoted inline JSON there. Add a root-help row. Tests: merge precedence, unknown key, type mismatch, enum, repeated option, catch-all, `@file`, stdin larger than `MAX_ARG_STRLEN`, empty stdin, TTY stdin, `=` form, REPL rejection, and a route that matches only because required positionals are in the JSON object.

### 2. Advertise invocation on `--capabilities` and document the contract

Add the optional `invocation` object to `CapabilitiesResponse` and emit it from `capabilities-emitter.cs`. Keep the property nullable so existing fixtures and older deserializers still load. Update `TimeWarp.Nuru.Search` only if it should surface the object; ignoring unknown JSON is already safe. Refresh capabilities round-trip tests and any full-document assertions. Document the agent loop (capabilities, then `--json-args -` / `@path`), the exact-name rule, argv-overrides-JSON, and the app-level fat-field interim for apps that cannot wait. Point the Nuru skill's `--capabilities` section at that contract.
