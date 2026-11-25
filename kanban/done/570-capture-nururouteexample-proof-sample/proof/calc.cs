#!/usr/bin/env -S dotnet --
// Proof sample for kanban 570: a worked example declared with [NuruRouteExample]
// shows up in per-route --help and in the --capabilities catalog.
// Pinned to the published TimeWarp.Nuru 4.0.0-beta.17 package by the repository
// Directory.Packages.props (not a project reference).
#:package TimeWarp.Nuru
#:property InterceptorsNamespaces=$(InterceptorsNamespaces);TimeWarp.Nuru.Generated
#:property IncludeSourceRevisionInInformationalVersion=false

using TimeWarp.Mediator;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map<AddQuery>()
  .Build();

return await app.RunAsync(args);

[NuruRoute("add", Description = "Add two numbers")]
[NuruRouteExample("add 2 3", Description = "Adds 2 and 3, prints 5")]
public sealed class AddQuery : IQuery<double>
{
  [Parameter(Order = 0, Description = "First number")]
  public double X { get; set; }

  [Parameter(Order = 1, Description = "Second number")]
  public double Y { get; set; }

  public sealed class Handler : IRequestHandler<AddQuery, double>
  {
    public ValueTask<double> Handle(AddQuery query, CancellationToken ct) =>
      ValueTask.FromResult(query.X + query.Y);
  }
}
