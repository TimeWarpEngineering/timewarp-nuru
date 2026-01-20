#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
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
using TimeWarp.Nuru;
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

#region Bug #373 Workaround - Default to REPL mode when no args
// ========================================
// BUG #373: The ideal approach would be a default route that starts REPL:
//
// .Map("")
//   .WithHandler(async (NuruCoreApp app) => await app.RunReplAsync())
//   .WithDescription("Start interactive REPL mode (default when no args)")
//   .AsCommand()
//   .Done()
//
// This doesn't work yet due to generator issues:
// 1. Handler invoker emits wrong variable name for NuruCoreApp parameter
// 2. Double-await issue when handler body contains await
// 3. DSL interpreter ignores if statements, so conditional RunReplAsync isn't intercepted
//
// Workaround: Treat empty args as "-i" to leverage built-in REPL flag handling.
// See kanban task #373 for details.
// ========================================
string[] effectiveArgs = args.Length == 0 ? ["-i"] : args;
#endregion

try
{
  Log.Information("Starting TimeWarp.Nuru REPL Demo");

  WriteLine("TimeWarp.Nuru REPL Demo");
  WriteLine("========================");
  WriteLine("Debug logs: repl-debug.log");
  WriteLine();

  NuruApp app = NuruApp.CreateBuilder(args)
    .AddTypeConverter(new EnumTypeConverter<Environment>()) // Register enum converter
    .WithDescription("Interactive REPL demo showcasing Nuru route patterns.")

    // ========================================
    // SIMPLE COMMANDS (Literal only)
    // ========================================
    .Map("status")
      .WithHandler(() =>
      {
        Log.Information("Status command executed");
        WriteLine("System is running OK");
      })
      .WithDescription("Displays the current system status.")
      .AsQuery()
      .Done()
    .Map("time")
      .WithHandler(() =>
      {
        DateTime now = DateTime.Now;
        Log.Information("Time command executed at: {Time}", now);
        WriteLine($"Current time: {now:HH:mm:ss}");
      })
      .WithDescription("Displays the current time.")
      .AsQuery()
      .Done()

    // ========================================
    // BASIC PARAMETERS
    // ========================================

    .Map("greet {name}")
      .WithHandler((string name) =>
      {
        Log.Information("Greet command: {Name}", name);
        WriteLine($"Hello, {name}!");
      })
      .WithDescription("Greets the person with the specified name.")
      .AsCommand()
      .Done()
    .Map("add {a:int} {b:int}")
      .WithHandler((int a, int b) =>
      {
        Log.Information("Add: {A} + {B}", a, b);
        WriteLine($"{a} + {b} = {a + b}");
      })
      .WithDescription("Adds two integers.")
      .AsQuery()
      .Done()

    // ========================================
    // ENUM PARAMETERS
    // ========================================

    .Map("deploy {env:environment} {tag?}")
      .WithHandler((Environment env, string? tag) =>
      {
        Log.Information("Deploy: env={Env}, tag={Tag}", env, tag);
        if (tag is not null)
          WriteLine($"Deploying to {env} with tag {tag}");
        else
          WriteLine($"Deploying to {env} (latest)");
      })
      .WithDescription("Deploys to environment (dev, staging, prod) with optional tag.")
      .AsCommand()
      .Done()

    // ========================================
    // CATCH-ALL PARAMETERS
    // ========================================

    .Map("echo {*message}")
      .WithHandler((string[] message) =>
      {
        Log.Information("Echo: {Message}", string.Join(" ", message));
        WriteLine(string.Join(" ", message));
      })
      .WithDescription("Echoes all arguments back.")
      .AsQuery()
      .Done()

    // ========================================
    // SUBCOMMANDS (Hierarchical routes)
    // ========================================

    .Map("git status")
      .WithHandler(() =>
      {
        Log.Information("git status executed");
        WriteLine("On branch main");
        WriteLine("nothing to commit, working tree clean");
      })
      .WithDescription("Shows git working tree status.")
      .AsQuery()
      .Done()
    .Map("git commit -m {message}")
      .WithHandler((string message) =>
      {
        Log.Information("git commit: {Message}", message);
        WriteLine($"[main abc1234] {message}");
        WriteLine(" 1 file changed, 1 insertion(+)");
      })
      .WithDescription("Creates a commit with the specified message.")
      .AsCommand()
      .Done()
    .Map("git log --count {n:int}")
      .WithHandler((int n) =>
      {
        Log.Information("git log --count {N}", n);
        WriteLine($"Showing last {n} commits:");
        for (int i = 0; i < n && i < 5; i++)
        {
          WriteLine($"  {Guid.NewGuid().ToString()[..7]} - Commit message {i + 1}");
        }
      })
      .WithDescription("Shows the last N commits.")
      .AsQuery()
      .Done()

    // ========================================
    // BOOLEAN OPTIONS
    // ========================================

    .Map("build --verbose,-v")
      .WithHandler((bool verbose) =>
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
      })
      .WithDescription("Builds the project. Use -v for verbose output.")
      .AsCommand()
      .Done()

    // ========================================
    // OPTIONS WITH VALUES
    // ========================================

    .Map("search {query} --limit,-l {count:int?}")
      .WithHandler((string query, int? count) =>
      {
        int limit = count ?? 10;
        Log.Information("Search: query={Query}, limit={Limit}", query, limit);
        WriteLine($"Searching for '{query}' (limit: {limit})...");
        for (int i = 1; i <= Math.Min(limit, 3); i++)
        {
          WriteLine($"  {i}. Result matching '{query}'");
        }
      })
      .WithDescription("Searches with optional result limit.")
      .AsQuery()
      .Done()

    // ========================================
    // COMBINED OPTIONS
    // ========================================

    .Map("backup {source} --compress,-c --output,-o {dest?}")
      .WithHandler((string source, bool compress, string? dest) =>
      {
        string destination = dest ?? $"{source}.bak";
        Log.Information("Backup: source={Source}, compress={Compress}, dest={Dest}", source, compress, destination);
        WriteLine($"Backing up '{source}' to '{destination}'");
        if (compress)
          WriteLine("  Compression: enabled");
        WriteLine("Backup complete.");
      })
      .WithDescription("Backs up source with optional compression and destination.")
      .AsCommand()
      .Done()

    .AddRepl(options =>
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
    })
    .Build();

  Log.Information("Running command: {Args}", string.Join(" ", effectiveArgs));
  int exitCode = await app.RunAsync(effectiveArgs);
  Log.Information("Closing logger");
  Log.CloseAndFlush();
  return exitCode;
}
catch (Exception ex)
{
  Log.Fatal(ex, "REPL demo terminated unexpectedly");
  Log.CloseAndFlush();
  return 1;
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
