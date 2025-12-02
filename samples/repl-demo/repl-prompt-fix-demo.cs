#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

// ═══════════════════════════════════════════════════════════════════════════════
// REPL PROMPT FIX DEMO
// ═══════════════════════════════════════════════════════════════════════════════
//
// Demonstrates the prompt display fix when arrow history is disabled.
// Before fix: No prompt was displayed, causing confusion
// After fix: Prompt displays correctly, providing clear UX
//
// This sample uses NuruApp.CreateBuilder(args) which provides all REPL features.
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using Microsoft.Extensions.DependencyInjection;

NuruAppOptions nuruAppOptions = new()
{
  ConfigureRepl = options =>
  {
    options.WelcomeMessage = "REPL Prompt Fix Demo - Arrow History Disabled";
    options.GoodbyeMessage = "Goodbye! (Note: Prompt was displayed for each command)";
    options.Prompt = "demo> ";
    options.EnableArrowHistory = false;  // Disabled - but prompt still displays!
    options.EnableColors = true;
    options.PromptColor = AnsiColors.Cyan;
    options.ShowTiming = true;
  }
};

NuruCoreApp app = NuruApp.CreateBuilder(args, nuruAppOptions)
  .ConfigureServices(services => services.AddMediator())
  .Map("hello", () =>
  {
    Console.WriteLine("Hello, World!");
    return 0;
  })
  .Map("status", () =>
  {
    Console.WriteLine("All systems operational");
    return 0;
  })
  .Build();

return await app.RunReplAsync();
