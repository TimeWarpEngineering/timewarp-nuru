#!/usr/bin/dotnet --

// This runfile tests delegate-based routing (Map with lambda handlers)
// Expected: COMPILE SUCCESS

WriteLine("Testing delegate-based routing with Map()");
WriteLine();

var app = NuruApp.CreateBuilder()
  .Map("ping").WithHandler(() => WriteLine("Pong!")).AsQuery().Done()
  .Map("greet {name}").WithHandler((string name) => WriteLine($"Hello, {name}!")).AsQuery().Done()
  .Map("add {a:int} {b:int}").WithHandler((int a, int b) => WriteLine($"Result: {a + b}")).AsQuery().Done()
  .Build();

int result = await app.RunAsync(args);

WriteLine();
WriteLine("SUCCESS: Compiled and ran successfully");

return result;
