#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - REPL WITH CUSTOM KEY BINDINGS ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// REPL mode with PSReadLine-style key binding profiles.
//
// DSL: Endpoint with custom REPL options
//
// PROFILES:
//   - Default: PSReadLine-compatible bindings
//   - Emacs: Ctrl+A (beginning), Ctrl+E (end), Ctrl+K (kill line)
//   - Vi: Modal editing with Esc/command mode
// ═══════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

// Check for keybinding preference
string? profile = Environment.GetEnvironmentVariable("REPL_KEYBINDINGS")?.ToLower();
string profileName = profile switch
{
  "emacs" => "Emacs",
  "vi" or "vim" => "Vi",
  _ => "Default"
};

Console.WriteLine($"REPL mode with {profileName} key bindings");
Console.WriteLine("Set REPL_KEYBINDINGS=emacs|vi to change");
Console.WriteLine();

// Single Build() with AddRepl() - handles both CLI and REPL modes
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddRepl(options =>
  {
    options.KeyBindingProfileName = profileName;
    options.Prompt = $"nuru-{profile ?? "default"}> ";
    options.WelcomeMessage = "Interactive REPL with custom key bindings";
    options.GoodbyeMessage = "Goodbye!";
    options.AutoStartWhenEmpty = true;
  })
  .Build();

// RunAsync handles both CLI and REPL modes automatically
return await app.RunAsync(args);
