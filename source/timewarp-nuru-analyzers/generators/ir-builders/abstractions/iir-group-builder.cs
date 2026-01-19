// Marker interface for nested group builders.
//
// Groups accumulate prefixes and return to their parent via Done().
// Unlike IIrAppBuilder, groups have a parent to return to.

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Nested group builder that returns to parent via Done().
/// </summary>
public interface IIrGroupBuilder : IIrRouteSource
{
  /// <summary>
  /// Completes the group and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder (boxed as object for polymorphic dispatch).</returns>
  object Done();
}
