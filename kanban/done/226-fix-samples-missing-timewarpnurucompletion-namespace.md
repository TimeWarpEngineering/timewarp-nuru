# Fix samples missing TimeWarp.Nuru.Completion namespace

## Description

Two samples reference `TimeWarp.Nuru.Completion` namespace but it doesn't exist or isn't accessible. Need to add correct `#:project` reference or update the using statements.

Error: `CS0234: The type or namespace name 'Completion' does not exist in the namespace 'TimeWarp.Nuru'`

## Checklist

- [x] samples/dynamic-completion-example/dynamic-completion-example.cs
- [x] samples/shell-completion-example/shell-completion-example.cs

## Notes

- Discovered by `runfiles/verify-samples.cs` (task 221)
- May need to add `#:project ../../source/timewarp-nuru-completion/timewarp-nuru-completion.csproj`

## Resolution

The `TimeWarp.Nuru.Completion` namespace doesn't exist - the completion types are in the `TimeWarp.Nuru` namespace (the project's `RootNamespace` is `TimeWarp.Nuru`).

Fixed by:
1. Removing the incorrect `using TimeWarp.Nuru.Completion;` statements
2. Updated samples to use the new fluent API (`NuruCoreApp.CreateSlimBuilder(args).Map().WithHandler().Done()`)
3. Both samples now compile successfully
