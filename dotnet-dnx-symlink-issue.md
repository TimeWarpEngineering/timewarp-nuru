# Missing /usr/bin/dnx symlink after .NET SDK installation

## Description
When installing the .NET SDK on Ubuntu (via the official Microsoft packages), the installation creates a symlink for `/usr/bin/dotnet` → `/usr/share/dotnet/dotnet`, but does not create a corresponding symlink for `/usr/bin/dnx` → `/usr/share/dotnet/dnx`, even though the `dnx` executable exists in `/usr/share/dotnet/`.

This inconsistency requires users to manually create the symlink or use the full path to access `dnx`.

## Environment
- **OS**: Ubuntu 24.04.2 LTS (Noble Numbat)
- **Installation method**: Official Microsoft APT packages
- **.NET versions installed**:
  - .NET SDK 8.0.411
  - .NET SDK 9.0.203
  - .NET SDK 10.0.100-preview.5.25277.114
  - .NET SDK 10.0.100-preview.6.25358.103
  - .NET SDK 10.0.100-preview.7.25380.108
  - .NET SDK 10.0.100-rc.1.25451.107

## Steps to Reproduce
1. Install .NET SDK on Ubuntu using the official Microsoft packages
2. Check for the dotnet symlink: `ls -la /usr/bin/dotnet` (exists)
3. Check for the dnx symlink: `ls -la /usr/bin/dnx` (does not exist)
4. Verify dnx executable exists: `ls /usr/share/dotnet/dnx` (exists)
5. Try to run `dnx` from command line: `which dnx` (command not found)

## Expected Behavior
The .NET SDK installation should create both symlinks:
- `/usr/bin/dotnet` → `/usr/share/dotnet/dotnet` ✅ (currently works)
- `/usr/bin/dnx` → `/usr/share/dotnet/dnx` ❌ (missing)

## Actual Behavior
Only the `/usr/bin/dotnet` symlink is created. The `/usr/bin/dnx` symlink is missing, preventing direct command-line access to `dnx`.

## Workaround
Manually create the symlink:
```bash
sudo ln -s /usr/share/dotnet/dnx /usr/bin/dnx
```

## Impact
Users cannot run `dnx` directly from the command line without either:
- Using the full path `/usr/share/dotnet/dnx`
- Manually creating the symlink
- Adding `/usr/share/dotnet` to their PATH

This affects the new .NET 10 single-file C# script functionality where scripts can use the shebang `#!/usr/bin/env dnx` or `#!/usr/bin/dnx`.

## Suggested Fix
Update the .NET SDK installation packages to create the `/usr/bin/dnx` symlink alongside the existing `/usr/bin/dotnet` symlink.