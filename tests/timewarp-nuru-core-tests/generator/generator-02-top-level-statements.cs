#!/usr/bin/dotnet --
// Generator test: Verify top-level statements work with source generator
// This is a runfile that uses top-level statements (no Main method)
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("")
    .WithHandler(() => "Hello World")
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(args);
