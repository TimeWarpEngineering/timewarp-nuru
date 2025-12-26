namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents a pipeline behavior (middleware) registration.
/// </summary>
/// <param name="FullTypeName">Fully qualified type name of the behavior</param>
/// <param name="Order">Execution order (lower runs first)</param>
/// <param name="AppliesTo">Which route types this behavior applies to</param>
public sealed record BehaviorDefinition(
  string FullTypeName,
  int Order,
  BehaviorScope AppliesTo)
{
  /// <summary>
  /// Creates a behavior that applies to all routes.
  /// </summary>
  public static BehaviorDefinition ForAll(string fullTypeName, int order = 0) => new(
    FullTypeName: fullTypeName,
    Order: order,
    AppliesTo: BehaviorScope.All);

  /// <summary>
  /// Creates a behavior that applies only to commands.
  /// </summary>
  public static BehaviorDefinition ForCommands(string fullTypeName, int order = 0) => new(
    FullTypeName: fullTypeName,
    Order: order,
    AppliesTo: BehaviorScope.Commands);

  /// <summary>
  /// Creates a behavior that applies only to queries.
  /// </summary>
  public static BehaviorDefinition ForQueries(string fullTypeName, int order = 0) => new(
    FullTypeName: fullTypeName,
    Order: order,
    AppliesTo: BehaviorScope.Queries);

  /// <summary>
  /// Gets the short type name for display.
  /// </summary>
  public string ShortTypeName
  {
    get
    {
      string typeName = FullTypeName;
      if (typeName.StartsWith("global::", StringComparison.Ordinal))
        typeName = typeName[8..];

      int lastDot = typeName.LastIndexOf('.');
      return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
    }
  }
}

/// <summary>
/// Specifies which routes a behavior applies to.
/// </summary>
public enum BehaviorScope
{
  /// <summary>
  /// Behavior applies to all routes.
  /// </summary>
  All,

  /// <summary>
  /// Behavior applies only to commands.
  /// </summary>
  Commands,

  /// <summary>
  /// Behavior applies only to queries.
  /// </summary>
  Queries
}
