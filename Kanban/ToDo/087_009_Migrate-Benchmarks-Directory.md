# Migrate Benchmarks Directory

## Description

Rename the Benchmarks directory and contents to kebab-case naming convention.

## Parent

087_Migrate-Repository-To-Kebab-Case-Naming

## Requirements

- Rename `Benchmarks/` → `benchmarks/`
- Rename `TimeWarp.Nuru.Benchmarks/` → `timewarp-nuru-benchmarks/`
- Rename subdirectories: `Analysis/`, `Commands/`, `Results/`
- Rename .csproj file
- Rename PascalCase C# files to kebab-case
- Update ProjectReference paths

## Checklist

- [ ] Rename Benchmarks directory to lowercase
- [ ] Rename benchmark project directory
- [ ] Rename subdirectories to kebab-case
- [ ] Rename .csproj file
- [ ] Rename C# source files
- [ ] Rename markdown files to lowercase
- [ ] Update ProjectReference paths
- [ ] Verify benchmarks still run

## Notes

- Medium risk - fewer dependencies than Source/Tests
- Command files (CliFxCommand.cs, etc.) need renaming
- Analysis markdown files need renaming
