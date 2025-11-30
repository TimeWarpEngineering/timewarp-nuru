# Migrate Assets And Msbuild Directories

## Description

Rename the Assets directory to lowercase. Verify msbuild directory compliance.

## Parent

087_Migrate-Repository-To-Kebab-Case-Naming

## Requirements

- Rename `Assets/` → `assets/`
- Rename `Logo.png` → `logo.png`
- Verify `msbuild/` already lowercase (it is)
- Rename `repository.props` if needed (already lowercase)

## Checklist

- [x] Rename Assets directory to lowercase
- [x] Rename image files to lowercase
- [x] Verify msbuild directory compliance
- [x] Update any references to logo/assets

## Notes

- Low risk - minimal dependencies
- Check if logo is referenced in .csproj or nuget packaging
