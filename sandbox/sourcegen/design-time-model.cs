// sandbox/sourcegen/design-time-model.cs
// Design-time model types for source generator (copied from experiments)
// Agent: Amina

namespace TimeWarp.Nuru.SourceGen;

using System.Collections.Immutable;

/// <summary>
/// Design-time representation of a complete route definition.
/// </summary>
public sealed record RouteDefinition(
  string OriginalPattern,
  ImmutableArray<SegmentDefinition> Segments,
  string MessageType,
  string? Description,
  HandlerDefinition Handler,
  PipelineDefinition? Pipeline,
  ImmutableArray<string> Aliases,
  string? GroupPrefix,
  int ComputedSpecificity,
  int Order)
{
  public static RouteDefinition Create(
    string originalPattern,
    ImmutableArray<SegmentDefinition> segments,
    HandlerDefinition handler,
    string messageType = "Unspecified",
    string? description = null,
    PipelineDefinition? pipeline = null,
    ImmutableArray<string>? aliases = null,
    string? groupPrefix = null,
    int computedSpecificity = 0,
    int order = 0)
  {
    return new RouteDefinition(
      OriginalPattern: originalPattern,
      Segments: segments,
      MessageType: messageType,
      Description: description,
      Handler: handler,
      Pipeline: pipeline,
      Aliases: aliases ?? [],
      GroupPrefix: groupPrefix,
      ComputedSpecificity: computedSpecificity,
      Order: order);
  }

  public string FullPattern => string.IsNullOrEmpty(GroupPrefix)
    ? OriginalPattern
    : $"{GroupPrefix} {OriginalPattern}";

  public IEnumerable<LiteralDefinition> Literals =>
    Segments.OfType<LiteralDefinition>();

  public IEnumerable<ParameterDefinition> Parameters =>
    Segments.OfType<ParameterDefinition>();

  public IEnumerable<OptionDefinition> Options =>
    Segments.OfType<OptionDefinition>();

  public bool HasRequiredParameters =>
    Parameters.Any(p => !p.IsOptional);

  public bool HasOptions => Options.Any();

  public bool HasCatchAll =>
    Parameters.Any(p => p.IsCatchAll);
}

/// <summary>
/// Abstract base for all route segment types.
/// </summary>
public abstract record SegmentDefinition(int Position)
{
  public abstract int SpecificityContribution { get; }
}

/// <summary>
/// Represents a literal (fixed text) segment in a route pattern.
/// </summary>
public sealed record LiteralDefinition(int Position, string Value)
  : SegmentDefinition(Position)
{
  public override int SpecificityContribution => 1000;
  public string NormalizedValue => Value.ToLowerInvariant();
}

/// <summary>
/// Represents a parameter (variable) segment in a route pattern.
/// </summary>
public sealed record ParameterDefinition(
  int Position,
  string Name,
  string? TypeConstraint,
  string? Description,
  bool IsOptional,
  bool IsCatchAll,
  string? ResolvedClrTypeName,
  string? DefaultValue = null)
  : SegmentDefinition(Position)
{
  public override int SpecificityContribution =>
    IsCatchAll ? 10 : (IsOptional ? 50 : 100);

  public string CamelCaseName =>
    string.IsNullOrEmpty(Name) ? Name : char.ToLowerInvariant(Name[0]) + Name[1..];

  public bool HasTypeConstraint => !string.IsNullOrEmpty(TypeConstraint);

  public string PatternSyntax
  {
    get
    {
      string typeSpec = HasTypeConstraint ? $":{TypeConstraint}" : "";
      if (IsCatchAll)
        return $"{{*{Name}{typeSpec}}}";
      if (IsOptional)
        return $"[{Name}{typeSpec}]";
      return $"{{{Name}{typeSpec}}}";
    }
  }
}

/// <summary>
/// Represents an option/flag segment in a route pattern.
/// </summary>
public sealed record OptionDefinition(
  int Position,
  string LongForm,
  string? ShortForm,
  string? ParameterName,
  string? TypeConstraint,
  string? Description,
  bool ExpectsValue,
  bool IsOptional,
  bool IsRepeated,
  bool ParameterIsOptional,
  string? ResolvedClrTypeName)
  : SegmentDefinition(Position)
{
  public override int SpecificityContribution =>
    IsOptional ? 25 : 75;

  public bool IsFlag => !ExpectsValue;
  public string LongFormWithPrefix => $"--{LongForm}";
  public string? ShortFormWithPrefix => ShortForm is not null ? $"-{ShortForm}" : null;
}

/// <summary>
/// Design-time representation of a route handler.
/// </summary>
public sealed record HandlerDefinition(
  HandlerKind HandlerKind,
  string? FullTypeName,
  string? MethodName,
  ImmutableArray<ParameterBinding> Parameters,
  HandlerReturnType ReturnType,
  bool IsAsync,
  bool RequiresCancellationToken,
  bool RequiresServiceProvider)
{
  public static HandlerDefinition ForDelegate(
    ImmutableArray<ParameterBinding> parameters,
    HandlerReturnType returnType,
    bool isAsync,
    bool requiresCancellationToken = false)
  {
    return new HandlerDefinition(
      HandlerKind: HandlerKind.Delegate,
      FullTypeName: null,
      MethodName: null,
      Parameters: parameters,
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: requiresCancellationToken,
      RequiresServiceProvider: false);
  }

  public static HandlerDefinition ForMediator(
    string fullTypeName,
    ImmutableArray<ParameterBinding> parameters,
    HandlerReturnType returnType)
  {
    return new HandlerDefinition(
      HandlerKind: HandlerKind.Mediator,
      FullTypeName: fullTypeName,
      MethodName: null,
      Parameters: parameters,
      ReturnType: returnType,
      IsAsync: true,
      RequiresCancellationToken: true,
      RequiresServiceProvider: true);
  }

  public bool HasParameters => Parameters.Length > 0;

  public IEnumerable<ParameterBinding> RouteParameters =>
    Parameters.Where(p => p.Source != BindingSource.Service);

  public IEnumerable<ParameterBinding> ServiceParameters =>
    Parameters.Where(p => p.Source == BindingSource.Service);
}

/// <summary>
/// Specifies the kind of handler.
/// </summary>
public enum HandlerKind
{
  Delegate,
  Mediator,
  Method
}

/// <summary>
/// Information about a handler's return type.
/// </summary>
public sealed record HandlerReturnType(
  string FullTypeName,
  string ShortTypeName,
  bool IsVoid,
  bool IsTask,
  string? UnwrappedTypeName)
{
  public static readonly HandlerReturnType Void = new(
    FullTypeName: "void",
    ShortTypeName: "void",
    IsVoid: true,
    IsTask: false,
    UnwrappedTypeName: null);

  public static readonly HandlerReturnType Task = new(
    FullTypeName: "global::System.Threading.Tasks.Task",
    ShortTypeName: "Task",
    IsVoid: false,
    IsTask: true,
    UnwrappedTypeName: null);

  public static HandlerReturnType TaskOf(string innerTypeName, string shortInnerTypeName) => new(
    FullTypeName: $"global::System.Threading.Tasks.Task<{innerTypeName}>",
    ShortTypeName: $"Task<{shortInnerTypeName}>",
    IsVoid: false,
    IsTask: true,
    UnwrappedTypeName: innerTypeName);

  public static HandlerReturnType Of(string fullTypeName, string shortTypeName) => new(
    FullTypeName: fullTypeName,
    ShortTypeName: shortTypeName,
    IsVoid: false,
    IsTask: false,
    UnwrappedTypeName: null);

  public bool HasValue => !IsVoid && (!IsTask || UnwrappedTypeName is not null);
}

/// <summary>
/// Represents how a handler parameter is bound from route data or services.
/// </summary>
public sealed record ParameterBinding(
  string ParameterName,
  string ParameterTypeName,
  BindingSource Source,
  string SourceName,
  bool IsOptional,
  bool IsArray,
  string? DefaultValueExpression,
  bool RequiresConversion,
  string? ConverterTypeName)
{
  public static ParameterBinding FromParameter(
    string parameterName,
    string typeName,
    string segmentName,
    bool isOptional = false,
    string? defaultValue = null,
    bool requiresConversion = false)
  {
    return new ParameterBinding(
      ParameterName: parameterName,
      ParameterTypeName: typeName,
      Source: BindingSource.Parameter,
      SourceName: segmentName,
      IsOptional: isOptional,
      IsArray: false,
      DefaultValueExpression: defaultValue,
      RequiresConversion: requiresConversion,
      ConverterTypeName: null);
  }

  public static ParameterBinding FromOption(
    string parameterName,
    string typeName,
    string optionName,
    bool isOptional = false,
    bool isArray = false,
    string? defaultValue = null,
    bool requiresConversion = false)
  {
    return new ParameterBinding(
      ParameterName: parameterName,
      ParameterTypeName: typeName,
      Source: BindingSource.Option,
      SourceName: optionName,
      IsOptional: isOptional,
      IsArray: isArray,
      DefaultValueExpression: defaultValue,
      RequiresConversion: requiresConversion,
      ConverterTypeName: null);
  }

  public static ParameterBinding FromFlag(string parameterName, string optionName)
  {
    return new ParameterBinding(
      ParameterName: parameterName,
      ParameterTypeName: "global::System.Boolean",
      Source: BindingSource.Flag,
      SourceName: optionName,
      IsOptional: true,
      IsArray: false,
      DefaultValueExpression: "false",
      RequiresConversion: false,
      ConverterTypeName: null);
  }

  public static ParameterBinding ForCancellationToken(string parameterName = "cancellationToken")
  {
    return new ParameterBinding(
      ParameterName: parameterName,
      ParameterTypeName: "global::System.Threading.CancellationToken",
      Source: BindingSource.CancellationToken,
      SourceName: "CancellationToken",
      IsOptional: false,
      IsArray: false,
      DefaultValueExpression: null,
      RequiresConversion: false,
      ConverterTypeName: null);
  }

  public bool HasDefaultValue => DefaultValueExpression is not null;

  public bool IsFromRoute => Source is BindingSource.Parameter
    or BindingSource.Option
    or BindingSource.Flag
    or BindingSource.CatchAll;

  public string ShortTypeName
  {
    get
    {
      string typeName = ParameterTypeName;
      if (typeName.StartsWith("global::", StringComparison.Ordinal))
        typeName = typeName[8..];
      int lastDot = typeName.LastIndexOf('.');
      if (lastDot >= 0)
        typeName = typeName[(lastDot + 1)..];
      return typeName;
    }
  }
}

/// <summary>
/// Specifies where a parameter's value comes from.
/// </summary>
public enum BindingSource
{
  Parameter,
  Option,
  Flag,
  CatchAll,
  Service,
  CancellationToken
}

/// <summary>
/// Design-time representation of a middleware pipeline for a route.
/// </summary>
public sealed record PipelineDefinition(
  ImmutableArray<MiddlewareDefinition> Middleware,
  bool HasAuthorization,
  bool HasValidation,
  bool HasLogging)
{
  public static readonly PipelineDefinition Empty = new(
    Middleware: [],
    HasAuthorization: false,
    HasValidation: false,
    HasLogging: false);

  public bool HasMiddleware => Middleware.Length > 0;
}

/// <summary>
/// Design-time representation of a single middleware in the pipeline.
/// </summary>
public sealed record MiddlewareDefinition(
  string FullTypeName,
  MiddlewareKind Kind,
  ExecutionPhase ExecutionPhase,
  int Order,
  ImmutableDictionary<string, string>? Configuration);

/// <summary>
/// Categorizes the kind of middleware.
/// </summary>
public enum MiddlewareKind
{
  Custom,
  Authorization,
  Validation,
  Logging,
  ErrorHandling,
  Caching,
  RateLimiting,
  Telemetry
}

/// <summary>
/// Specifies when middleware executes relative to the handler.
/// </summary>
public enum ExecutionPhase
{
  Before,
  After,
  Wrap
}
