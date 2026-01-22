# Deployment

Deploy your TimeWarp.Nuru CLI applications as native executables, .NET 10 runfiles, or traditional .NET applications.

## Native AOT Compilation

TimeWarp.Nuru is designed for full AOT compatibility. Both the Fluent API and Endpoint API work seamlessly with Native AOT.

### Basic AOT Setup

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

```bash
dotnet publish -c Release -r linux-x64
# Creates self-contained binary: ~3-5 MB
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

TimeWarp.Nuru uses source generators to achieve full AOT compatibility with zero IL2XXX/IL3XXX warnings. When you reference the `TimeWarp.Nuru` NuGet package, the source generators are automatically included and run at compile time.

**What the source generators do:**
- Analyze your route patterns and handler signatures
- Generate typed invoker methods at compile time
- Generate service resolution code (no runtime reflection)
- Ensure fast, AOT-compatible execution

### AOT and Dependency Injection

TimeWarp.Nuru provides two DI strategies:

**Source-Generated DI (Default)** - Fully AOT compatible:
```csharp
NuruApp app = NuruApp.CreateBuilder()
  .WithName("myapp")
  .ConfigureServices(services =>
  {
    services.AddSingleton<IGreeter, Greeter>();
  })
  .Map("greet {name}")
    .WithHandler((string name, IGreeter greeter) => greeter.Greet(name))
    .Done()
  .Build();
```

**Runtime DI** - May have AOT limitations:
```csharp
NuruApp app = NuruApp.CreateBuilder()
  .UseMicrosoftDependencyInjection()  // Uses MS DI container at runtime
  .ConfigureServices(services =>
  {
    services.AddSingleton<IGreeter, Greeter>();
  })
  // ...
  .Build();
```

**AOT Considerations for `UseMicrosoftDependencyInjection()`:**
- The MS DI container itself is AOT compatible
- Services with simple constructors work fine
- Extension methods like `AddDbContext()` may require additional configuration
- For maximum AOT compatibility, prefer the default source-generated DI

### Fail-Fast Behavior

TimeWarp.Nuru uses a **fail-fast, no silent fallback** approach:

- If a route pattern or handler has issues, an exception is thrown immediately
- There is no silent fallback to reflection
- This ensures you discover any issues at development time, not in production

### AOT Supported Features

**Fully Supported:**
- All built-in types (int, double, string, bool, DateTime, Guid, etc.)
- Nullable types (int?, string?, etc.)
- Array parameters for catch-all routes (`string[]`)
- Async/await patterns
- Optional parameters with `?` syntax
- Boolean options (flags)
- Custom type converters (when AOT-compatible)
- Fluent API (`Map().WithHandler().Done()`)
- Endpoint API (`[NuruRoute]` attributes)

**Considerations:**
- Custom type converters must not use runtime code generation
- Third-party libraries in your handlers must be AOT-compatible
- `UseMicrosoftDependencyInjection()` may limit some DI extension methods

## .NET 10 Runfiles

Create single-file executables that run directly.

### Basic Runfile

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru@1.0.0

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .WithName("greet")
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .Done()
  .Build();

return await app.RunAsync(args);
```

```bash
chmod +x mycli.cs
./mycli.cs greet World
```

### Publishing a Runfile as AOT

Runfiles can be published as AOT binaries:

```bash
dotnet publish mycli.cs -c Release -r linux-x64
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
| Native AOT | ~3-5 MB | Instant | Slow | Fastest startup |
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
./bin/Release/net10.0/publish/mycli --version
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
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Publish
      run: dotnet publish -c Release -r ${{ matrix.rid }} -p:PublishAot=true

    - name: Upload
      uses: actions/upload-artifact@v4
      with:
        name: mytool-${{ matrix.rid }}
        path: bin/Release/net10.0/${{ matrix.rid }}/publish/
```

## Related Documentation

- **[Architecture Choices](architecture-choices.md)** - Choose Fluent API vs Endpoint API
- **[Performance](../reference/performance.md)** - Detailed benchmarks
- **[Getting Started](../getting-started.md)** - Development setup
