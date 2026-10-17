#region Purpose
// Tries to bind an array of objects the way multi_query binds operations.
#endregion

namespace RoslynkNuru.Binding;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// Echoes an array of nested operations when the binder accepts one.
/// </summary>
[NuruRoute("object-array", Description = "Bind an array of objects")]
public sealed class ObjectArrayQuery : IQuery<string>
{
  [Option("operations", Description = "Nested operations.")]
  public ProbeOperation[] Operations { get; set; } = [];

  public sealed class Handler : IQueryHandler<ObjectArrayQuery, string>
  {
    public Task<string> Handle(ObjectArrayQuery query, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult($"count={query.Operations.Length}");
    }
  }
}
