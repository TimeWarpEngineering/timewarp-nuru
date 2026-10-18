#!/usr/bin/env -S dotnet --
// Calculator written contracts-first.
// A request with [Operation] and a TimeWarp.Mediator kind is the endpoint.
// Sibling of samples/endpoints/02-calculator, which spells the same ideas with [NuruRoute].
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddRepl(options =>
  {
    options.AutoStartWhenEmpty = true;
  })
  .Build();

return await app.RunAsync(args);
