namespace TimeWarp.Nuru.SourceGen;

/// <summary>
/// Design-time representation of a middleware pipeline for a route.
/// Captures the chain of middleware that should wrap the handler.
/// </summary>
/// <param name="Middleware">The ordered list of middleware in the pipeline</param>
/// <param name="HasAuthorization">Whether any authorization middleware is present</param>
/// <param name="HasValidation">Whether any validation middleware is present</param>
/// <param name="HasLogging">Whether any logging middleware is present</param>
internal sealed record PipelineDefinition(
  ImmutableArray<MiddlewareDefinition> Middleware,
  bool HasAuthorization,
  bool HasValidation,
  bool HasLogging)
{
  /// <summary>
  /// An empty pipeline with no middleware.
  /// </summary>
  public static readonly PipelineDefinition Empty = new(
    Middleware: [],
    HasAuthorization: false,
    HasValidation: false,
    HasLogging: false);

  /// <summary>
  /// Creates a pipeline from a list of middleware.
  /// </summary>
  public static PipelineDefinition Create(ImmutableArray<MiddlewareDefinition> middleware)
  {
    bool hasAuth = middleware.Any(m => m.Kind == MiddlewareKind.Authorization);
    bool hasValidation = middleware.Any(m => m.Kind == MiddlewareKind.Validation);
    bool hasLogging = middleware.Any(m => m.Kind == MiddlewareKind.Logging);

    return new PipelineDefinition(
      Middleware: middleware,
      HasAuthorization: hasAuth,
      HasValidation: hasValidation,
      HasLogging: hasLogging);
  }

  /// <summary>
  /// Creates a pipeline with a single middleware.
  /// </summary>
  public static PipelineDefinition With(MiddlewareDefinition middleware)
    => Create([middleware]);

  /// <summary>
  /// Gets whether this pipeline has any middleware.
  /// </summary>
  public bool HasMiddleware => Middleware.Length > 0;

  /// <summary>
  /// Gets the middleware in execution order (before handler).
  /// </summary>
  public IEnumerable<MiddlewareDefinition> BeforeHandler =>
    Middleware.Where(m => m.ExecutionPhase == ExecutionPhase.Before);

  /// <summary>
  /// Gets the middleware in execution order (after handler).
  /// </summary>
  public IEnumerable<MiddlewareDefinition> AfterHandler =>
    Middleware.Where(m => m.ExecutionPhase == ExecutionPhase.After);

  /// <summary>
  /// Gets middleware that wraps the handler (both before and after).
  /// </summary>
  public IEnumerable<MiddlewareDefinition> Wrapping =>
    Middleware.Where(m => m.ExecutionPhase == ExecutionPhase.Wrap);
}

/// <summary>
/// Design-time representation of a single middleware in the pipeline.
/// </summary>
/// <param name="FullTypeName">Fully qualified type name of the middleware</param>
/// <param name="Kind">The kind of middleware (for categorization)</param>
/// <param name="ExecutionPhase">When this middleware executes relative to the handler</param>
/// <param name="Order">Explicit order within the pipeline</param>
/// <param name="Configuration">Optional configuration for the middleware</param>
internal sealed record MiddlewareDefinition(
  string FullTypeName,
  MiddlewareKind Kind,
  ExecutionPhase ExecutionPhase,
  int Order,
  ImmutableDictionary<string, string>? Configuration)
{
  /// <summary>
  /// Creates a middleware definition with default values.
  /// </summary>
  public static MiddlewareDefinition Create(
    string fullTypeName,
    MiddlewareKind kind = MiddlewareKind.Custom,
    ExecutionPhase phase = ExecutionPhase.Wrap,
    int order = 0,
    ImmutableDictionary<string, string>? configuration = null)
  {
    return new MiddlewareDefinition(
      FullTypeName: fullTypeName,
      Kind: kind,
      ExecutionPhase: phase,
      Order: order,
      Configuration: configuration);
  }

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
  /// Gets whether this middleware has configuration.
  /// </summary>
  public bool HasConfiguration => Configuration?.Count > 0;
}

/// <summary>
/// Categorizes the kind of middleware.
/// </summary>
internal enum MiddlewareKind
{
  /// <summary>
  /// Custom middleware without special handling.
  /// </summary>
  Custom,

  /// <summary>
  /// Authorization/authentication middleware.
  /// </summary>
  Authorization,

  /// <summary>
  /// Input validation middleware.
  /// </summary>
  Validation,

  /// <summary>
  /// Logging/tracing middleware.
  /// </summary>
  Logging,

  /// <summary>
  /// Error handling middleware.
  /// </summary>
  ErrorHandling,

  /// <summary>
  /// Caching middleware.
  /// </summary>
  Caching,

  /// <summary>
  /// Rate limiting middleware.
  /// </summary>
  RateLimiting,

  /// <summary>
  /// Telemetry/metrics middleware.
  /// </summary>
  Telemetry
}

/// <summary>
/// Specifies when middleware executes relative to the handler.
/// </summary>
internal enum ExecutionPhase
{
  /// <summary>
  /// Executes before the handler.
  /// </summary>
  Before,

  /// <summary>
  /// Executes after the handler.
  /// </summary>
  After,

  /// <summary>
  /// Wraps the handler (executes both before and after).
  /// </summary>
  Wrap
}
