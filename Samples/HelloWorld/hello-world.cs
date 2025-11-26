#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;

var app = NuruApp.CreateBuilder(args)
  .MapDefault(() => "Hello World")
  .Build();

await app.RunAsync(args);
