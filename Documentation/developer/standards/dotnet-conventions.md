.NET CONVENTIONS:

FRAMEWORK:
- Target net9.0

PROJECT CONFIGURATION:
- Use Directory.Build.props for shared project properties
- Use Directory.Packages.props for centralized package versioning
- Enable nullable reference types

SOLUTION MANAGEMENT:
- Use the new .slnx format
- Never edit .slnx file directly
  ✓ `dotnet sln add ./src/MyProject/MyProject.csproj`
  ✗ Manual .slnx file editing

TOOLING:
- Initialize local tool manifest
  ✓ ```pwsh
     dotnet new tool-manifest
     ```
  Creates: .config/dotnet-tools.json
