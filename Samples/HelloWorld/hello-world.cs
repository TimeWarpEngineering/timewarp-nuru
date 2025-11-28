#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .MapDefault(() => "Hello World")
  .Build();

await app.RunAsync(args);
