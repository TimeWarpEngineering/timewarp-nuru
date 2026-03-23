# TimeWarp.Nuru.DevCli

Reusable dev-cli endpoints for TimeWarp repositories. This package provides source-only endpoint files that can be consumed by any TimeWarp repository.

## What's Included

| Endpoint | Description | Dependencies |
|----------|-------------|--------------|
| `clean` | Clean solution and build artifacts | `IRepoCleanService` (TimeWarp.Amuru) |
| `self-install` | AOT compile dev CLI to ./bin | None (standalone) |
| `check-version` | Verify version is ready to release | `IRepoCheckVersionService` (TimeWarp.Amuru) |

## Installation

Add the package to your project:

```bash
dotnet add package TimeWarp.Nuru.DevCli
```

The source files will be automatically included in your project's compilation via the `.props` file.

## Requirements

- TimeWarp.Nuru (the CLI framework)
- TimeWarp.Amuru 1.0.0-beta.22+ (for repo services)
- TimeWarp.Terminal (for ITerminal)

## Usage

The endpoints will be automatically discovered when you use `DiscoverEndpoints()`:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services =>
  {
    // Register required services
    services.AddSingleton<IRepoCleanService, RepoCleanService>();
    services.AddSingleton<IRepoCheckVersionService, RepoCheckVersionService>();
    services.AddSingleton<IRepoConfigService, RepoConfigService>();
  })
  .UseMicrosoftDependencyInjection()
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);
```

## Source-Only Package

This is a **source-only NuGet package**. The endpoint files are included in your project's compilation, not as a compiled assembly. This is required for Nuru's source generators to work correctly.

### Why Source-Only?

Nuru uses source generators to create route matching code at compile time. The generator needs to see the endpoint class definitions in your project's source. Traditional NuGet packages with compiled DLLs would hide the source from the generator.

### Creating Your Own Source-Only Endpoint Packages

This package serves as a reference for creating your own reusable endpoint packages:

1. Create a project with `IncludeBuildOutput=false`
2. Place endpoint files in `content/any/endpoints/`
3. Create a `.props` file in `build/` to include the content files
4. Pack the project as a NuGet package

Example `.props` file:

```xml
<Project>
  <ItemGroup>
    <Compile Include="$(MSBuildThisFileDirectory)../content/any/endpoints/*.cs" 
             Visible="false" />
  </ItemGroup>
</Project>
```

## License

MIT
