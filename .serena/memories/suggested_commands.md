# Suggested Commands for Development

## Build Commands
- **Build library**: `dotnet build Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj --configuration Release`
- **Build with script**: `./Scripts/Build.cs` (executable C# script using TimeWarp.Cli)
- **Build all**: `dotnet build`

## Testing Commands
- **Run integration tests**: 
  - `cd Samples/TimeWarp.Nuru.IntegrationTests && ./test-all.sh`
  - Or: `dotnet run --project Samples/TimeWarp.Nuru.IntegrationTests -- [command]`
- **Test specific command**: `dotnet run --project Samples/TimeWarp.Nuru.IntegrationTests -- git status`

## Package Management
- **Restore packages**: `dotnet restore`
- **Pack NuGet**: `dotnet pack Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj --configuration Release`
- **Local NuGet cache**: Packages are restored to `LocalNuGetCache` directory

## Git Commands
- **Status**: `git status`
- **Diff**: `git diff`
- **Commit**: `git commit -m "message"`
- **Main branch**: `master` (not `main`)

## Running Samples
- **Sample app**: `dotnet run --project Samples/TimeWarp.Nuru.Sample -- [command]`
- **Integration tests app**: `dotnet run --project Samples/TimeWarp.Nuru.IntegrationTests -- [command]`

## Script Execution
- Scripts in the Scripts directory are executable C# files
- Make executable: `chmod +x Scripts/*.cs`
- Run: `./Scripts/Build.cs`

## Linting and Code Quality
- **Build with analyzers**: `dotnet build` (analyzers run automatically)
- **Format code**: `dotnet format` (standard .NET formatting)

Note: No explicit linting tool is configured. The project relies on .NET analyzers configured in Directory.Build.props.