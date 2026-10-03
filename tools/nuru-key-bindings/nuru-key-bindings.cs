#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

// Everything belongs to the key-bindings command, so a bare invocation,
// `Emacs --detailed`, and `key-bindings Emacs --detailed` all list bindings.
string[] forwarded = args.Length > 0 && args[0] == "key-bindings"
  ? args
  : ["key-bindings", .. args];

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(forwarded);
