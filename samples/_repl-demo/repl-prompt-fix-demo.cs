#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package TimeWarp.Terminal

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
using TimeWarp.Terminal;

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
  .Map("hello")
    .WithHandler(() => Console.WriteLine("Hello, World!"))
    .AsQuery()
    .Done()
  .Map("status")
    .WithHandler(() => Console.WriteLine("All systems operational"))
    .AsQuery()
    .Done()
  .Build();

await app.RunReplAsync();
