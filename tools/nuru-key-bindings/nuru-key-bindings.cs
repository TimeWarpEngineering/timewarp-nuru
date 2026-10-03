#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

// A leading option belongs to the key-bindings command, so a bare invocation
// and `key-bindings ...` both list bindings.
string[] forwarded = args.Length == 0 || args[0].StartsWith('-')
  ? ["key-bindings", .. args]
  : args;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(forwarded);
