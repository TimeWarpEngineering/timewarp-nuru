#!/usr/bin/dotnet --

// This runfile tests what happens when Mediator.SourceGenerator is missing
// Expected: COMPILE FAILURE
// Error: CS1061 - 'IServiceCollection' does not contain a definition for 'AddMediator'
//
// The source generator must be DIRECTLY referenced to generate AddMediator().
// Transitive reference from timewarp-nuru is NOT sufficient.

using Mediator;
using Microsoft.Extensions.DependencyInjection;

WriteLine("This should NOT compile - missing Mediator.SourceGenerator");

var app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services => services.AddMediator())
  .Map<PingCommand>("ping")
  .Build();

await app.RunAsync(args);

public record PingCommand : IRequest;

public sealed class PingCommandHandler : IRequestHandler<PingCommand>
{
  public ValueTask<Unit> Handle(PingCommand request, CancellationToken cancellationToken)
  {
    WriteLine("Pong!");
    return default;
  }
}
