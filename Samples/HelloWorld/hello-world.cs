#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;

var app = NuruApp.CreateBuilder(args)
  .Map("hello", () => "Hello World")
  .Build();

return await app.RunAsync(args);
