#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Demo: Colored vs Plain help output
using TimeWarp.Nuru;

EndpointCollection endpoints = [];
endpoints.Add(new Endpoint { RoutePattern = "build", CompiledRoute = PatternParser.Parse("build"), Handler = () => { }, Description = "Build the project" });
endpoints.Add(new Endpoint { RoutePattern = "deploy {env}", CompiledRoute = PatternParser.Parse("deploy {env}"), Handler = () => { }, Description = "Deploy to an environment" });
endpoints.Add(new Endpoint { RoutePattern = "config set {key} {value?}", CompiledRoute = PatternParser.Parse("config set {key} {value?}"), Handler = () => { }, Description = "Set a config value" });
endpoints.Add(new Endpoint { RoutePattern = "--verbose", CompiledRoute = PatternParser.Parse("--verbose"), Handler = () => { }, Description = "Enable verbose output" });
endpoints.Add(new Endpoint { RoutePattern = "--dry-run?", CompiledRoute = PatternParser.Parse("--dry-run?"), Handler = () => { }, Description = "Preview without changes" });

Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("  COLORED HELP OUTPUT (useColor: true)");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine(HelpProvider.GetHelpText(endpoints, "myapp", "A sample CLI application", useColor: true));
Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("  PLAIN HELP OUTPUT (useColor: false)");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine(HelpProvider.GetHelpText(endpoints, "myapp", "A sample CLI application", useColor: false));

return 0;
