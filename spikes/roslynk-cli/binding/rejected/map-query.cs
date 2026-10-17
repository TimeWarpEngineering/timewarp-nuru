#region Purpose
// Tries to bind a string-to-JSON map the way multi_query binds arguments.
#endregion

namespace RoslynkNuru.Binding;

using System.Text.Json;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// Echoes a dictionary when the binder accepts one.
/// </summary>
[NuruRoute("map", Description = "Bind a JSON object map")]
public sealed class MapQuery : IQuery<string>
{
  [Option("arguments", Description = "Tool arguments object.")]
  public Dictionary<string, JsonElement>? Arguments { get; set; }

  public sealed class Handler : IQueryHandler<MapQuery, string>
  {
    public Task<string> Handle(MapQuery query, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      int count = query.Arguments?.Count ?? -1;
      return Task.FromResult($"arguments={count}");
    }
  }
}
