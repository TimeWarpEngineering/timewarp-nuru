# Deployment

Deploy your TimeWarp.Nuru CLI applications as native executables, .NET 10 runfiles, or traditional .NET applications.

## Native AOT Compilation

Compile to fast, self-contained native binaries with instant startup.

### For Direct Approach

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

```bash
dotnet publish -c Release -r linux-x64
# Creates self-contained binary: ~3.3 MB
# Instant startup: < 1ms
```

### For Mediator/Mixed Approach

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <TrimMode>partial</TrimMode>  <!-- Preserve reflection for handlers -->
</PropertyGroup>
```

```bash
dotnet publish -c Release -r linux-x64
# Creates self-contained binary: ~4.8 MB
# Instant startup: < 1ms
```

### Cross-Platform Builds

```bash
# Linux
dotnet publish -c Release -r linux-x64

# macOS (Intel)
dotnet publish -c Release -r osx-x64

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64

# Windows
dotnet publish -c Release -r win-x64
```

### Source Generators for AOT

TimeWarp.Nuru uses source generators to achieve full AOT compatibility with zero IL2XXX/IL3XXX warnings. When you reference the `TimeWarp.Nuru` NuGet package, the `NuruInvokerGenerator` source generator is automatically included and runs at compile time.

**What the source generator does:**
- Analyzes your `Map()` delegate signatures
- Generates typed invoker methods at compile time
- Eliminates reflection-based delegate invocation
- Ensures fast, AOT-compatible execution

**When using `NuruApp.CreateBuilder()` with DI:**

You must register the Mediator source generator for AOT-compatible dependency injection:

```csharp
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;

NuruAppBuilder builder = NuruApp.CreateBuilder(args);

// REQUIRED: Register source-generated Mediator for AOT compatibility
builder.Services.AddMediator();

builder.Map("hello", () => Console.WriteLine("Hello!"));
// ... more routes

NuruCoreApp app = builder.Build();
return await app.RunAsync(args);
```

Add the Mediator packages to your project:

```xml
<ItemGroup>
  <PackageReference Include="Mediator.Abstractions" />
  <PackageReference Include="Mediator.SourceGenerator">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### Fail-Fast Behavior

TimeWarp.Nuru uses a **fail-fast, no silent fallback** approach for AOT compatibility:

- If a delegate signature doesn't have a generated invoker, an exception is thrown immediately
- There is no silent fallback to reflection (which would fail at runtime with AOT anyway)
- This ensures you discover any issues at development time, not in production

Example error:
```
No source-generated invoker found for signature 'MyCustomSignature'.
Ensure the NuruInvokerGenerator source generator is running and the delegate signature is supported.
```

**Supported delegate signatures include:**
- Parameterless: `() => ...`
- With parameters: `(string name, int count) => ...`
- With optional parameters: `(string name, int? count) => ...`
- Async variants: `async () => ...`, `async (string name) => ...`
- Returning int or Task<int> for exit codes

### AOT Limitations and Edge Cases

**Fully Supported:**
- All built-in types (int, double, string, bool, DateTime, Guid, etc.)
- Nullable types (int?, string?, etc.)
- Array parameters for catch-all routes (`string[]`)
- Async/await patterns
- Optional parameters
- Boolean options (flags)

**Considerations:**
- Custom type converters must be AOT-compatible (no runtime code generation)
- Dynamic completion providers must not use reflection
- Third-party libraries in your handlers must be AOT-compatible

**Complete Example:**

See the [AOT Example](../../../samples/aot-example/) for a complete, working AOT sample.

### Migration from Non-AOT Versions

If upgrading from a version without AOT support:

1. **Add Mediator packages** (if using `CreateBuilder()`):
   ```xml
   <PackageReference Include="Mediator.Abstractions" />
   <PackageReference Include="Mediator.SourceGenerator">
     <PrivateAssets>all</PrivateAssets>
     <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
   </PackageReference>
   ```

2. **Call `AddMediator()`**:
   ```csharp
   builder.Services.AddMediator();
   ```

3. **Add AOT properties** to your .csproj:
   ```xml
   <PropertyGroup>
     <PublishAot>true</PublishAot>
     <TrimMode>partial</TrimMode>
   </PropertyGroup>
   ```

4. **Build and test** - any unsupported patterns will fail fast with clear error messages

5. **Publish**:
   ```bash
   dotnet publish -c Release -r linux-x64
   ```

No other code changes are required for standard use cases.

## .NET 10 Runfiles

Create single-file executables that run directly.

### Basic Runfile

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru

using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("greet {name}", (string name) => $"Hello, {name}!")
  .Build();

return await app.RunAsync(args);
```

```bash
chmod +x mycli.cs
./mycli.cs greet World
```

### With Multiple Packages

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru
#:package TimeWarp.Nuru.Logging
#:package Serilog

using TimeWarp.Nuru;
using TimeWarp.Nuru.Logging;
using Serilog;

// Your CLI code here
```

## Traditional .NET Deployment

### Framework-Dependent

Requires .NET runtime on target machine:

```bash
dotnet publish -c Release
```

**Benefits:**
- Small deployment size
- Shares runtime with other apps
- Easy updates via runtime updates

### Self-Contained

Includes .NET runtime:

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

**Benefits:**
- No runtime installation needed
- Specific runtime version guaranteed
- Isolated from system updates

## Distribution Methods

### Package Managers

#### NuGet (.NET Global Tool)

```xml
<PropertyGroup>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>mytool</ToolCommandName>
</PropertyGroup>
```

```bash
dotnet pack
dotnet tool install --global --add-source ./bin/Release MyTool
```

Users install:
```bash
dotnet tool install --global MyTool
mytool --help
```

#### npm (for .NET tools)

```json
{
  "name": "@myorg/mytool",
  "version": "1.0.0",
  "bin": {
    "mytool": "./bin/mytool"
  }
}
```

```bash
npm install -g @myorg/mytool
```

#### Homebrew (macOS/Linux)

```ruby
class Mytool < Formula
  desc "My CLI tool"
  homepage "https://github.com/myorg/mytool"
  url "https://github.com/myorg/mytool/releases/download/v1.0.0/mytool-osx-x64.tar.gz"
  sha256 "..."

  def install
    bin.install "mytool"
  end
end
```

### GitHub Releases

```bash
# Build for multiple platforms
dotnet publish -c Release -r linux-x64 -o dist/linux-x64
dotnet publish -c Release -r osx-x64 -o dist/osx-x64
dotnet publish -c Release -r win-x64 -o dist/win-x64

# Create archives
tar -czf mytool-linux-x64.tar.gz -C dist/linux-x64 .
tar -czf mytool-osx-x64.tar.gz -C dist/osx-x64 .
zip -r mytool-win-x64.zip dist/win-x64/*

# Upload to GitHub Releases
gh release create v1.0.0 \
  mytool-linux-x64.tar.gz \
  mytool-osx-x64.tar.gz \
  mytool-win-x64.zip
```

## Size Optimization

### Enable Trimming

```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>partial</TrimMode>
</PropertyGroup>
```

### Disable Globalization

If you don't need internationalization:

```xml
<PropertyGroup>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

Saves ~1-2 MB.

### Single File

```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
</PropertyGroup>
```

Bundles into single executable.

### Compression

```xml
<PropertyGroup>
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
</PropertyGroup>
```

## Performance Comparison

| Approach | Binary Size | Startup Time | Build Time | Notes |
|----------|-------------|--------------|------------|-------|
| Framework-Dependent | ~200 KB | Fast | Fast | Requires runtime |
| Self-Contained | ~65 MB | Fast | Moderate | No runtime needed |
| Self-Contained + Trimmed | ~25 MB | Fast | Slow | Optimized |
| Native AOT (Direct) | ~3.3 MB | Instant | Slow | Fastest |
| Native AOT (Mediator) | ~4.8 MB | Instant | Slow | Fast |
| .NET 10 Runfile | Source file | Fast | On-demand | Development |

See [Performance Reference](../reference/performance.md) for detailed benchmarks.

## Best Practices

### Development

Use runfiles or framework-dependent:
```bash
# Fast iteration
dotnet run -- command args
# or
./mycli.cs command args
```

### Testing

Use framework-dependent builds:
```bash
dotnet publish -c Release
./bin/Release/net9.0/publish/mycli --version
```

### Production

Use Native AOT for best performance:
```bash
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
```

### Distribution

- **Internal tools**: .NET Global Tool
- **Public tools**: Native AOT binaries via GitHub Releases
- **Cross-platform**: Multiple AOT builds for each platform
- **Quick scripts**: .NET 10 runfiles

## CI/CD Integration

### GitHub Actions

```yaml
name: Release

on:
  push:
    tags: ['v*']

jobs:
  build:
    strategy:
      matrix:
        os: [ubuntu-latest, macos-latest, windows-latest]
        include:
          - os: ubuntu-latest
            rid: linux-x64
          - os: macos-latest
            rid: osx-x64
          - os: windows-latest
            rid: win-x64

    runs-on: ${{ matrix.os }}

    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Publish
      run: dotnet publish -c Release -r ${{ matrix.rid }} -p:PublishAot=true

    - name: Upload
      uses: actions/upload-artifact@v3
      with:
        name: mytool-${{ matrix.rid }}
        path: bin/Release/net9.0/${{ matrix.rid }}/publish/
```

## Related Documentation

- **[Architecture Choices](architecture-choices.md)** - Choose Direct vs Mediator for optimal size
- **[Performance](../reference/performance.md)** - Detailed benchmarks
- **[Getting Started](../getting-started.md)** - Development setup
