#!/usr/bin/dotnet --
// test-logging.cs - Test logging configurations

// Build a simple app without pre-configured logging
NuruApp app = new NuruAppBuilder()
    .AddRoute("test {value}", (string value) => WriteLine($"Test: {value}"))
    .AddRoute("hello {name}", (string name) => WriteLine($"Hello, {name}!"))
    .Build();

return await app.RunAsync(args).ConfigureAwait(false);