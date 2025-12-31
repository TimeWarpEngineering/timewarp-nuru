namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents a pipeline behavior (middleware) registration.
/// </summary>
/// <param name="FullTypeName">Fully qualified type name of the behavior</param>
/// <param name="Order">Execution order (lower runs first)</param>
/// <param name="AppliesTo">Which route types this behavior applies to</param>
/// <param name="ConstructorDependencies">Services required by the behavior constructor</param>
/// <param name="StateTypeName">Fully qualified name of nested State class, or null if none defined</param>
public sealed record BehaviorDefinition(
  string FullTypeName,
  int Order,
  BehaviorScope AppliesTo,
  ImmutableArray<ParameterBinding> ConstructorDependencies,
  string? StateTypeName)
{
  /// <summary>
  /// Creates a behavior that applies to all routes.
  /// </summary>
  public static BehaviorDefinition ForAll(
    string fullTypeName,
    int order = 0,
    ImmutableArray<ParameterBinding> constructorDependencies = default,
    string? stateTypeName = null) => new(
    FullTypeName: fullTypeName,
    Order: order,
    AppliesTo: BehaviorScope.All,
    ConstructorDependencies: constructorDependencies.IsDefault ? [] : constructorDependencies,
    StateTypeName: stateTypeName);

  /// <summary>
  /// Creates a behavior that applies only to commands.
  /// </summary>
  public static BehaviorDefinition ForCommands(
    string fullTypeName,
    int order = 0,
    ImmutableArray<ParameterBinding> constructorDependencies = default,
    string? stateTypeName = null) => new(
    FullTypeName: fullTypeName,
    Order: order,
    AppliesTo: BehaviorScope.Commands,
    ConstructorDependencies: constructorDependencies.IsDefault ? [] : constructorDependencies,
    StateTypeName: stateTypeName);

  /// <summary>
  /// Creates a behavior that applies only to queries.
  /// </summary>
  public static BehaviorDefinition ForQueries(
    string fullTypeName,
    int order = 0,
    ImmutableArray<ParameterBinding> constructorDependencies = default,
    string? stateTypeName = null) => new(
    FullTypeName: fullTypeName,
    Order: order,
    AppliesTo: BehaviorScope.Queries,
    ConstructorDependencies: constructorDependencies.IsDefault ? [] : constructorDependencies,
    StateTypeName: stateTypeName);

  /// <summary>
  /// Gets whether this behavior has a custom nested State class.
  /// If false, the generator will create an empty state class.
  /// </summary>
  public bool HasCustomState => StateTypeName is not null;

  /// <summary>
  /// Gets the state type name to use (custom or generated).
  /// </summary>
  public string EffectiveStateTypeName => StateTypeName ?? $"{ShortTypeName}_GeneratedState";

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

  /// <summary>
  /// Gets a safe identifier name for use in generated code (e.g., field names).
  /// Replaces dots with underscores.
  /// </summary>
  public string SafeIdentifierName
  {
    get
    {
      string typeName = FullTypeName;
      if (typeName.StartsWith("global::", StringComparison.Ordinal))
        typeName = typeName[8..];

      return typeName.Replace(".", "_", StringComparison.Ordinal);
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
