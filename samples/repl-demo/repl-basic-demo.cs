#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator
#:package Serilog
#:package Serilog.Sinks.File
#:package Serilog.Extensions.Logging

// ═══════════════════════════════════════════════════════════════════════════════
// REPL BASIC DEMO - ROUTE PATTERN EXAMPLES
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) which provides:
// - Full DI container setup
// - Configuration support
// - Auto-help generation
// - REPL support with tab completion
// - All extensions enabled by default
//
// Demonstrates various route pattern types supported by Nuru:
// - Literal commands (status, time)
// - Subcommands (git status, git commit, git log)
// - Required parameters (greet {name})
// - Optional parameters (deploy {env} {tag?})
// - Typed parameters (add {a:int} {b:int})
// - Enum parameters (deploy {env:environment})
// - Catch-all parameters (echo {*message})
// - Boolean options (build --verbose, build -v)
// - Options with values (search --limit {n})
// - Short aliases (--verbose,-v)
// - Combined options (backup --compress --output {dest})
//
// Supports both CLI and REPL modes:
//   ./repl-basic-demo.cs greet Alice       - CLI mode (single command)
//   ./repl-basic-demo.cs --interactive     - Enter REPL mode
//   ./repl-basic-demo.cs -i                - Enter REPL mode (short form)
//
// Debug logs written to: repl-debug.log
// ============================================================================

using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using static System.Console;

// Configure Serilog with filtered logging for REPL debugging only
Log.Logger = new LoggerConfiguration()
  .MinimumLevel.Verbose()
  .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
  .Filter.ByExcluding(e =>
  {
    string message = e.RenderMessage();
    return message.Contains("Registering route:") ||
           message.Contains("Starting lexical analysis") ||
           message.Contains("Lexical analysis complete") ||
           message.Contains("Parsing pattern:") ||
           message.Contains("AST:") ||
           message.Contains("Checking route:") ||
           message.Contains("Failed to match") ||
           message.Contains("Route") && message.Contains("failed at") ||
           message.Contains("Tokens:") ||
           message.Contains("Setting boolean option parameter") ||
           message.Contains("Optional boolean option") ||
           message.Contains("Positional matching") ||
           message.Contains("Resolving command:");
  })
  .WriteTo.File
  (
    path: "repl-debug.log",
    rollingInterval: RollingInterval.Day,
    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}",
    retainedFileCountLimit: 7
  )
  .Enrich.FromLogContext()
  .CreateLogger();

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
  builder.AddSerilog(Log.Logger);
});

try
{
  Log.Information("Starting TimeWarp.Nuru REPL Demo");

  WriteLine("TimeWarp.Nuru REPL Demo");
  WriteLine("========================");
  WriteLine("Debug logs: repl-debug.log");
  WriteLine();

  NuruAppOptions nuruAppOptions = new()
  {
    ConfigureRepl = options =>
    {
      options.Prompt = "demo> ";
      options.WelcomeMessage =
        "Welcome to the Nuru REPL Demo!\n" +
        "\n" +
        "SIMPLE COMMANDS:\n" +
        "  status              - Show system status\n" +
        "  time                - Show current time\n" +
        "\n" +
        "PARAMETERS:\n" +
        "  greet Alice         - Basic parameter\n" +
        "  add 5 3             - Typed parameters (int)\n" +
        "  deploy dev          - Enum param (dev/staging/prod)\n" +
        "  deploy prod v1.2    - Enum with optional tag\n" +
        "  echo hello world    - Catch-all parameter\n" +
        "\n" +
        "SUBCOMMANDS:\n" +
        "  git status          - Literal subcommand\n" +
        "  git commit -m \"fix\" - Short option with value\n" +
        "  git log --count 3   - Long option with typed value\n" +
        "\n" +
        "OPTIONS:\n" +
        "  build               - Without verbose\n" +
        "  build -v            - With verbose (short)\n" +
        "  build --verbose     - With verbose (long)\n" +
        "  search foo          - Default limit\n" +
        "  search foo -l 5     - Custom limit\n" +
        "  backup data         - Basic backup\n" +
        "  backup data -c      - With compression\n" +
        "  backup data -c -o x - With compression and dest\n" +
        "\n" +
        "Type 'help' for all commands, 'exit' to quit.";
      options.GoodbyeMessage = "Thanks for trying the REPL demo!";
      options.PersistHistory = false;
      Log.Information("REPL configured");
    }
  };

  NuruCoreApp app = NuruApp.CreateBuilder(args, nuruAppOptions)
    .ConfigureServices(services => services.AddMediator())
    .UseLogging(loggerFactory)
    .AddTypeConverter(new EnumTypeConverter<Environment>()) // Register enum converter
    .WithMetadata
    (
      description: "Interactive REPL demo showcasing Nuru route patterns."
    )

    // ========================================
    // SIMPLE COMMANDS (Literal only)
    // ========================================

    .Map
    (
      pattern: "status",
      handler: () =>
      {
        Log.Information("Status command executed");
        WriteLine("System is running OK");
      },
      description: "Displays the current system status."
    )
    .Map
    (
      pattern: "time",
      handler: () =>
      {
        DateTime now = DateTime.Now;
        Log.Information("Time command executed at: {Time}", now);
        WriteLine($"Current time: {now:HH:mm:ss}");
      },
      description: "Displays the current time."
    )

    // ========================================
    // BASIC PARAMETERS
    // ========================================

    .Map
    (
      pattern: "greet {name}",
      handler: (string name) =>
      {
        Log.Information("Greet command: {Name}", name);
        WriteLine($"Hello, {name}!");
      },
      description: "Greets the person with the specified name."
    )
    .Map
    (
      pattern: "add {a:int} {b:int}",
      handler: (int a, int b) =>
      {
        Log.Information("Add: {A} + {B}", a, b);
        WriteLine($"{a} + {b} = {a + b}");
      },
      description: "Adds two integers."
    )

    // ========================================
    // ENUM PARAMETERS
    // ========================================

    .Map
    (
      pattern: "deploy {env:environment} {tag?}",
      handler: (Environment env, string? tag) =>
      {
        Log.Information("Deploy: env={Env}, tag={Tag}", env, tag);
        if (tag is not null)
          WriteLine($"Deploying to {env} with tag {tag}");
        else
          WriteLine($"Deploying to {env} (latest)");
      },
      description: "Deploys to environment (dev, staging, prod) with optional tag."
    )

    // ========================================
    // CATCH-ALL PARAMETERS
    // ========================================

    .Map
    (
      pattern: "echo {*message}",
      handler: (string[] message) =>
      {
        Log.Information("Echo: {Message}", string.Join(" ", message));
        WriteLine(string.Join(" ", message));
      },
      description: "Echoes all arguments back."
    )

    // ========================================
    // SUBCOMMANDS (Hierarchical routes)
    // ========================================

    .Map
    (
      pattern: "git status",
      handler: () =>
      {
        Log.Information("git status executed");
        WriteLine("On branch main");
        WriteLine("nothing to commit, working tree clean");
      },
      description: "Shows git working tree status."
    )
    .Map
    (
      pattern: "git commit -m {message}",
      handler: (string message) =>
      {
        Log.Information("git commit: {Message}", message);
        WriteLine($"[main abc1234] {message}");
        WriteLine(" 1 file changed, 1 insertion(+)");
      },
      description: "Creates a commit with the specified message."
    )
    .Map
    (
      pattern: "git log --count {n:int}",
      handler: (int n) =>
      {
        Log.Information("git log --count {N}", n);
        WriteLine($"Showing last {n} commits:");
        for (int i = 0; i < n && i < 5; i++)
        {
          WriteLine($"  {Guid.NewGuid().ToString()[..7]} - Commit message {i + 1}");
        }
      },
      description: "Shows the last N commits."
    )

    // ========================================
    // BOOLEAN OPTIONS
    // ========================================

    .Map
    (
      pattern: "build --verbose,-v",
      handler: (bool verbose) =>
      {
        Log.Information("Build: verbose={Verbose}", verbose);
        if (verbose)
        {
          WriteLine("Building project...");
          WriteLine("  Compiling src/main.cs");
          WriteLine("  Compiling src/utils.cs");
          WriteLine("  Linking...");
          WriteLine("Build succeeded.");
        }
        else
        {
          WriteLine("Build succeeded.");
        }
      },
      description: "Builds the project. Use -v for verbose output."
    )

    // ========================================
    // OPTIONS WITH VALUES
    // ========================================

    .Map
    (
      pattern: "search {query} --limit,-l {count:int?}",
      handler: (string query, int? count) =>
      {
        int limit = count ?? 10;
        Log.Information("Search: query={Query}, limit={Limit}", query, limit);
        WriteLine($"Searching for '{query}' (limit: {limit})...");
        for (int i = 1; i <= Math.Min(limit, 3); i++)
        {
          WriteLine($"  {i}. Result matching '{query}'");
        }
      },
      description: "Searches with optional result limit."
    )

    // ========================================
    // COMBINED OPTIONS
    // ========================================

    .Map
    (
      pattern: "backup {source} --compress,-c --output,-o {dest?}",
      handler: (string source, bool compress, string? dest) =>
      {
        string destination = dest ?? $"{source}.bak";
        Log.Information("Backup: source={Source}, compress={Compress}, dest={Dest}", source, compress, destination);
        WriteLine($"Backing up '{source}' to '{destination}'");
        if (compress)
          WriteLine("  Compression: enabled");
        WriteLine("Backup complete.");
      },
      description: "Backs up source with optional compression and destination."
    )

    .Build();

  // If no args or --interactive/-i, enter REPL mode
  // Otherwise execute the command and exit
  if (args.Length == 0)
  {
    Log.Information("No args - starting REPL mode");
    return await app.RunReplAsync();
  }

  Log.Information("Running command: {Args}", string.Join(" ", args));
  return await app.RunAsync(args);
}
catch (Exception ex)
{
  Log.Fatal(ex, "REPL demo terminated unexpectedly");
  return 1;
}
finally
{
  Log.Information("Closing logger");
  Log.CloseAndFlush();
}

// ============================================================================
// Enum for deploy command - demonstrates enum type conversion
// ============================================================================
public enum Environment
{
  Dev,
  Staging,
  Prod
}
