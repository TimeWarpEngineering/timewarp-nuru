#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// REPLOPTIONS COMPREHENSIVE SHOWCASE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample uses NuruApp.CreateBuilder(args) which provides all REPL features.
//
// This demo demonstrates ALL ReplOptions configuration properties.
// Run this file to explore each feature interactively.
//
// Features demonstrated:
// - PersistHistory: Saves command history to file between sessions
// - HistoryFilePath: Custom location for history file
// - MaxHistorySize: Limits number of commands stored in history
// - ShowExitCode: Displays exit code after each command
// - PromptColor: Custom ANSI color for the prompt
// - ContinueOnError: Whether REPL exits on command failure
// - HistoryIgnorePatterns: Excludes sensitive commands from history
// - ShowTiming: Displays execution time for commands
// - EnableArrowHistory: Arrow key navigation through history
// - EnableColors: Toggle colored output
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using static System.Console;

WriteLine("ReplOptions Comprehensive Showcase");
WriteLine("===================================");
WriteLine();
WriteLine("This demo showcases ALL ReplOptions configuration features.");
WriteLine("Pay attention to:");
WriteLine("  - Cyan prompt color (not the default green)");
WriteLine("  - Exit codes shown after each command (ShowExitCode=true)");
WriteLine("  - REPL stops on error (ContinueOnError=false)");
WriteLine("  - History limited to 50 entries (MaxHistorySize=50)");
WriteLine("  - Custom history file location");
WriteLine("  - Sensitive commands excluded from history");
WriteLine();

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .WithDescription("ReplOptions comprehensive showcase demonstrating all configuration features.")

  // --------------------------------------------------------
  // Success command - completes without error
  // Demonstrates: Normal successful command execution
  // --------------------------------------------------------
  .Map("success")
    .WithHandler(() => WriteLine("Command completed successfully!"))
    .WithDescription("Executes successfully (exit code 0).")
    .AsQuery()
    .Done()

  // --------------------------------------------------------
  // Fail command - throws exception for non-zero exit code
  // Demonstrates: ContinueOnError=false will stop REPL
  // NOTE: In Nuru, throw an exception to signal failure.
  // Handler return values are OUTPUT, not exit codes.
  // --------------------------------------------------------
  .Map("fail")
    .WithHandler(() =>
    {
      WriteLine("This command intentionally fails to demonstrate ContinueOnError=false");
      throw new InvalidOperationException("Intentional failure - REPL will exit because ContinueOnError is disabled.");
    })
    .WithDescription("Intentionally fails (throws exception) to demonstrate ContinueOnError behavior.")
    .AsCommand()
    .Done()

  // --------------------------------------------------------
  // Exit code demo - shows that return values are OUTPUT, not exit codes
  // Demonstrates: Handler return values are written to terminal
  // --------------------------------------------------------
  .Map("output {value:int}")
    .WithHandler((int value) =>
    {
      WriteLine($"Handler returning {value} - this will be written as output, NOT as exit code.");
      return value;
    })
    .WithDescription("Outputs the specified value to demonstrate that return values are output, not exit codes.")
    .AsQuery()
    .Done()

  // --------------------------------------------------------
  // Password command - excluded from history
  // Demonstrates: HistoryIgnorePatterns feature
  // --------------------------------------------------------
  .Map("set-password {value}")
    .WithHandler((string value) =>
    {
      WriteLine($"Password set (not really - this is a demo).");
      WriteLine("NOTE: This command will NOT appear in history due to HistoryIgnorePatterns.");
      WriteLine("Try 'history' command to verify it's not recorded.");
    })
    .WithDescription("Simulates setting a password - excluded from history by pattern.")
    .AsIdempotentCommand()
    .Done()

  // --------------------------------------------------------
  // Token command - excluded from history
  // Demonstrates: HistoryIgnorePatterns feature
  // --------------------------------------------------------
  .Map("set-token {value}")
    .WithHandler((string value) =>
    {
      WriteLine($"Token configured (not really - this is a demo).");
      WriteLine("NOTE: This command will NOT appear in history due to *token* pattern.");
    })
    .WithDescription("Simulates setting a token - excluded from history by pattern.")
    .AsIdempotentCommand()
    .Done()

  // --------------------------------------------------------
  // Secret command - excluded from history
  // Demonstrates: HistoryIgnorePatterns feature
  // --------------------------------------------------------
  .Map("my-secret-command")
    .WithHandler(() =>
    {
      WriteLine("This is a secret command!");
      WriteLine("NOTE: This command will NOT appear in history due to *secret* pattern.");
    })
    .WithDescription("A secret command - excluded from history by pattern.")
    .AsQuery()
    .Done()

  // --------------------------------------------------------
  // Long running command
  // Demonstrates: ShowTiming feature
  // --------------------------------------------------------
  .Map("slow {ms:int}")
    .WithHandler(async (int ms) =>
    {
      WriteLine($"Sleeping for {ms}ms to demonstrate ShowTiming feature...");
      await Task.Delay(ms);
      WriteLine("Done! Check the timing display above.");
    })
    .WithDescription("Delays for specified milliseconds to demonstrate ShowTiming.")
    .AsQuery()
    .Done()

  // --------------------------------------------------------
  // Echo command - useful for testing
  // --------------------------------------------------------
  .Map("echo {*message}")
    .WithHandler((string[] message) =>
    {
      WriteLine(string.Join(" ", message));
    })
    .WithDescription("Echoes the message back. Useful for testing.")
    .AsQuery()
    .Done()

  // --------------------------------------------------------
  // Info command - shows current configuration
  // --------------------------------------------------------
  .Map("config")
    .WithHandler(() =>
    {
      WriteLine("Current ReplOptions Configuration:");
      WriteLine("-----------------------------------");
      WriteLine("Prompt:               'showcase> ' (custom)");
      WriteLine("PromptColor:          Cyan (\\x1b[36m)");
      WriteLine("PersistHistory:       true");
      WriteLine("HistoryFilePath:      ./repl-showcase-history.txt");
      WriteLine("MaxHistorySize:       50");
      WriteLine("ShowExitCode:         true");
      WriteLine("ContinueOnError:      false (REPL stops on failure)");
      WriteLine("ShowTiming:           true");
      WriteLine("EnableColors:         true");
      WriteLine("EnableArrowHistory:   true");
      WriteLine("KeyBindingProfileName: Default");
      WriteLine();
      WriteLine("HistoryIgnorePatterns:");
      WriteLine("  - *password*");
      WriteLine("  - *secret*");
      WriteLine("  - *token*");
      WriteLine("  - *apikey*");
      WriteLine("  - *credential*");
      WriteLine("  - clear-history");
    })
    .WithDescription("Displays the current ReplOptions configuration.")
    .AsQuery()
    .Done()

  // --------------------------------------------------------
  // REPL configuration - must be last before Build()
  // --------------------------------------------------------
  .AddRepl(options =>
  {
    // ========================================
    // Prompt Customization
    // ========================================

    // Custom prompt text (default is "> ")
    options.Prompt = "showcase> ";

    // Custom prompt color: Cyan instead of default Green
    // Common ANSI codes:
    //   "\x1b[31m" = Red
    //   "\x1b[32m" = Green (default)
    //   "\x1b[33m" = Yellow
    //   "\x1b[34m" = Blue
    //   "\x1b[35m" = Magenta
    //   "\x1b[36m" = Cyan
    options.PromptColor = "\x1b[36m"; // Cyan

    // Enable colored output (default is true)
    options.EnableColors = true;

    // ========================================
    // Welcome and Goodbye Messages
    // ========================================

    options.WelcomeMessage =
      "ReplOptions Showcase - Demonstrating ALL configuration features!\n" +
      "Try these commands:\n" +
      "  config         - View current configuration\n" +
      "  success        - Command that succeeds (exit code 0)\n" +
      "  fail           - Command that fails (REPL will exit!)\n" +
      "  output 42      - Return custom exit code\n" +
      "  slow 500       - Demonstrate timing display\n" +
      "  set-password x - Excluded from history\n" +
      "  history        - View command history";

    options.GoodbyeMessage = "Thanks for exploring ReplOptions! Check ./repl-showcase-history.txt for persisted history.";

    // ========================================
    // History Configuration
    // ========================================

    // Enable history persistence (saves commands between sessions)
    options.PersistHistory = true;

    // Custom history file location (default is ~/.nuru_history)
    // Using local file for demo so users can easily inspect it
    options.HistoryFilePath = "./repl-showcase-history.txt";

    // Limit history to 50 entries (default is 1000)
    // Useful for memory-constrained environments or privacy
    options.MaxHistorySize = 50;

    // Enable arrow key navigation through history (default is true)
    options.EnableArrowHistory = true;

    // ========================================
    // Error Handling
    // ========================================

    // IMPORTANT: Setting this to false means REPL will EXIT
    // when any command returns a non-zero exit code.
    // Default is true (continue running on error).
    // Try running 'fail' command to see this behavior!
    options.ContinueOnError = false;

    // ========================================
    // Display Options
    // ========================================

    // Show exit code after each command (default is false)
    // Displays "[Exit code: X]" after command execution
    options.ShowExitCode = true;

    // Show execution time for commands (default is true)
    // Displays timing information after command execution
    options.ShowTiming = true;

    // ========================================
    // Key Binding Profile
    // ========================================

    // Choose a key binding profile for the REPL (default is "Default")
    // Available profiles: "Default", "Emacs", "Vi", "VSCode"
    // - Default: Standard readline-style bindings
    // - Emacs: GNU Readline/Bash-style (Ctrl+A/E, Ctrl+F/B)
    // - Vi: Vi-inspired insert mode bindings (Ctrl+W, Ctrl+U)
    // - VSCode: Modern IDE-style (Ctrl+Arrow for word movement)
    options.KeyBindingProfileName = "Default";
  })
  .Build();

// Start REPL mode
await app.RunReplAsync();
