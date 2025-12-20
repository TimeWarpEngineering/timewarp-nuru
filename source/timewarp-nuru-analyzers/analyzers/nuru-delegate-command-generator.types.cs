namespace TimeWarp.Nuru;

/// <summary>
/// Record types and enums for the delegate command generator.
/// </summary>
public partial class NuruDelegateCommandGenerator
{
  /// <summary>
  /// Message type for generated commands/queries.
  /// </summary>
  private enum GeneratedMessageType
  {
    Command,
    IdempotentCommand,
    Query
  }

  /// <summary>
  /// Information extracted from walking back the fluent chain.
  /// </summary>
  private sealed record FluentChainInfo(
    string Pattern,
    ExpressionSyntax HandlerExpression,
    GeneratedMessageType MessageType);

  /// <summary>
  /// Route parameter information extracted from parsing the route pattern.
  /// </summary>
  private sealed record RouteParameterInfo(
    List<string> PositionalParams,
    Dictionary<string, string> OptionParams);

  /// <summary>
  /// Match result when finding a route parameter.
  /// </summary>
  private sealed record RouteParamMatch(string Name, bool IsOption);

  /// <summary>
  /// Complete information about a delegate command to generate.
  /// </summary>
  private sealed record DelegateCommandInfo(
    string ClassName,
    ImmutableArray<CommandPropertyInfo> Properties,
    string ReturnType,
    GeneratedMessageType MessageType,
    HandlerInfo? Handler);

  /// <summary>
  /// Property information for the generated command class.
  /// </summary>
  private sealed record CommandPropertyInfo(
    string Name,
    string TypeName,
    bool IsNullable,
    bool IsArray,
    string? DefaultValue);

  /// <summary>
  /// Classifies a delegate parameter as either a route parameter or DI parameter.
  /// </summary>
  private sealed record ParameterClassification(
    string Name,
    string TypeFullName,
    bool IsRouteParam,
    bool IsDiParam);

  /// <summary>
  /// Contains information needed to generate the Handler class.
  /// </summary>
  private sealed record HandlerInfo(
    ImmutableArray<ParameterClassification> Parameters,
    string LambdaBody,
    bool IsAsync,
    DelegateTypeInfo ReturnType);
}
