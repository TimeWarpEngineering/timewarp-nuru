TimeWarp.Nuru AddConfiguration() auto-discovers both appsettings.json and application-specific config files (e.g., myapp.settings.json) for runfiles and published apps.

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru@2.1.0-beta.27

NuruApp app =
  new NuruAppBuilder()
  .AddDependencyInjection()
  .AddConfiguration(args)  // Auto-discovers config files
  .AddRoute("test", () => Console.WriteLine(app.Configuration["ApiKey"]))
  .Build();

await app.RunAsync(args);
```

Smart fallback: Assembly dir → AppContext (runfiles) → CallerFilePath → Current dir.

Requires version 2.1.0-beta.27 or greater.
