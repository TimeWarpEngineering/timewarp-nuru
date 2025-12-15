#!/usr/bin/dotnet --
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

// This runfile tests NURU_D001: MediatorDependencyAnalyzer
// Expected: COMPILE SUCCESS - Should NOT report NURU_D001 error
//
// When run WITH Mediator.Abstractions referenced, the analyzer should
// detect that Mediator is present and NOT report any error.

using Mediator;
using Microsoft.Extensions.DependencyInjection;

WriteLine("Testing NURU_D001: Map<T>() WITH Mediator packages");
WriteLine("Expected: This should compile successfully - no analyzer error");
WriteLine();

// Use NuruApp.CreateBuilder for full-featured builder (supports Map<TCommand>)
var app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services => services.AddMediator())
  // This should NOT trigger NURU_D001 because:
  // 1. We're calling Map<T>() (generic form)
  // 2. Mediator.Abstractions IS referenced (via #:package directive)
  // 3. The analyzer should detect Mediator is present and NOT report
  .Map<PingCommand>("ping")
  .Build();

int result = await app.RunAsync(args);

WriteLine("SUCCESS: Compiled and ran without NURU_D001 error");

return result;

// Command type implementing IRequest (from Mediator.Abstractions)
// Using IRequest (no response) which matches Map<TCommand> constraint
public record PingCommand : IRequest;

// Handler (required by Mediator.SourceGenerator)
public sealed class PingCommandHandler : IRequestHandler<PingCommand>
{
  public ValueTask<Unit> Handle(PingCommand request, CancellationToken cancellationToken)
  {
    WriteLine("Pong!");
    return default;
  }
}
