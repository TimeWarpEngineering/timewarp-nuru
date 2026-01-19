# User Secrets Sample

This sample demonstrates how to use .NET user secrets with TimeWarp.Nuru runfiles.

## Using `#:property` directive

See: [user-secrets-property.cs](user-secrets-property.cs)

```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:property UserSecretsId=nuru-user-secrets-demo

using Microsoft.Extensions.Configuration;
using TimeWarp.Nuru;

NuruApp app =
  new NuruAppBuilder()
  .AddDependencyInjection()
  .AddConfiguration(args)
  .Map("show", (IConfiguration config) =>
  {
    // Access secrets through IConfiguration
    string? apiKey = config["ApiKey"];
    Console.WriteLine($"ApiKey: {apiKey}");
  })
  .Build();

await app.RunAsync(args);
```

**Why `#:property` for runfiles:**
- Clean, declarative syntax
- Follows runfile conventions (`#:package`, `#:sdk`, `#:property`)
- Automatically generates the UserSecretsId in the generated .csproj
- No conflicts with auto-generated assembly attributes

**Note:** For traditional .csproj files, you can set `<UserSecretsId>` directly in the project file's `<PropertyGroup>`.

## Setup

### 1. Initialize User Secrets

```bash
# Initialize user secrets with the same ID used in the runfile
dotnet user-secrets init --id nuru-user-secrets-demo
```

### 2. Set Some Secrets

```bash
# Set an API key
dotnet user-secrets set "ApiKey" "super-secret-api-key-123" --id nuru-user-secrets-demo

# Set a database connection string
dotnet user-secrets set "Database:ConnectionString" "Server=localhost;Database=MyDb;User=sa;Password=SecretPass123!" --id nuru-user-secrets-demo
```

### 3. List Secrets (Optional)

```bash
dotnet user-secrets list --id nuru-user-secrets-demo
```

## Running the Sample

### In Development Environment (secrets will load)

```bash
DOTNET_ENVIRONMENT=Development dotnet run user-secrets-property.cs show
```

**Expected output:**
```
Configuration Values:
  ApiKey: super-secret-api-key-123
  Database:ConnectionString: Server=localhost;Database=MyDb;User=sa;Password=SecretPass123!

Note: User secrets are only loaded in Development environment.
Current environment: Development
```

### In Production Environment (secrets won't load)

```bash
DOTNET_ENVIRONMENT=Production dotnet run user-secrets-property.cs show
```

**Expected output:**
```
Configuration Values:
  ApiKey: (not set)
  Database:ConnectionString: (not set)

Note: User secrets are only loaded in Development environment.
Current environment: Production
```

## How It Works

1. **User secrets are only loaded in Development** - This is a safety feature to prevent accidentally using development secrets in production

2. **Storage location** - Secrets are stored in:
   - **Windows**: `%APPDATA%\Microsoft\UserSecrets\nuru-user-secrets-demo\secrets.json`
   - **Linux/macOS**: `~/.microsoft/usersecrets/nuru-user-secrets-demo/secrets.json`

3. **Configuration priority** - User secrets override earlier configuration sources but are overridden by environment variables and command line args:
   1. appsettings.json
   2. appsettings.Development.json
   3. {AppName}.settings.json
   4. {AppName}.settings.Development.json
   5. **User secrets** ← You are here
   6. Environment variables
   7. Command line arguments

4. **No UserSecretsId?** - If you don't set a UserSecretsId, user secrets simply won't load. No error is thrown.

## Best Practices

- ✅ **Use for development only** - Never use user secrets in production
- ✅ **Keep secrets out of source control** - User secrets are stored outside your project directory
- ✅ **Generate unique GUIDs** - Use `uuidgen` or similar for the UserSecretsId
- ✅ **Document required secrets** - Create a template showing what secrets are needed
- ✅ **Use environment variables in CI/CD** - User secrets don't work in build pipelines
- ❌ **Don't commit the UserSecretsId** - It's okay to commit, but don't commit the actual secret values

## Troubleshooting

**Secrets not loading?**
- Check you're running in Development environment
- Verify the UserSecretsId matches between your runfile and `dotnet user-secrets` commands
- List secrets with `dotnet user-secrets list --id your-id-here`

**Where are my secrets stored?**
```bash
# Windows PowerShell
echo $env:APPDATA\Microsoft\UserSecrets\nuru-user-secrets-demo\secrets.json

# Linux/macOS
echo ~/.microsoft/usersecrets/nuru-user-secrets-demo/secrets.json
```

You can directly edit the `secrets.json` file if needed, but using `dotnet user-secrets` commands is recommended.
