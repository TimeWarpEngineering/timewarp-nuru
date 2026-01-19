#!/usr/bin/dotnet --
#:project ../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

// Chained pattern - this is what bug #295 says doesn't work
return await NuruApp.CreateBuilder(args)
  .Map("hello")
    .WithHandler(() => WriteLine("Hello from chained pattern!"))
    .Done()
  .Map("")
    .WithHandler(() => "Chained Build().RunAsync() works!")
    .AsQuery()
    .Done()
  .Build()
  .RunAsync(args);
