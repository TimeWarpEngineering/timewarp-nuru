// IR builder for nested route groups with prefix accumulation.
//
// Mirrors runtime GroupBuilder<TParent> for semantic interpretation.
// Groups accumulate prefixes: "admin" + "config" = "admin config".
// Routes within a group have the accumulated prefix prepended.

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// IR builder for nested route groups with prefix accumulation.
/// Mirrors runtime GroupBuilder&lt;TParent&gt;.
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to via Done().</typeparam>
public sealed class IrGroupBuilder<TParent> : IIrGroupBuilder
{
  private readonly TParent Parent;
  private readonly string AccumulatedPrefix;
  private readonly Action<RouteDefinition> RegisterRoute;

  /// <summary>
  /// Creates a new group builder.
  /// </summary>
  /// <param name="parent">The parent builder to return to via Done().</param>
  /// <param name="accumulatedPrefix">The full prefix including all parent groups.</param>
  /// <param name="registerRoute">Callback to register completed routes with the root.</param>
  internal IrGroupBuilder(
    TParent parent,
    string accumulatedPrefix,
    Action<RouteDefinition> registerRoute)
  {
    Parent = parent;
    AccumulatedPrefix = accumulatedPrefix;
    RegisterRoute = registerRoute;
  }

  /// <summary>
  /// Creates a new route with the accumulated prefix prepended.
  /// </summary>
  /// <param name="pattern">The route pattern without prefix (e.g., "status").</param>
  /// <returns>A route builder for configuring the route.</returns>
  public IIrRouteBuilder Map(string pattern)
  {
    string fullPattern = $"{AccumulatedPrefix} {pattern}";
    ImmutableArray<SegmentDefinition> segments = PatternStringExtractor.ExtractSegments(fullPattern);
    return new IrRouteBuilder<IrGroupBuilder<TParent>>(this, fullPattern, segments, RegisterRoute);
  }

  /// <summary>
  /// Creates a nested group with an additional prefix.
  /// </summary>
  /// <param name="prefix">The additional prefix for the nested group.</param>
  /// <returns>A group builder for the nested group.</returns>
  public IIrGroupBuilder WithGroupPrefix(string prefix)
  {
    string newPrefix = $"{AccumulatedPrefix} {prefix}";
    return new IrGroupBuilder<IrGroupBuilder<TParent>>(this, newPrefix, RegisterRoute);
  }

  /// <summary>
  /// Completes the group and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder.</returns>
  public TParent Done() => Parent;

  /// <summary>
  /// Explicit interface implementation for polymorphic dispatch.
  /// </summary>
  object IIrGroupBuilder.Done() => Done()!;
}
