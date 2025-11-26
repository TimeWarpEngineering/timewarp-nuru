# Title User Secrets in TimeWarp.Nuru

User secrets now work in TimeWarp.Nuru even with runfiles! Keep API keys and connection strings out of source control during development.

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru@2.1.0-beta.28
#:property UserSecretsId=my-app-guid

NuruApp app =
  new NuruAppBuilder()
  .AddDependencyInjection()
  .AddConfiguration(args)  // ✅ Loads secrets in Development
  .Map("test", (IConfiguration config) =>
    Console.WriteLine(config["ApiKey"]))
  .Build();
```

Set secrets with:
```bash
dotnet user-secrets set "ApiKey" "secret-123" --id my-app-guid
```

Auto-loads in Development. Never loads in Production. Standard .NET behavior.

Requires version 2.1.0-beta.28 or greater.

[TimeWarp.State repo](https://github.com/TimeWarpEngineering/timewarp-nuru)