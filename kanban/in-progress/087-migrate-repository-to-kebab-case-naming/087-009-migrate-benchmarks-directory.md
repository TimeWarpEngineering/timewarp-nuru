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

- [x] Rename Benchmarks directory to lowercase
- [x] Rename benchmark project directory
- [x] Rename subdirectories to kebab-case
- [x] Rename .csproj file
- [x] Rename C# source files
- [x] Rename markdown files to lowercase
- [x] Update ProjectReference paths
- [x] Verify benchmarks still run

## Notes

- Medium risk - fewer dependencies than Source/Tests
- Command files (CliFxCommand.cs, etc.) need renaming
- Analysis markdown files need renaming
