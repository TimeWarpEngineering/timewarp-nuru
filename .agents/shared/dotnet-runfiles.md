## .NET 10 File-Based Apps (RUNFILES)

**CRITICAL TERMINOLOGY**: These are called RUNFILES or "file-based apps" - NEVER call them "scripts". They are FULLY COMPILED APPLICATIONS with generated .csproj files. The `#!/usr/bin/dotnet --` shebang triggers full compilation. They support AOT, source generators, and all features of compiled apps.

### Directives (Built into .NET 10 SDK)

- `#:package PackageName@Version` - Add NuGet package (@ for version)
- `#:project path/to/project.csproj` - Reference project
- `#:property PropertyName=Value` - Set MSBuild property (= for assignment)
- `#:sdk SdkName@Version` - Add SDK reference (@ for version)

### Example

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Amuru@1.0.0-beta.5

using TimeWarp.Amuru;

// Shell-like execution - streams to console
await Shell.Builder("dotnet", "build").RunAsync();

// Capture output for processing
var result = await Shell.Builder("git", "status").CaptureAsync();
if (result.Success)
{
    Console.WriteLine($"Clean: {!result.Stdout.Contains("modified")}");
}
```

### Execution

- Make executable: `chmod +x runfile.cs`
- Run directly: `./runfile.cs`
- Or: `dotnet run runfile.cs`
- Publish: `dotnet publish runfile.cs`
- Clean cache: `dotnet clean <runfile>` (clears the compiled runfile cache)

### Important

- **NEVER use `#r` directives** - that's obsolete dotnet-script syntax
- **NEVER mention dotnet-script** - it's dead technology
- Use `#:package` for NuGet references
- Use `#:project` for project references
