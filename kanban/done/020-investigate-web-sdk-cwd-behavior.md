# Investigate How Web SDK Changes Working Directory for File-Based Apps

## Problem

When using `#:sdk Microsoft.NET.Sdk.Web` in a runfile, the current working directory (CWD) is automatically set to the script's directory, allowing configuration files like `{appname}.settings.json` to be found regardless of where the script is executed from.

Without the Web SDK, CWD remains where the user executed the script from, causing configuration files to not be found.

## Current Behavior

**With `#:sdk Microsoft.NET.Sdk.Web`:**
```bash
cd /anywhere
./Samples/Configuration/script.cs
# CWD inside script: /path/to/Samples/Configuration/ ✓
```

**Without Web SDK:**
```bash
cd /anywhere  
./Samples/Configuration/script.cs
# CWD inside script: /anywhere ✗
```

## Investigation Results

We've confirmed:
- NOT an assembly attribute
- NOT an AppContext data value (`ENTRY_POINT_FILE_PATH` is empty)
- NOT MSBuild properties
- Happens at **runtime**, not build time
- No obvious source code in ASP.NET DefaultBuilder

## Possible Causes

1. **Dotnet host behavior** - The native dotnet executable may check SDK type and change CWD
2. **Hidden module initializer** - Web SDK may inject code that runs before Main
3. **Runtime configuration** - Some `.runtimeconfig.json` setting we haven't found
4. **Application host model** - ASP.NET uses different hosting that changes directory

## Next Steps

Waiting for response from Damien Edwards (@DamianEdwards) on Twitter: https://x.com/StevenTCramer/status/1982309927361327452

Once we understand the mechanism, we can either:
1. Replicate it in TimeWarp.Nuru's build process
2. Add a lightweight package that provides just this behavior
3. Make `AddConfiguration()` smarter about finding settings files

## Workaround

For now, NuruApp configuration samples must be run from their containing directory, or use `#:sdk Microsoft.NET.Sdk.Web` (which pulls in all of ASP.NET).

## References

- Twitter thread: https://x.com/StevenTCramer/status/1982309927361327452
- .NET file-based apps spec: https://github.com/dotnet/sdk/blob/main/documentation/general/dotnet-run-file.md

## RESOLVED ✓

**Solution Found:** Use `[CallerFilePath]` attribute with smart fallback chain!

Instead of trying to replicate the Web SDK's mysterious CWD behavior, we implemented a better solution using `[CallerFilePath]` with a fallback strategy that works for both development and production scenarios.

### Implementation

The solution is implemented in `NuruAppBuilder.AddConfiguration()` which calls the extracted helper method `DetermineConfigurationBasePath()`:

```csharp
private static string DetermineConfigurationBasePath(string sourceFilePath)
{
  string basePath = AppContext.BaseDirectory;

  // Check if application-specific settings exist in assembly directory
  string? applicationName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
  bool configInAssemblyDir = false;

  if (!string.IsNullOrEmpty(applicationName))
  {
    string sanitizedName = applicationName
      .Replace(Path.DirectorySeparatorChar, '_')
      .Replace(Path.AltDirectorySeparatorChar, '_');
    string assemblyConfigPath = Path.Combine(basePath, $"{sanitizedName}.settings.json");
    configInAssemblyDir = File.Exists(assemblyConfigPath) || File.Exists(Path.Combine(basePath, "appsettings.json"));
  }

  // If no config in assembly directory and we have source file path, try source directory
  if (!configInAssemblyDir && !string.IsNullOrEmpty(sourceFilePath))
  {
    string? sourceDir = Path.GetDirectoryName(sourceFilePath);
    if (!string.IsNullOrEmpty(sourceDir) && Directory.Exists(sourceDir))
    {
      basePath = sourceDir;
    }
  }

  // Final fallback to current directory if assembly dir and source dir don't have configs
  if (!configInAssemblyDir && basePath == AppContext.BaseDirectory)
  {
    basePath = Directory.GetCurrentDirectory();
  }

  return basePath;
}
```

### Fallback Chain Strategy

**Priority order:**
1. **Assembly directory** (`AppContext.BaseDirectory`) - For published executables with deployed config files
2. **Source file directory** (from `[CallerFilePath]`) - For file-based apps during development
3. **Current directory** - Final fallback

### Why This Order Matters

**Initial incorrect approach:** Checked source directory first, then assembly directory
- ❌ Problem: `[CallerFilePath]` is compile-time, so published executables contain the build machine's path
- ❌ Published apps would fail when deployed to different machines

**Correct approach:** Check assembly directory first, then source directory
- ✅ Published executables find config in their deployment directory
- ✅ File-based apps fall back to source directory (build cache doesn't contain config files)
- ✅ Works seamlessly in both scenarios without special configuration

### Testing Results

**Development scenario (file-based app):**
```bash
./test-config-path.cs test
# AppContext.BaseDirectory: ~/.local/share/dotnet/runfile/.../bin/debug/
# No config in build cache → falls back to source directory ✓
```

**Published scenario (standard publish):**
```bash
dotnet publish test-config-path.cs -o published
cd published && ./test-config-path test
# AppContext.BaseDirectory: /path/to/published/
# Config found in assembly directory ✓
```

**AOT scenario (native compilation):**
```bash
dotnet publish test-config-path.cs -o published-aot -r linux-x64 -p:PublishAot=true
cd published-aot && ./test-config-path test
# Zero warnings during AOT compilation ✓
# 4.8MB native executable ✓
# Config found in assembly directory ✓
```

### Benefits

The `[CallerFilePath]` solution provides:
- ✅ Configuration files work from any directory without Web SDK
- ✅ Zero runtime overhead (compile-time attribute)
- ✅ Works for both file-based apps AND published executables
- ✅ Full AOT compatibility with zero warnings
- ✅ Clean code organization (extracted to `DetermineConfigurationBasePath()` method)
- ✅ Handles path separators in assembly names (e.g., `.agent/workspace/script.cs`)

### Result

Configuration loading now works correctly in all scenarios:
- File-based apps can be run from anywhere
- Published executables find config in their deployment directory
- AOT compilation produces zero warnings
- No dependency on `#:sdk Microsoft.NET.Sdk.Web`

**Implemented in:** Source/TimeWarp.Nuru/NuruAppBuilder.cs (lines 523-557)
