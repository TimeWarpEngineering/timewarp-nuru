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
//   - Emacs: Ctrl+A (beginning), Ctrl+E (end), Ctrl+K (kill line)
//   - Windows: Home, End, Ctrl+Arrow keys
//   - Vi: Modal editing with Esc/command mode
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using TimeWarp.Terminal;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

// Check for keybinding preference
string? profile = Environment.GetEnvironmentVariable("REPL_KEYBINDINGS")?.ToLower();
KeyBindings bindings = profile switch
{
  "windows" => KeyBindings.Windows,
  "vi" or "vim" => KeyBindings.Vi,
  _ => KeyBindings.Emacs
};

Console.WriteLine($"REPL mode with {bindings} key bindings");
Console.WriteLine("Set REPL_KEYBINDINGS=windows|vi|emacs to change");
Console.WriteLine();

// Configure REPL options
ReplOptions options = new()
{
  KeyBindings = bindings,
  Prompt = $"nuru-{profile ?? "emacs"}> ",
  WelcomeMessage = "Interactive REPL with custom key bindings",
  GoodbyeMessage = "Goodbye!"
};

return await app.RunAsReplAsync(options);

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
  [Parameter(IsOptional = true)] public int? Count { get; set; }

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
