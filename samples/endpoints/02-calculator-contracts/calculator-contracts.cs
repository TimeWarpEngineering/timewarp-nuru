#!/usr/bin/env -S dotnet --
// Calculator written contracts-first.
// A request with [Operation] and a TimeWarp.Mediator kind is the endpoint.
// Sibling of samples/endpoints/02-calculator, which spells the same ideas with [NuruRoute].
// `calculator-contracts mcp` serves the operations as MCP tools: `calc_total` publishes the real JSON
// schema of its Notes (object array) and Format (object) members, and its title and openWorldHint.
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddMcpRoute(instructions: "Each tool takes the same arguments as the CLI route of the same name.")
  .AddRepl(options =>
  {
    options.AutoStartWhenEmpty = true;
  })
  .Build();

return await app.RunAsync(args);
