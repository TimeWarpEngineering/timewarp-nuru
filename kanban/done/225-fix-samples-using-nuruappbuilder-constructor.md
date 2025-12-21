# Fix samples using NuruAppBuilder constructor

## Description

Two samples instantiate `NuruAppBuilder` directly with no arguments, but the constructor now requires arguments. Update to use `NuruApp.CreateBuilder(args)` factory method instead.

Error: `CS1729: 'NuruAppBuilder' does not contain a constructor that takes 0 arguments`

## Checklist

- [x] samples/builtin-types-example.cs
- [x] samples/custom-type-converter-example.cs

## Notes

- Discovered by `runfiles/verify-samples.cs` (task 221)
- These samples should use `NuruApp.CreateBuilder([])` or similar factory method
