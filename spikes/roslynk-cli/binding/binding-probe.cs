#!/usr/bin/env -S dotnet --
#region Purpose
// Probe app for --json-args shapes. Not part of the pitch CLI.
#endregion
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
