#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;

// Demonstrates the prompt display fix when arrow history is disabled
// Before fix: No prompt was displayed, causing confusion
// After fix: Prompt displays correctly, providing clear UX

NuruApp app = new NuruAppBuilder()
  .AddRoute("hello", () =>
  {
    Console.WriteLine("Hello, World!");
    return 0;
  })
  .AddRoute("status", () =>
  {
    Console.WriteLine("All systems operational");
    return 0;
  })
  .AddReplSupport(options =>
  {
    options.WelcomeMessage = "REPL Prompt Fix Demo - Arrow History Disabled";
    options.GoodbyeMessage = "Goodbye! (Note: Prompt was displayed for each command)";
    options.Prompt = "demo> ";
    options.EnableArrowHistory = false;  // Disabled - but prompt still displays!
    options.EnableColors = true;
    options.PromptColor = AnsiColors.Cyan;
    options.ShowTiming = true;
  })
  .Build();

return await app.RunReplAsync();
