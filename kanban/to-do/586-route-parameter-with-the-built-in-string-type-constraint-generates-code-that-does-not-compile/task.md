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

## Checklist

- [ ] `{x:string}` and `{x:string?}` routes compile and bind
- [ ] Test covering both
- [ ] Check options (`--name {value:string}`) take the same path

## Session

- Created: 1411579 (2026-11-14), filed from task 532

## Notes

- The tw-nuru-verify docs step classifies this snippet (`supported-types.md` "String (Default)")
  as a fragment because the CS0103 is in generated code. It should be broken; once this is fixed
  the snippet compiles.
