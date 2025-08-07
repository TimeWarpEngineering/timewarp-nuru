#!/usr/bin/dotnet --
// test-boolean-option.cs - Test boolean option binding
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

// Enable more logging
Environment.SetEnvironmentVariable("NURU_LOG_MATCHER", "trace");
Environment.SetEnvironmentVariable("NURU_LOG_BINDER", "trace");
Environment.SetEnvironmentVariable("NURU_LOG_PARSER", "debug");

// Test boolean option binding
NuruApp app = new NuruAppBuilder()
    .AddRoute("sync --all", (bool all) => WriteLine($"Sync with --all = {all}"))
    .Build();

return await app.RunAsync(args).ConfigureAwait(false);