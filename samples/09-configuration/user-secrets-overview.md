# User Secrets with csproj

This sample demonstrates user secrets support in TimeWarp.Nuru with a traditional csproj project.

## Setup

### 1. Configure UserSecretsId in .csproj

```xml
<PropertyGroup>
  <UserSecretsId>nuru-csproj-user-secrets-demo</UserSecretsId>
</PropertyGroup>
```

### 2. Initialize and Set Secrets

```bash
# Set secrets using the UserSecretsId
dotnet user-secrets set "ApiKey" "super-secret-api-key-123" --id nuru-csproj-user-secrets-demo
dotnet user-secrets set "Database:ConnectionString" "Server=localhost;Database=TestDB" --id nuru-csproj-user-secrets-demo

# Or from the project directory
dotnet user-secrets set "ApiKey" "super-secret-api-key-123"
```

### 3. Run the Sample

```bash
# Development - secrets load
DOTNET_ENVIRONMENT=Development dotnet run -- show

# Production - secrets don't load (secure by default)
DOTNET_ENVIRONMENT=Production dotnet run -- show
```

## How It Works

The `AddConfiguration()` method automatically:
1. Loads user secrets when `DOTNET_ENVIRONMENT=Development`
2. Skips user secrets in Production (safe by default)
3. Uses the `UserSecretsId` from the assembly attribute (generated from csproj property)

## Accessing Configuration

Inject `IConfiguration` into your route handlers:

```csharp
.Map("show", (IConfiguration config) =>
{
  string? apiKey = config["ApiKey"];
  string? dbConnection = config["Database:ConnectionString"];
  // Use the secrets...
})
```

## Comparison: csproj vs Runfile

| Aspect | csproj | Runfile |
|--------|--------|---------|
| UserSecretsId | `<UserSecretsId>` in .csproj | `#:property UserSecretsId=guid` |
| Setting Secrets | `dotnet user-secrets set` with `--id` | `dotnet user-secrets set` with `--id` |
| Behavior | Identical | Identical |

Both approaches work exactly the same - the only difference is how you specify the UserSecretsId.

## Benefits

✅ Keep sensitive config out of source control
✅ Team collaboration with individual secrets
✅ Standard .NET workflow
✅ Same behavior as ASP.NET Core

## Version

Requires TimeWarp.Nuru **2.1.0-beta.28** or greater.
