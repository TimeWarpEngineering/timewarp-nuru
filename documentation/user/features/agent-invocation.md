# Agent invocation

An agent discovers a Nuru CLI with `--capabilities`, then invokes a route with `--json-args`. Route literals stay on argv. Every parameter and option value may come from one JSON object. Keys are the `name` strings on `parameters[]` and `options[]`. There is no second schema.

```bash
myapp --capabilities
myapp apply_patch --json-args - <<'EOF'
{
  "solution": "/abs/path/to.sln",
  "patch": "--- a/Foo.cs\n+++ b/Foo.cs\n@@\n-old\n+new\n",
  "check-only": true
}
EOF
```

The same object can come from a file:

```bash
myapp apply_patch --json-args @./apply-patch.json
```

## Capabilities

`--capabilities` prints a `CapabilitiesResponse`. The optional `invocation` object names the transport. Every generated app emits it. An endpoint with no parameters accepts `--json-args '{}'` and rejects unknown keys.

```json
"invocation": {
  "jsonArgs": "--json-args",
  "stdin": "-",
  "filePrefix": "@",
  "merge": "argvOverridesJson",
  "unknownKeys": "error"
}
```

`invocation` is nullable. A document that omits it still deserializes, and `Invocation` is null. A reader that does not declare the property ignores the JSON field. `TimeWarp.Nuru.Search` indexes `endpoints` and leaves `invocation` in the stored capabilities document. It does not copy the object onto search results.

`parameters[]` and `options[]` remain the schema. `--capabilities` does not emit a JSON Schema.

## `--json-args`

`--json-args` takes one value. There is no short alias. The value is a separate argv token.

| Value | Meaning |
|-------|---------|
| `-` | Read stdin to EOF as UTF-8 JSON. A pipe and a heredoc both use this. |
| `@path` | Read that filesystem path as UTF-8 JSON. `~/` may be expanded. |
| `{...}` | Inline JSON object. Use this for tests and tiny calls. It still counts against `ARG_MAX`. |

Any other value exits 1. Attached forms (`--json-args=`, `--json-args:`, `/json-args=`, `/json-args:`) exit 1. The `@` prefix is special only as the value of `--json-args`.

Inside the REPL, `-` exits 1. `@path` and a quoted inline object work there.

## Exact names

Copy each key from the catalog `name`. Comparison is ordinal. An `alias` does not match a JSON key. Authors who want `check-only` or `checkOnly` declare that long form. Agents copy `name`.

## Merge

Argv overrides JSON for the same `name`. A key only in JSON is used. A key only in argv is used. When both set a repeated option, the argv list replaces the JSON array. Absent keys leave the argv value or the default. JSON `null` is a type error for a non-nullable value and clears a nullable reference.

`bool` values are JSON booleans. Numbers are JSON numbers. Enums and other strings are JSON strings. A repeated option is a JSON array. A catch-all is an array of strings.

## App-level fat field

An app that does not emit `invocation` can still take one large field in the handler. The handler interprets the option value. Document the sentinel in that option's description so `--capabilities` shows it. Only one such option can use `-` per process. Each app defines its own errors and merge rules.

```csharp
[Option("patch", Description = "Unified diff; '-' reads stdin, '@path' reads a file")]
public string Patch { get; set; } = "";
```

The handler reads stdin when `Patch` is `-`, the path after `@`, or the literal. Callers pipe or use a heredoc. This is an application convention. The Nuru contract is `--json-args`.

## Related documentation

- **[Built-in Routes](built-in-routes.md)** - `--capabilities` and `--json-args` beside the other built-ins
- **[Auto-Help](auto-help.md)** - root help lists `--json-args`; per-route help says values may come from it
