#!/usr/bin/dotnet --
// Test top-level statements as runfile
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("")
    .WithHandler(() => "Hello World")
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(args);
