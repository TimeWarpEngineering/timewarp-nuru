# Output Handling

TimeWarp.Nuru gives you full control over console output, separating diagnostic messages from data output for better composability and scripting.

## Console Streams

### stdout (Standard Output)
Use for **data and results** that other programs might consume:

```csharp
// Simple text output
builder.Map("hello", () => Console.WriteLine("Hello, World!"));

// Structured data (automatic JSON serialization)
builder.Map("info", () => new {
    Name = "MyApp",
    Version = "1.0.0",
    Status = "Running"
});
```

### stderr (Standard Error)
Use for **diagnostics, progress, and errors** that humans read:

```csharp
builder.Map
(
  "process {file}",
  (string file) =>
  {
    Console.Error.WriteLine($"Processing {file}...");  // Progress → stderr
    Thread.Sleep(1000);
    Console.Error.WriteLine("Complete!");              // Status → stderr

    return new { File = file, Lines = 42 };             // Data → stdout
  }
);
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

Return objects from handlers for automatic JSON output:

```csharp
public record AnalysisResult(
    string File,
    int Lines,
    int Errors,
    string Status
);

builder.Map
(
  "analyze {file}",
  (string file) =>
  {
    Console.Error.WriteLine($"Analyzing {file}...");

    AnalysisResult result = AnalyzeFile(file);

    // Returned object → JSON to stdout
    return new AnalysisResult
    (
      File: file,
      Lines: result.LineCount,
      Errors: result.ErrorCount,
      Status: "Complete"
    );
  }
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
using TimeWarp.Nuru;
using TimeWarp.Nuru.Logging;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .UseConsoleLogging()  // Logs → stderr
  .ConfigureServices(services =>
  {
    services.AddScoped<IAnalyzer, Analyzer>();
  })
  .Map<AnalyzeCommand>("analyze {path}")
  .Build();

return await app.RunAsync(args);

public sealed class AnalyzeCommand : IRequest<AnalysisResult>
{
    public string Path { get; set; }

    public sealed class Handler(
      IAnalyzer analyzer,
      ILogger<Handler> logger) : IRequestHandler<AnalyzeCommand, AnalysisResult>
    {
      public async Task<AnalysisResult> Handle
      (
        AnalyzeCommand cmd,
        CancellationToken ct
      )
      {
        // Structured logging → stderr
        logger.LogInformation("Starting analysis of {Path}", cmd.Path);

        AnalysisResult result = await analyzer.AnalyzeAsync(cmd.Path);

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
builder.Map
(
  "download {url}",
  async (string url) =>
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

    return new { Url = url, Size = data.Length, Status = "Success" };  // stdout
  }
);
```

### Error Reporting

```csharp
builder.Map
(
  "validate {file}",
  (string file) =>
  {
    Console.Error.WriteLine($"Validating {file}...");

    List<ValidationError> errors = Validate(file);

    if (errors.Any())
    {
      Console.Error.WriteLine($"❌ Found {errors.Count} errors");
      foreach (ValidationError error in errors)
      {
        Console.Error.WriteLine($"  Line {error.Line}: {error.Message}");
      }
      return 1;  // Exit code for errors
    }

    Console.Error.WriteLine("✅ Validation passed");
    return new { File = file, Status = "Valid" };
  }
);
```

### Multi-Step Operations

```csharp
builder.Map
(
  "deploy {env}",
  async (string env) =>
  {
    Console.Error.WriteLine($"Deploying to {env}...");

    Console.Error.WriteLine("Step 1/3: Building...");
    await BuildAsync();

    Console.Error.WriteLine("Step 2/3: Running tests...");
    await TestAsync();

    Console.Error.WriteLine("Step 3/3: Uploading...");
    DeploymentResult result = await UploadAsync(env);

    Console.Error.WriteLine("✅ Deployment complete");

    return new
    {
      Environment = env,
      Version = result.Version,
      Url = result.Url,
      DeployedAt = DateTime.UtcNow
    };
  }
);
```

## Output Control

### Verbose Mode

```csharp
builder.Map
(
  "process {file} --verbose",
  (string file, bool verbose) =>
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
  }
);
```

### Quiet Mode

```csharp
builder.Map
(
  "backup {source} --quiet",
  (string source, bool quiet) =>
  {
    if (!quiet)
      Console.Error.WriteLine($"Backing up {source}...");

    BackupResult result = Backup(source);

    if (!quiet)
      Console.Error.WriteLine("Backup complete");

    return result;
  }
);
```

## Best Practices

### ✅ DO

```csharp
// Progress and diagnostics → stderr
Console.Error.WriteLine("Processing...");
logger.LogInformation("Started at {Time}", DateTime.Now);

// Data and results → stdout (or return objects)
Console.WriteLine(jsonData);
return new { Result = data };
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

Return exit codes for shell scripting:

```csharp
builder.Map
(
  "check {file}",
  (string file) =>
  {
    if (!File.Exists(file))
    {
      Console.Error.WriteLine($"❌ File not found: {file}");
      return 1;  // Error exit code
    }

    Console.Error.WriteLine("✅ File exists");
    return 0;  // Success
  }
);
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
