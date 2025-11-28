#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Core/TimeWarp.Nuru.Core.csproj

using TimeWarp.Nuru;

NuruCoreApp app = new NuruAppBuilder()
  .MapDefault(() => "Hello World")
  .Build();

await app.RunAsync(args);
