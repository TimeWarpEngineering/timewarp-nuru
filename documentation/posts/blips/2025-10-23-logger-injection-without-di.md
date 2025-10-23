Inject ILoggerFactory into delegates. No DI container needed.

```csharp
.UseConsoleLogging()
.AddRoute("process {file}", (string file, ILoggerFactory loggerFactory) =>
{
  ILogger logger = loggerFactory.CreateLogger("Processor");
  logger.LogInformation("Processing: {File}", file);
})
```

dotnet add package TimeWarp.Nuru --version 2.1.0-beta.22