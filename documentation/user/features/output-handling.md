# Output Handling

TimeWarp.Nuru gives you full control over console output, separating diagnostic messages from data output for better composability and scripting.

## Console Streams

### stdout (Standard Output)
Use for **data and results** that other programs might consume:

```csharp
// Simple text output
builder.Map("hello")
  .WithHandler(() => Console.WriteLine("Hello, World!"))
  .AsQuery()
  .Done();

// Structured data (automatic JSON serialization)
builder.Map("info")
  .WithHandler(() => new AppInfo("MyApp", "1.0.0", "Running"))
  .AsQuery()
  .Done();

public sealed record AppInfo(string Name, string Version, string Status);
```

### stderr (Standard Error)
Use for **diagnostics, progress, and errors** that humans read:

```csharp
builder.Map("process {file}")
  .WithHandler((string file) =>
  {
    Console.Error.WriteLine($"Processing {file}...");  // Progress → stderr
    Thread.Sleep(1000);
    Console.Error.WriteLine("Complete!");              // Status → stderr

    return new FileSummary(file, 42);                  // Data → stdout
  })
  .AsCommand()
  .Done();

public sealed record FileSummary(string File, int Lines);
```

## Why Separate Streams?

This separation enables powerful scripting patterns:

```bash
# Capture only data, ignore progress messages
./myapp process data.csv > result.json

# Pipe data to another tool
./myapp analyze data.csv | jq '.summary'

# Save data and errors separately
./myapp process file.txt > data.json 2> errors.log

# Show only progress, discard data
./myapp process file.txt > /dev/null
```

## Automatic JSON Serialization

Return objects from handlers for automatic JSON output. Return a named type (a record or class); the generated JSON context cannot name an anonymous type, so `new { ... }` does not build:

```csharp
builder.Map("analyze {file}")
  .WithHandler((string file) =>
  {
    Console.Error.WriteLine($"Analyzing {file}...");

    (int lines, int errors) = AnalyzeFile(file);

    // Returned object → JSON to stdout
    return new AnalysisResult(file, lines, errors, "Complete");
  })
  .AsQuery()
  .Done();

public sealed record AnalysisResult(
  string File,
  int Lines,
  int Errors,
  string Status
);
```

```bash
./myapp analyze data.csv
```

**stderr (human sees this):**
```
Analyzing data.csv...
```

**stdout (for machines):**
```json
{
  "file": "data.csv",
  "lines": 1000,
  "errors": 3,
  "status": "Complete"
}
```

## With Dependency Injection

Structured logging goes to stderr, results to stdout:

```csharp
using TimeWarp.Mediator;
using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    // Logs → stderr
    services.AddLogging(logging => logging
      .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace));
    services.AddScoped<IAnalyzer, Analyzer>();
  })
  .Map<AnalyzeQuery>()
  .Build();

return await app.RunAsync(args);

[NuruRoute("analyze", Description = "Analyze a path")]
public sealed class AnalyzeQuery : IQuery<AnalysisResult>
{
  [Parameter] public string Path { get; set; } = "";

  public sealed class Handler(IAnalyzer analyzer, ILogger<AnalyzeQuery> logger)
    : IRequestHandler<AnalyzeQuery, AnalysisResult>
  {
    public async ValueTask<AnalysisResult> Handle(AnalyzeQuery query, CancellationToken cancellationToken)
    {
      // Structured logging → stderr
      logger.LogInformation("Starting analysis of {Path}", query.Path);

      AnalysisResult result = await analyzer.AnalyzeAsync(query.Path);

      logger.LogInformation("Analysis complete. Found {Count} issues", result.IssueCount);

      // Returned object → JSON to stdout
      return result;
    }
  }
}
```

## Common Patterns

### Progress Reporting

```csharp
builder.Map("download {url}")
  .WithHandler(async (string url) =>
  {
    Console.Error.WriteLine($"Downloading {url}...");

    byte[] data = await DownloadAsync
    (
      url,
      progress =>
      {
        Console.Error.WriteLine($"Progress: {progress}%");  // stderr
      }
    );

    Console.Error.WriteLine("Download complete!");

    return new DownloadResult(url, data.Length, "Success");  // stdout
  })
  .AsCommand()
  .Done();

public sealed record DownloadResult(string Url, int Size, string Status);
```

### Error Reporting

```csharp
builder.Map("validate {file}")
  .WithHandler((string file) =>
  {
    Console.Error.WriteLine($"Validating {file}...");

    List<ValidationError> errors = Validate(file);

    if (errors.Count > 0)
    {
      Console.Error.WriteLine($"❌ Found {errors.Count} errors");
      foreach (ValidationError error in errors)
      {
        Console.Error.WriteLine($"  Line {error.Line}: {error.Message}");
      }

      Environment.ExitCode = 1;  // Exit code for errors
      return;
    }

    Console.Error.WriteLine("✅ Validation passed");
    Console.WriteLine(file);     // Data → stdout
  })
  .AsQuery()
  .Done();
```

### Multi-Step Operations

```csharp
builder.Map("deploy {env}")
  .WithHandler(async (string env) =>
  {
    Console.Error.WriteLine($"Deploying to {env}...");

    Console.Error.WriteLine("Step 1/3: Building...");
    await BuildAsync();

    Console.Error.WriteLine("Step 2/3: Running tests...");
    await TestAsync();

    Console.Error.WriteLine("Step 3/3: Uploading...");
    DeploymentResult result = await UploadAsync(env);

    Console.Error.WriteLine("✅ Deployment complete");

    return new DeploymentSummary(env, result.Version, result.Url, DateTime.UtcNow);
  })
  .AsCommand()
  .Done();

public sealed record DeploymentSummary(string Environment, string Version, string Url, DateTime DeployedAt);
```

## Output Control

### Verbose Mode

```csharp
builder.Map("process {file} --verbose")
  .WithHandler((string file, bool verbose) =>
  {
    if (verbose)
      Console.Error.WriteLine("Verbose mode enabled");

    Console.Error.WriteLine($"Processing {file}...");

    ProcessResult result = Process
    (
      file,
      (step) =>
      {
        if (verbose)
          Console.Error.WriteLine($"  {step}");
      }
    );

    return result;
  })
  .AsCommand()
  .Done();
```

### Quiet Mode

```csharp
builder.Map("backup {source} --quiet")
  .WithHandler((string source, bool quiet) =>
  {
    if (!quiet)
      Console.Error.WriteLine($"Backing up {source}...");

    BackupResult result = Backup(source);

    if (!quiet)
      Console.Error.WriteLine("Backup complete");

    return result;
  })
  .AsCommand()
  .Done();
```

## Best Practices

### ✅ DO

```csharp
// Progress and diagnostics → stderr
Console.Error.WriteLine("Processing...");
logger.LogInformation("Started at {Time}", DateTime.Now);

// Data and results → stdout (or return a record; it is written as JSON)
Console.WriteLine(jsonData);
return new ExportResult(data);
```

### ❌ DON'T

```csharp
// Don't mix data with progress on stdout
Console.WriteLine("Processing...");  // Bad: progress on stdout
Console.WriteLine(jsonData);          // Now stdout has mixed content

// Don't put results on stderr
Console.Error.WriteLine(jsonData);  // Bad: data on stderr
```

## Scripting Examples

### Pipe to jq

```bash
./myapp analyze data.csv | jq '.summary'
./myapp list --format json | jq '.[] | select(.status == "active")'
```

### Save Data, Show Progress

```bash
./myapp process large-file.dat > result.json
# Progress appears on console, data saved to file
```

### Chain Commands

```bash
./myapp extract data.zip | ./myapp transform | ./myapp load
```

### Combine with Traditional Tools

```bash
./myapp query "SELECT * FROM users" | grep "admin" | wc -l
```

## Exit Codes

Set `Environment.ExitCode` for shell scripting. A returned `int` is printed to stdout; it is not the exit code:

```csharp
builder.Map("check {file}")
  .WithHandler((string file) =>
  {
    if (!File.Exists(file))
    {
      Console.Error.WriteLine($"❌ File not found: {file}");
      Environment.ExitCode = 1;  // Error exit code
      return;
    }

    Console.Error.WriteLine("✅ File exists");  // Exit code stays 0
  })
  .AsQuery()
  .Done();
```

```bash
if ./myapp check important.txt; then
    echo "File OK"
else
    echo "File missing"
fi
```

## Related Documentation

- **[Logging](logging.md)** - Structured logging configuration
- **[Getting Started](../getting-started.md)** - Basic examples
- **[Use Cases](../use-cases.md)** - Real-world patterns
