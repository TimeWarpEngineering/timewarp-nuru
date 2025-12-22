#!/usr/bin/dotnet --
// hello-world - Minimal CLI using delegate-based routing
// Uses CreateSlimBuilder for lightweight delegate-only patterns (no DI, no Mediator)
#:project ../../source/timewarp-nuru-core/timewarp-nuru-core.csproj

using TimeWarp.Nuru;

NuruCoreApp app = NuruCoreApp.CreateSlimBuilder(args)
  .Map("")
    .WithHandler(() => "Hello World")
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(args);
