#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

// ============================================================================
// ReplOptions Comprehensive Showcase
// ============================================================================
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
// ============================================================================

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
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

var app = new NuruAppBuilder()
  .WithMetadata
  (
    description: "ReplOptions comprehensive showcase demonstrating all configuration features."
  )

  // --------------------------------------------------------
  // Success command - returns exit code 0
  // Demonstrates: Normal successful command execution
  // --------------------------------------------------------
  .AddRoute
  (
    pattern: "success",
    handler: () =>
    {
      WriteLine("Command completed successfully!");
      return 0;
    },
    description: "Executes successfully with exit code 0."
  )

  // --------------------------------------------------------
  // Fail command - returns non-zero exit code
  // Demonstrates: ContinueOnError=false will stop REPL
  // --------------------------------------------------------
  .AddRoute
  (
    pattern: "fail",
    handler: () =>
    {
      WriteLine("This command intentionally fails to demonstrate ContinueOnError=false");
      WriteLine("The REPL will exit after this command because ContinueOnError is disabled.");
      return 1;
    },
    description: "Intentionally fails with exit code 1 to demonstrate ContinueOnError behavior."
  )

  // --------------------------------------------------------
  // Custom exit code command
  // Demonstrates: ShowExitCode feature with various codes
  // --------------------------------------------------------
  .AddRoute
  (
    pattern: "exitcode {code:int}",
    handler: (int code) =>
    {
      WriteLine($"Returning exit code: {code}");
      WriteLine($"Watch for '[Exit code: {code}]' displayed by ShowExitCode feature.");
      return code;
    },
    description: "Returns the specified exit code to demonstrate ShowExitCode display."
  )

  // --------------------------------------------------------
  // Password command - excluded from history
  // Demonstrates: HistoryIgnorePatterns feature
  // --------------------------------------------------------
  .AddRoute
  (
    pattern: "set-password {value}",
    handler: (string value) =>
    {
      WriteLine($"Password set (not really - this is a demo).");
      WriteLine("NOTE: This command will NOT appear in history due to HistoryIgnorePatterns.");
      WriteLine("Try 'history' command to verify it's not recorded.");
    },
    description: "Simulates setting a password - excluded from history by pattern."
  )

  // --------------------------------------------------------
  // Token command - excluded from history
  // Demonstrates: HistoryIgnorePatterns feature
  // --------------------------------------------------------
  .AddRoute
  (
    pattern: "set-token {value}",
    handler: (string value) =>
    {
      WriteLine($"Token configured (not really - this is a demo).");
      WriteLine("NOTE: This command will NOT appear in history due to *token* pattern.");
    },
    description: "Simulates setting a token - excluded from history by pattern."
  )

  // --------------------------------------------------------
  // Secret command - excluded from history
  // Demonstrates: HistoryIgnorePatterns feature
  // --------------------------------------------------------
  .AddRoute
  (
    pattern: "my-secret-command",
    handler: () =>
    {
      WriteLine("This is a secret command!");
      WriteLine("NOTE: This command will NOT appear in history due to *secret* pattern.");
    },
    description: "A secret command - excluded from history by pattern."
  )

  // --------------------------------------------------------
  // Long running command
  // Demonstrates: ShowTiming feature
  // --------------------------------------------------------
  .AddRoute
  (
    pattern: "slow {ms:int}",
    handler: async (int ms) =>
    {
      WriteLine($"Sleeping for {ms}ms to demonstrate ShowTiming feature...");
      await Task.Delay(ms);
      WriteLine("Done! Check the timing display above.");
    },
    description: "Delays for specified milliseconds to demonstrate ShowTiming."
  )

  // --------------------------------------------------------
  // Echo command - useful for testing
  // --------------------------------------------------------
  .AddRoute
  (
    pattern: "echo {*message}",
    handler: (string[] message) =>
    {
      WriteLine(string.Join(" ", message));
    },
    description: "Echoes the message back. Useful for testing."
  )

  // --------------------------------------------------------
  // Info command - shows current configuration
  // --------------------------------------------------------
  .AddRoute
  (
    pattern: "config",
    handler: () =>
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
      WriteLine();
      WriteLine("HistoryIgnorePatterns:");
      WriteLine("  - *password*");
      WriteLine("  - *secret*");
      WriteLine("  - *token*");
      WriteLine("  - *apikey*");
      WriteLine("  - *credential*");
      WriteLine("  - clear-history");
    },
    description: "Displays the current ReplOptions configuration."
  )

  // --------------------------------------------------------
  // REPL Support Configuration - THE MAIN SHOWCASE
  // --------------------------------------------------------
  .AddReplSupport
  (
    options =>
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
        "  exitcode 42    - Return custom exit code\n" +
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

      // Patterns for commands to EXCLUDE from history
      // Wildcards: * matches any characters, ? matches single character
      // Default already includes common sensitive patterns
      options.HistoryIgnorePatterns =
      [
        "*password*",   // Excludes: set-password, change-password, etc.
        "*secret*",     // Excludes: my-secret-command, show-secret, etc.
        "*token*",      // Excludes: set-token, refresh-token, etc.
        "*apikey*",     // Excludes: set-apikey, show-apikey, etc.
        "*credential*", // Excludes: store-credential, etc.
        "clear-history" // Don't record history management commands
      ];

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
    }
  )
  .Build();

// Start REPL mode
return await app.RunReplAsync();
