Inject ILogger<T> into delegates. No DI container needed.

```csharp
NuruApp app =
  new NuruAppBuilder()
  .UseConsoleLogging()
  .AddRoute
  (
    "process {file}",
    (string file, ILogger<Program> logger) =>
    {
      logger.LogInformation("Processing: {File}", file);
    }
  )
  .Build();

await app.RunAsync(args);
```

Zero overhead if you don't use logging. ~24 bytes if you do.

dotnet add package TimeWarp.Nuru --version 2.1.0-beta.22