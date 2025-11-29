#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

// ============================================================================
// Custom Key Bindings Demo
// ============================================================================
// This demo shows how to create personalized REPL key bindings using
// CustomKeyBindingProfile. You can:
//   - Start from any built-in profile (Default, Emacs, Vi, VSCode)
//   - Override existing bindings
//   - Add new bindings
//   - Remove unwanted bindings
//   - Customize exit keys
//
// Run this file to explore the custom key binding system interactively.
// ============================================================================

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using static System.Console;

WriteLine("Custom Key Bindings Demo");
WriteLine("========================");
WriteLine();
WriteLine("This demo uses a CustomKeyBindingProfile based on the Emacs profile");
WriteLine("with the following modifications:");
WriteLine();
WriteLine("  ADDED:    Ctrl+G  - Bell/ding sound (custom action)");
WriteLine("  REMOVED:  Ctrl+D  - No longer exits (EOF disabled)");
WriteLine("  KEPT:     All other Emacs bindings (Ctrl+A, Ctrl+E, etc.)");
WriteLine();
WriteLine("Try these key combinations:");
WriteLine("  Ctrl+A    - Move to beginning of line (from Emacs)");
WriteLine("  Ctrl+E    - Move to end of line (from Emacs)");
WriteLine("  Ctrl+G    - Bell/ding (custom addition)");
WriteLine("  Ctrl+D    - Does nothing (removed from Emacs profile)");
WriteLine("  Tab       - Tab completion");
WriteLine("  Up/Down   - History navigation (Emacs doesn't include arrows by default)");
WriteLine();

// ============================================================================
// Create a custom key binding profile
// ============================================================================
// CustomKeyBindingProfile lets you start from any existing profile and
// modify it to suit your preferences. Here we start from Emacs and:
//   1. Add a custom Ctrl+L binding to clear the screen
//   2. Remove the Ctrl+D (EOF) binding that would exit the REPL
// ============================================================================

CustomKeyBindingProfile customProfile = new CustomKeyBindingProfile(new EmacsKeyBindingProfile())
  .WithName("EmacsCustomized")

  // Add Ctrl+G to "ding" (bell) - a simple custom action
  // Note: Custom actions can't call internal ReplConsoleReader methods from outside the assembly,
  // but they can perform any other action (write output, play sounds, etc.)
  .Add
  (
    ConsoleKey.G,
    ConsoleModifiers.Control,
    _ => () => Write("\a") // Bell character
  )

  // Remove Ctrl+D so it doesn't exit the REPL
  // Users must type 'exit' or 'quit' instead
  .Remove(ConsoleKey.D, ConsoleModifiers.Control);

// ============================================================================
// Build the app with custom key bindings
// ============================================================================

NuruCoreApp app = new NuruAppBuilder()
  .WithMetadata
  (
    description: "Demonstrates CustomKeyBindingProfile for personalized REPL key bindings."
  )

  // --------------------------------------------------------
  // Echo command for testing
  // --------------------------------------------------------
  .Map
  (
    pattern: "echo {*message}",
    handler: (string[] message) =>
    {
      WriteLine(string.Join(" ", message));
    },
    description: "Echoes the message back."
  )

  // --------------------------------------------------------
  // Show current key binding info
  // --------------------------------------------------------
  .Map
  (
    pattern: "bindings",
    handler: () =>
    {
      WriteLine("Current Key Binding Profile: EmacsCustomized");
      WriteLine();
      WriteLine("Base: Emacs profile");
      WriteLine();
      WriteLine("Modifications:");
      WriteLine("  + Ctrl+G  : Bell/ding (custom addition)");
      WriteLine("  - Ctrl+D  : Removed (no EOF exit)");
      WriteLine();
      WriteLine("Inherited Emacs Bindings:");
      WriteLine("  Ctrl+A    : Beginning of line");
      WriteLine("  Ctrl+E    : End of line");
      WriteLine("  Ctrl+B    : Backward char");
      WriteLine("  Ctrl+F    : Forward char");
      WriteLine("  Alt+B     : Backward word");
      WriteLine("  Alt+F     : Forward word");
      WriteLine("  Ctrl+P    : Previous history");
      WriteLine("  Ctrl+N    : Next history");
      WriteLine("  Escape    : Clear current line");
    },
    description: "Shows the current key binding configuration."
  )

  // --------------------------------------------------------
  // List available profiles
  // --------------------------------------------------------
  .Map
  (
    pattern: "profiles",
    handler: () =>
    {
      WriteLine("Available Built-in Profiles:");
      WriteLine();
      WriteLine("  Default  - PSReadLine-compatible (Windows PowerShell style)");
      WriteLine("  Emacs    - Emacs/bash/readline conventions");
      WriteLine("  Vi       - Vi-inspired modal editing");
      WriteLine("  VSCode   - Modern IDE-style bindings");
      WriteLine();
      WriteLine("This demo uses: CustomKeyBindingProfile based on Emacs");
    },
    description: "Lists available key binding profiles."
  )

  // --------------------------------------------------------
  // REPL configuration with custom key bindings
  // --------------------------------------------------------
  .AddReplSupport
  (
    options =>
    {
      options.Prompt = "custom> ";
      options.PromptColor = "\x1b[35m"; // Magenta to indicate custom bindings

      options.WelcomeMessage =
        "Custom Key Bindings Demo - REPL with personalized Emacs bindings\n" +
        "Try: 'bindings' to see customizations, 'profiles' to see available profiles\n" +
        "Try: Ctrl+G for bell, Ctrl+A/Ctrl+E for line navigation";

      options.GoodbyeMessage = "Custom key bindings demo complete!";

      // Set the custom profile
      options.KeyBindingProfile = customProfile;
    }
  )
  .Build();

// Start REPL mode
return await app.RunReplAsync();
