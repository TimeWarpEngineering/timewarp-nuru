#!/usr/bin/dotnet --

using Microsoft.Extensions.Options;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder(args)
  .Map("test")
    .WithHandler((IOptions<TestOptions> opts) => $"Value: {opts.Value.Value}")
    .AsQuery()
    .Done()
  .Build();

int result = await app.RunAsync(args);
Console.WriteLine($"Exit: {result}");
return result;

public class TestOptions { public string Value { get; set; } = "default"; }
