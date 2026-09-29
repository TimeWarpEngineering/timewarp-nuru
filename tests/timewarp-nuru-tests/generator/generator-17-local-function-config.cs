#!/usr/bin/env -S dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Local Function ConfigureServices
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify unqualified method group references to local functions work.
// This is the most natural pattern for users:
//   static void ConfigureServices(IServiceCollection services) { ... }
//   .ConfigureServices(ConfigureServices)
//
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.DependencyInjection;

// Local function - this is how users naturally write it in top-level statements
static void ConfigureMyServices(IServiceCollection services)
{
  services.AddSingleton<ILfc17Service, Lfc17Service>();
}

// Run the test
using TestTerminal terminal = new();

NuruApp app = NuruApp.CreateBuilder()
  .UseTerminal(terminal)
  .UseMicrosoftDependencyInjection()
  .ConfigureServices(ConfigureMyServices) // Unqualified local function reference!
  .Map("lfc17-test")
    .WithHandler((ILfc17Service svc) => svc.GetMessage())
    .AsQuery()
    .Done()
  .Build();

int exitCode = await app.RunAsync(["lfc17-test"]);

if (exitCode != 0)
  throw new InvalidOperationException($"Exit code was {exitCode}");

if (!terminal.OutputContains("Local function service works!"))
  throw new InvalidOperationException($"Output was '{terminal.Output}'");

return 0;

// Service definitions at bottom (after top-level code)
public interface ILfc17Service
{
  string GetMessage();
}

public class Lfc17Service : ILfc17Service
{
  public string GetMessage() => "Local function service works!";
}
