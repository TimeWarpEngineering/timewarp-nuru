#!/usr/bin/dotnet --
// Test filtering by ROOT group

using Editions.GroupFiltering;
using TimeWarp.Nuru;

// Filter by GandaGroup (the ROOT)
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints(typeof(GandaGroup))
  .Build();

// Test without "ganda" prefix - should work now
Console.WriteLine("Testing 'kanban add test' (without ganda prefix):");
int exitCode = await app.RunAsync(["kanban", "add", "test"]);
Console.WriteLine($"Exit code: {exitCode}");

// Test with "ganda" prefix - should NOT work
Console.WriteLine("\nTesting 'ganda kanban add test' (with ganda prefix):");
exitCode = await app.RunAsync(["ganda", "kanban", "add", "test2"]);
Console.WriteLine($"Exit code: {exitCode}");
