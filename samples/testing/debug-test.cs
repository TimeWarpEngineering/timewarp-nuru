#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruCoreApp app = NuruCoreApp.CreateSlimBuilder()
  .AddAutoHelp()
  .Map("greet {name}", (string name, ITerminal t) => t.WriteLine($"Hello, {name}!"))
  .Build();

using TestTerminal terminal = new();
TestTerminalContext.Current = terminal;

int exitCode = await app.RunAsync(["unknown-command"]);

Console.WriteLine($"Exit code: {exitCode}");
Console.WriteLine($"Output: [{terminal.Output}]");
Console.WriteLine($"ErrorOutput: [{terminal.ErrorOutput}]");
Console.WriteLine($"ErrorContains: {terminal.ErrorContains("No matching command found")}");
