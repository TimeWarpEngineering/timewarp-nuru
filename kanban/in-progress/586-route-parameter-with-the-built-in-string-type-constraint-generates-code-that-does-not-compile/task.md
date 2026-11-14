# Route parameter with the built-in string type constraint generates code that does not compile

## Description

`{name:string}` is a built-in type constraint: `source/timewarp-nuru-parsing/parsing/parser/built-in-type-names.cs`
lists `"string"` first, and `documentation/user/reference/supported-types.md` documents
`{name}` or `{name:string}`. The generator does not handle it. `EmitTypeConversions`
(`source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.type-conversions.cs`)
has no `string` branch, so it emits `// WARNING: No converter found for type constraint 'string'`
and never declares the handler variable. The build fails with
`NuruGenerated.g.cs: error CS0103: The name 'message' does not exist in the current context`.

Repro (found by task 532 on master 2026-11-14 and on the 4.0.0-beta.13 package):

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("echo {message:string}")
    .WithHandler((string message) => Console.WriteLine(message))
    .AsCommand()
    .Done()
  .Build();
```

The source says the constraint is valid, so this is a code defect, not a doc defect. The likely
fix is to treat `string` (and `string?`) as no conversion: assign the captured value to the handler
variable, as an untyped `{name}` does.

## Goal (implementer restatement)

`string` is a documented built-in type constraint, so a route that writes `{x:string}` or
`{x:string?}` must compile and bind the argument exactly like an untyped `{x}` / `{x?}`.
Today the generator emits a "no converter" warning comment and never declares the handler
variable, so the generated code fails with CS0103.

Acceptance:
- `{x:string}` binds the captured argument; `{x:string?}` binds it when given and binds null when omitted.
- Constraint casing (`String`) works the same way.
- Options typed `--opt {value:string}` / `{value:string?}` bind as untyped string options do.
- A test pins all of the above, and fails without the fix (CS0103).

## Checklist

- [x] `{x:string}` and `{x:string?}` routes compile and bind
- [x] Test covering both
- [x] Check options (`--name {value:string}`) take the same path

## Session

- Created: 1411579 (2026-11-14), filed from task 532
- 2026-11-14 implement oracle (claude): string-constraint emitter branch + option path + test; gates green
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 14f21ddf-a5b7-4e76-9f23-ec10db4848d1, max-turns 500, Done — 2026-11-14T02:54:55Z

## Notes

- The tw-nuru-verify docs step classifies this snippet (`supported-types.md` "String (Default)")
  as a fragment because the CS0103 is in generated code. It should be broken; once this is fixed
  the snippet compiles.

## Results

Fix: `EmitTypeConversions` (`route-matcher-emitter.type-conversions.cs`) now has a `string` branch.
For `{x:string}` / `{x:string?}` (any casing) it declares the handler variable from the capture with no
conversion: `string x = __x_N;`, or `string? x = __x_N;` when the parameter is optional. That is the same
binding an untyped `{x}` gets.

- New `TypeConversionMap.IsStringConstraint(string?)` checks for `string`/`string?` in any casing.
- Options: a `--opt {value:string}` option already compiled through the unknown-type fallback
  (`string? value = raw;`). It now skips the conversion step and takes the untyped option path
  (`route-matcher-emitter.option-parsing.cs`), so a raw default initializer applies the same way it
  does for an untyped option.
- The string-typed catch-all checks in `handler-invoker-emitter.output.cs` and
  `json-args-emitter.slots.cs` now use the same helper. Before this they matched only an exact
  `"string"`, while `EmitTypeConversions` already skipped `string?` as well.
- New test `tests/timewarp-nuru-tests/routing/routing-38-string-type-constraint.cs` has 8 cases:
  required, `String` casing, optional given and omitted, string next to int, required option, and
  optional option given and omitted. The CI multi-mode glob picks it up. Without the fix it fails to
  compile with `CS0103: The name 'message' does not exist` (checked by reverting the source change).
- Once this merges, the `supported-types.md` "String (Default)" snippet compiles. No doc change was needed.

### Gates

- `./bin/dev build`: succeeded, 0 warnings, 0 errors.
- `./bin/dev test`: exit 0. The log has 2449 PASSED and 0 FAILED. StringTypeConstraint ran in multi-mode,
  and standalone finished with 0 failed.
- `ganda repo audit`: passed with 1 advisory warning, `vscode-window-icon` (.vscode/settings.json missing,
  ignored file). After `ganda repo audit --fix`, `ganda repo audit` reported "Repository passes all audit
  checks." The fix touched no tracked files. The commit hook then flagged `runfile-executable`
  because the new test runfile had no executable bit. Fixed with chmod +x and a re-attest.

### How to validate

Smoke:
```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/routing/routing-38-string-type-constraint.cs
```

Expect: the file compiles, with no CS0103 in NuruGenerated.g.cs, and reports `Total: 8  Passed: 8`.
