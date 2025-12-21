# Fix samples missing TimeWarp.Nuru.Completion namespace

## Description

Two samples reference `TimeWarp.Nuru.Completion` namespace but it doesn't exist or isn't accessible. Need to add correct `#:project` reference or update the using statements.

Error: `CS0234: The type or namespace name 'Completion' does not exist in the namespace 'TimeWarp.Nuru'`

## Checklist

- [ ] samples/dynamic-completion-example/dynamic-completion-example.cs
- [ ] samples/shell-completion-example/shell-completion-example.cs

## Notes

- Discovered by `runfiles/verify-samples.cs` (task 221)
- May need to add `#:project ../../source/timewarp-nuru-completion/timewarp-nuru-completion.csproj`
