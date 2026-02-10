#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - REPL WITH CUSTOM KEY BINDINGS ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// REPL mode with PSReadLine-style key binding profiles.
//
// DSL: Endpoint with custom REPL options
//
// PROFILES:
//   - Default: PSReadLine-compatible bindings
//   - Emacs: Ctrl+A (beginning), Ctrl+E (end), Ctrl+K (kill line)
//   - Vi: Modal editing with Esc/command mode
// ═══════════════════════════════════════════════════════════════════════════════

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

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("echo", Description = "Echo text back")]
public sealed class EchoCommand : ICommand<Unit>
{
  [Parameter] public string Text { get; set; } = "";

  public sealed class Handler : ICommandHandler<EchoCommand, Unit>
  {
    public ValueTask<Unit> Handle(EchoCommand c, CancellationToken ct)
    {
      Console.WriteLine(c.Text);
      return default;
    }
  }
}

[NuruRoute("time", Description = "Show current time")]
public sealed class TimeCommand : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<TimeCommand, Unit>
  {
    public ValueTask<Unit> Handle(TimeCommand q, CancellationToken ct)
    {
      Console.WriteLine($"Current time: {DateTime.Now:HH:mm:ss}");
      return default;
    }
  }
}

[NuruRoute("history", Description = "Show command history")]
public sealed class HistoryCommand : ICommand<Unit>
{
  // Use nullable type for optional parameter (no IsOptional attribute needed)
  [Parameter] public int? Count { get; set; }

  public sealed class Handler : ICommandHandler<HistoryCommand, Unit>
  {
    public ValueTask<Unit> Handle(HistoryCommand c, CancellationToken ct)
    {
      Console.WriteLine("Recent commands:");
      // In real app, this would read from history store
      Console.WriteLine("  (history would appear here)");
      return default;
    }
  }
}
