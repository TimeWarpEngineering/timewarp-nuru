#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj
#:package Microsoft.Extensions.Logging

using TimeWarp.Nuru;
using TimeWarp.Nuru.Logging;
using Microsoft.Extensions.Logging;

var app = new NuruAppBuilder()
    .UseDebugLogging() // Enable debug logging to see all log levels
    .AddRoute("test", () => Console.WriteLine("Test command executed"))
    .AddRoute("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
    .Build();

return await app.RunAsync(args);