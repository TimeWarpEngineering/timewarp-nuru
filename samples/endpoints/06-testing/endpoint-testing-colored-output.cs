#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - TESTING COLORED OUTPUT ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
// Demonstrates testing handlers that produce colored/styled output.
//
// DSL: Endpoint with ITerminal and ANSI color codes
//
// PATTERN: Verify ANSI codes in captured output from TestTerminal
// ═══════════════════════════════════════════════════════════════════════════════
#:package Shouldly
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using Shouldly;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

Console.WriteLine("=== Testing Colored Output (Endpoint DSL) ===\n");

using TestTerminal terminal = new();

NuruApp app = NuruApp.CreateBuilder()
  .UseTerminal(terminal)
  .DiscoverEndpoints()
  .Build();

// Test success output
Console.WriteLine("Testing success output...");
await app.RunAsync(["status"]);
terminal.OutputContains("✓").ShouldBeTrue();
terminal.OutputContains("Healthy".Green().ToString()).ShouldBeTrue();
Console.WriteLine("  ✓ Success indicators captured");

// Test error output
Console.WriteLine("\nTesting error output...");
terminal.Clear();
await app.RunAsync(["error", "test"]).ContinueWith(t => { /* Expected to throw */ });
terminal.ErrorContains("Error").ShouldBeTrue();
Console.WriteLine("  ✓ Error indicators captured");

// Test mixed output
Console.WriteLine("\nTesting mixed colored output...");
terminal.Clear();
await app.RunAsync(["report"]);
string output = terminal.Output;
output.ShouldContain("SUCCESS".Green().ToString());
output.ShouldContain("WARNING".Yellow().ToString());
output.ShouldContain("ERROR".Red().ToString());
Console.WriteLine("  ✓ All color codes captured");

Console.WriteLine("\n--- Full captured output ---");
Console.WriteLine(terminal.Output);
Console.WriteLine("=== Test Complete ===".BrightGreen().Bold());

// =============================================================================
// ENDPOINT DEFINITIONS - Colored output using ITerminal
// =============================================================================

[NuruRoute("status", Description = "Show status with colors")]
public sealed class StatusCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal T) : ICommandHandler<StatusCommand, Unit>
  {
    public ValueTask<Unit> Handle(StatusCommand c, CancellationToken ct)
    {
      T.WriteLine($"{'✓'.Green()} System Healthy".Green());
      T.WriteLine($"{'✓'.Green()} Database Connected".Green());
      T.WriteLine($"{'✓'.Green()} API Responsive".Green());
      return default;
    }
  }
}

[NuruRoute("error", Description = "Simulate an error")]
public sealed class ErrorCommand : ICommand<Unit>
{
  [Parameter] public string Message { get; set; } = "";

  public sealed class Handler(ITerminal T) : ICommandHandler<ErrorCommand, Unit>
  {
    public ValueTask<Unit> Handle(ErrorCommand c, CancellationToken ct)
    {
      T.WriteErrorLine($"{'✗'.Red()} Error: {c.Message}".Red());
      throw new InvalidOperationException(c.Message);
    }
  }
}

[NuruRoute("report", Description = "Show report with mixed colors")]
public sealed class ReportCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal T) : ICommandHandler<ReportCommand, Unit>
  {
    public ValueTask<Unit> Handle(ReportCommand c, CancellationToken ct)
    {
      T.WriteLine("System Report:".Bold().Underline());
      T.WriteLine($"  {'SUCCESS'.Green()}: All systems operational");
      T.WriteLine($"  {'WARNING'.Yellow()}: High memory usage");
      T.WriteLine($"  {'ERROR'.Red()}: 1 failed job (non-critical)");
      return default;
    }
  }
}
