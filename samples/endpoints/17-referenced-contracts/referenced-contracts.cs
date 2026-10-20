#!/usr/bin/env -S dotnet --
// A CLI whose endpoints live in a referenced contracts assembly (contracts/), not in this file.
// [assembly: NuruEndpointAssembly(typeof(...))] opts that assembly into DiscoverEndpoints().
// Without it, a referenced assembly adds no commands. Map<GreetQuery>() would add one endpoint
// to this app without the attribute.
//
//   dotnet run samples/endpoints/17-referenced-contracts/referenced-contracts.cs -- greet --Name Ada
//   dotnet run samples/endpoints/17-referenced-contracts/referenced-contracts.cs -- sum --Values 2 --Values 3
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project contracts/referenced-contracts-library.csproj

using ReferencedContracts;
using TimeWarp.Nuru;

[assembly: NuruEndpointAssembly(typeof(GreetQuery))]

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
