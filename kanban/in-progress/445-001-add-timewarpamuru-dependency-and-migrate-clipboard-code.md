# Add TimeWarp.Amuru dependency and migrate clipboard code

## Description

Add TimeWarp.Amuru as a dependency to timewarp-nuru and migrate the clipboard code from `System.Diagnostics.Process` to Amuru's `Shell.Builder` API.

## Checklist

- [ ] Add TimeWarp.Amuru package reference to timewarp-nuru.csproj
- [ ] Update Directory.Packages.props with Amuru version
- [ ] Migrate repl-console-reader.clipboard.cs to use Shell.Builder
- [ ] Remove direct System.Diagnostics.Process usage
- [ ] Run tests to verify clipboard functionality

## Notes

### Current State

The clipboard code in `source/timewarp-nuru/repl/input/repl-console-reader.clipboard.cs` uses `System.Diagnostics.Process` directly for:
- Windows: `powershell` for Get-Clipboard/Set-Clipboard
- macOS: `pbpaste`/`pbcopy`
- Linux: `pwsh`, `xclip`, `xsel`, `wl-copy`/`wl-paste`

### Migration Pattern

```csharp
// Before
using System.Diagnostics.Process process = new()
{
  StartInfo = new System.Diagnostics.ProcessStartInfo
  {
    FileName = "pbpaste",
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
  }
};
process.Start();
string result = process.StandardOutput.ReadToEnd();
process.WaitForExit();

// After
CommandOutput output = await Shell.Builder("pbpaste")
  .CaptureAsync();
string result = output.Stdout;
```

### Why Amuru

Per the Amuru skill: "ALWAYS use TimeWarp.Amuru for process execution in .NET."

Benefits:
- Cleaner async API
- Built-in error handling
- Consistent with TimeWarp conventions
- Required for --search feature anyway

### Parent Task

#445 - Add --search and --group-filter options to --capabilities
