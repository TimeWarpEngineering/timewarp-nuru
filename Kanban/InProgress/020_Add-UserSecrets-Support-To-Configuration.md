# Add User Secrets Support to Configuration

## Overview

Add support for .NET user secrets to the configuration chain in `AddConfiguration()`, matching ASP.NET Core behavior for development-time secret management.

## Context

User secrets provide a standard way to store sensitive configuration data during development without committing them to source control. This is a common pattern in .NET applications that Nuru should support.

## Current State

The configuration chain in `AddConfiguration()` includes:
1. appsettings.json
2. appsettings.{Environment}.json
3. {ApplicationName}.settings.json
4. {ApplicationName}.settings.{Environment}.json
5. Environment variables
6. Command line args

**Missing**: User secrets (typically loaded between app-specific settings and environment variables)

## Requirements

### 1. Add Package Dependency

Add to `TimeWarp.Nuru.csproj`:
```xml
<PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" />
```

Add to `Directory.Packages.props`:
```xml
<PackageVersion Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.10" />
```

### 2. Update Configuration Chain

Modify `AddConfiguration()` to include user secrets in Development environment:

```csharp
// After application-specific settings files
if (environmentName == "Development")
{
  // Add user secrets - optional parameter means it won't throw if UserSecretsId is missing
  configuration.AddUserSecrets(Assembly.GetEntryAssembly()!, optional: true, reloadOnChange: true);
}

configuration.AddEnvironmentVariables();
```

### 3. Documentation

**For runfiles**, document TWO ways to set UserSecretsId:

**Option 1: Using #:property directive (recommended for runfiles)**:

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru@2.1.0-beta.28
#:property UserSecretsId=my-app-secrets-id

NuruApp app =
  new NuruAppBuilder()
  .AddDependencyInjection()
  .AddConfiguration(args)  // ✅ Loads user secrets in Development
  .AddRoute("test", () => Console.WriteLine(app.Configuration["ApiKey"]))
  .Build();

await app.RunAsync(args);
```

**Option 2: Using assembly attribute**:

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru@2.1.0-beta.28

[assembly: Microsoft.Extensions.Configuration.UserSecrets.UserSecretsId("my-app-secrets-id")]

NuruApp app =
  new NuruAppBuilder()
  .AddDependencyInjection()
  .AddConfiguration(args)  // ✅ Loads user secrets in Development
  .AddRoute("test", () => Console.WriteLine(app.Configuration["ApiKey"]))
  .Build();

await app.RunAsync(args);
```

**Initialize user secrets**:
```bash
# For runfiles
dotnet user-secrets init --id my-app-secrets-id

# Set a secret
dotnet user-secrets set "ApiKey" "super-secret-123" --id my-app-secrets-id

# Run
DOTNET_ENVIRONMENT=Development ./myapp.cs test
```

**For published apps**, use standard project file approach:
```xml
<PropertyGroup>
  <UserSecretsId>my-app-secrets-id</UserSecretsId>
</PropertyGroup>
```

### 4. Standard Paths

User secrets are stored in:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/{UserSecretsId}/secrets.json`

### 5. Priority Order

Final configuration chain with user secrets:
1. appsettings.json
2. appsettings.{Environment}.json
3. {ApplicationName}.settings.json
4. {ApplicationName}.settings.{Environment}.json
5. **User secrets (Development only)**
6. Environment variables
7. Command line args

## Testing

Create test scenarios:
1. Runfile with `[assembly: UserSecretsId]` attribute
2. Runfile without attribute (should not crash)
3. Published app with UserSecretsId in project file
4. Published app without UserSecretsId (should not crash)
5. Production environment (user secrets should not load)

## Benefits

- ✅ Matches ASP.NET Core behavior
- ✅ Standard .NET development practice
- ✅ Keep secrets out of source control
- ✅ Cross-platform support
- ✅ Works for both runfiles and published apps

## References

- [Microsoft.Extensions.Configuration.UserSecrets](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.UserSecrets)
- [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [UserSecretsConfigurationExtensions](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.usersecretsconfigurationextensions)

## Acceptance Criteria

- [ ] Package dependency added
- [ ] Configuration chain updated to include user secrets
- [ ] Only loads in Development environment
- [ ] Uses `optional: true` parameter (default behavior, gracefully handles missing UserSecretsId)
- [ ] Documentation updated with runfile examples
- [ ] Tests created for all scenarios
- [ ] Sample created demonstrating user secrets
- [ ] Blip created for social media

## Estimated Effort

Small - 1-2 hours
- Package reference: 5 minutes
- Code changes: 30 minutes
- Testing: 30 minutes
- Documentation: 30 minutes
