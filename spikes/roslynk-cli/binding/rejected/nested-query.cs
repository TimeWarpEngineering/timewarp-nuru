#region Purpose
// Tries to bind one nested object the way multi_query binds one operation.
#endregion

namespace RoslynkNuru.Binding;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// Echoes a nested operation when the binder accepts one.
/// </summary>
[NuruRoute("nested", Description = "Bind one nested object")]
public sealed class NestedQuery : IQuery<string>
{
  [Option("operation", Description = "One nested operation.")]
  public ProbeOperation? Operation { get; set; }

  public sealed class Handler : IQueryHandler<NestedQuery, string>
  {
    public Task<string> Handle(NestedQuery query, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (query.Operation is null)
      {
        return Task.FromResult("operation=null");
      }

      return Task.FromResult($"operation={query.Operation.Tool}");
    }
  }
}
