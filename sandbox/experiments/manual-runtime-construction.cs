#!/usr/bin/env dotnet run
// Runfile: sandbox/manual-runtime-construction.cs
// Purpose: Manually construct runtime structures from RouteDefinition to execute "add 2 2"
// This is step-2 of task #242 - showing what the source generator needs to emit.
//
// Usage: dotnet sandbox/manual-runtime-construction.cs -- add 2 2
// Expected output: 4

using System.Collections.Immutable;

// =============================================================================
// STEP 1: Build the Design-Time Model (copied from step-1)
// =============================================================================

RouteDefinition addRoute = RouteDefinition.Create
(
  originalPattern: "add {x:int} {y:int}",
  segments:
  [
    new LiteralDefinition(Position: 0, Value: "add"),
    new ParameterDefinition
    (
      Position: 1,
      Name: "x",
      TypeConstraint: "int",
      Description: null,
      IsOptional: false,
      IsCatchAll: false,
      ResolvedClrTypeName: "global::System.Int32",
      DefaultValue: null
    ),
    new ParameterDefinition
    (
      Position: 2,
      Name: "y",
      TypeConstraint: "int",
      Description: null,
      IsOptional: false,
      IsCatchAll: false,
      ResolvedClrTypeName: "global::System.Int32",
      DefaultValue: null
    ),
  ],
  handler: HandlerDefinition.ForDelegate
  (
    parameters:
    [
      ParameterBinding.FromParameter
      (
        parameterName: "x",
        typeName: "global::System.Int32",
        segmentName: "x",
        isOptional: false,
        defaultValue: null,
        requiresConversion: true
      ),
      ParameterBinding.FromParameter
      (
        parameterName: "y",
        typeName: "global::System.Int32",
        segmentName: "y",
        isOptional: false,
        defaultValue: null,
        requiresConversion: true
      ),
    ],
    returnType: HandlerReturnType.Of("global::System.Int32", "int"),
    isAsync: false,
    requiresCancellationToken: false
  ),
  messageType: "Query",
  description: "Add two integers"
);

// =============================================================================
// STEP 2: Build the Runtime Structures
// =============================================================================

// The actual handler function that will be invoked
Func<int, int, int> addHandler = (int x, int y) => x + y;

// Build a compiled route from the design-time definition
CompiledRoute compiledRoute = CompiledRoute.FromDefinition
(
  addRoute,
  invoker: (Dictionary<string, object> parameters) =>
  {
    // This is what the generator would emit - extract typed parameters and call handler
    int x = (int)parameters["x"];
    int y = (int)parameters["y"];
    int result = addHandler(x, y);
    return result;
  }
);

// Create the router with our single route
Router router = new([compiledRoute]);

// =============================================================================
// STEP 3: Execute the Command
// =============================================================================

// Get command-line args (skip the "--" separator if present)
string[] inputArgs = args;

// Try to match and execute
MatchResult matchResult = router.Match(inputArgs);

if (matchResult.IsMatch)
{
  object? result = matchResult.Execute();
  Console.WriteLine(result);
}
else
{
  Console.WriteLine($"Error: No route matched for '{string.Join(" ", inputArgs)}'");
  Console.WriteLine($"Reason: {matchResult.FailureReason}");
  Environment.ExitCode = 1;
}

// =============================================================================
// RUNTIME TYPES - What the source generator needs to produce
// =============================================================================

/// <summary>
/// A compiled route ready for runtime matching and execution.
/// This is what gets generated from a RouteDefinition.
/// </summary>
public sealed class CompiledRoute
{
  public string Pattern { get; }
  public ImmutableArray<ISegmentMatcher> SegmentMatchers { get; }
  public ImmutableArray<ParameterExtractor> ParameterExtractors { get; }
  public Func<Dictionary<string, object>, object?> Invoker { get; }

  private CompiledRoute
  (
    string pattern,
    ImmutableArray<ISegmentMatcher> segmentMatchers,
    ImmutableArray<ParameterExtractor> parameterExtractors,
    Func<Dictionary<string, object>, object?> invoker
  )
  {
    Pattern = pattern;
    SegmentMatchers = segmentMatchers;
    ParameterExtractors = parameterExtractors;
    Invoker = invoker;
  }

  public static CompiledRoute FromDefinition
  (
    RouteDefinition definition,
    Func<Dictionary<string, object>, object?> invoker
  )
  {
    // Build segment matchers from the definition
    ImmutableArray<ISegmentMatcher>.Builder matcherBuilder = ImmutableArray.CreateBuilder<ISegmentMatcher>();
    ImmutableArray<ParameterExtractor>.Builder extractorBuilder = ImmutableArray.CreateBuilder<ParameterExtractor>();

    foreach (SegmentDefinition segment in definition.Segments)
    {
      switch (segment)
      {
        case LiteralDefinition literal:
          matcherBuilder.Add(new LiteralMatcher(literal.Position, literal.Value));
          break;

        case ParameterDefinition param:
          // Create matcher based on type constraint
          ISegmentMatcher matcher = param.TypeConstraint switch
          {
            "int" => new IntParameterMatcher(param.Position, param.Name),
            "string" or null => new StringParameterMatcher(param.Position, param.Name),
            _ => new StringParameterMatcher(param.Position, param.Name)
          };
          matcherBuilder.Add(matcher);

          // Create extractor for this parameter
          ParameterExtractor extractor = param.TypeConstraint switch
          {
            "int" => new ParameterExtractor(param.Position, param.Name, TypeConverter.ToInt32),
            "string" or null => new ParameterExtractor(param.Position, param.Name, TypeConverter.ToString),
            _ => new ParameterExtractor(param.Position, param.Name, TypeConverter.ToString)
          };
          extractorBuilder.Add(extractor);
          break;
      }
    }

    return new CompiledRoute
    (
      pattern: definition.OriginalPattern,
      segmentMatchers: matcherBuilder.ToImmutable(),
      parameterExtractors: extractorBuilder.ToImmutable(),
      invoker: invoker
    );
  }

  /// <summary>
  /// Attempts to match the given args against this route.
  /// </summary>
  public RouteMatchAttempt TryMatch(string[] args)
  {
    // Check segment count matches
    if (args.Length != SegmentMatchers.Length)
    {
      return RouteMatchAttempt.Failed($"Expected {SegmentMatchers.Length} segments, got {args.Length}");
    }

    // Check each segment matches
    for (int i = 0; i < SegmentMatchers.Length; i++)
    {
      ISegmentMatcher matcher = SegmentMatchers[i];
      string arg = args[i];

      if (!matcher.Matches(arg))
      {
        return RouteMatchAttempt.Failed($"Segment {i} '{arg}' does not match {matcher.Description}");
      }
    }

    // Extract parameters
    Dictionary<string, object> parameters = new();
    foreach (ParameterExtractor extractor in ParameterExtractors)
    {
      string rawValue = args[extractor.Position];
      object? converted = extractor.Convert(rawValue);
      if (converted is null)
      {
        return RouteMatchAttempt.Failed($"Failed to convert '{rawValue}' for parameter '{extractor.Name}'");
      }
      parameters[extractor.Name] = converted;
    }

    return RouteMatchAttempt.Succeeded(this, parameters);
  }
}

/// <summary>
/// Result of attempting to match a route.
/// </summary>
public sealed class RouteMatchAttempt
{
  public bool IsSuccess { get; }
  public string? FailureReason { get; }
  public CompiledRoute? Route { get; }
  public Dictionary<string, object>? Parameters { get; }

  private RouteMatchAttempt
  (
    bool isSuccess,
    string? failureReason,
    CompiledRoute? route,
    Dictionary<string, object>? parameters
  )
  {
    IsSuccess = isSuccess;
    FailureReason = failureReason;
    Route = route;
    Parameters = parameters;
  }

  public static RouteMatchAttempt Failed(string reason) =>
    new(isSuccess: false, failureReason: reason, route: null, parameters: null);

  public static RouteMatchAttempt Succeeded(CompiledRoute route, Dictionary<string, object> parameters) =>
    new(isSuccess: true, failureReason: null, route: route, parameters: parameters);
}

/// <summary>
/// Interface for segment matchers.
/// </summary>
public interface ISegmentMatcher
{
  int Position { get; }
  string Description { get; }
  bool Matches(string value);
}

/// <summary>
/// Matches a literal (fixed text) segment.
/// </summary>
public sealed class LiteralMatcher : ISegmentMatcher
{
  public int Position { get; }
  public string ExpectedValue { get; }
  public string Description => $"literal '{ExpectedValue}'";

  public LiteralMatcher(int position, string expectedValue)
  {
    Position = position;
    ExpectedValue = expectedValue;
  }

  public bool Matches(string value) =>
    string.Equals(value, ExpectedValue, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Matches a string parameter (any non-empty value).
/// </summary>
public sealed class StringParameterMatcher : ISegmentMatcher
{
  public int Position { get; }
  public string Name { get; }
  public string Description => $"parameter '{Name}' (string)";

  public StringParameterMatcher(int position, string name)
  {
    Position = position;
    Name = name;
  }

  public bool Matches(string value) => !string.IsNullOrEmpty(value);
}

/// <summary>
/// Matches an int parameter (must be parseable as int).
/// </summary>
public sealed class IntParameterMatcher : ISegmentMatcher
{
  public int Position { get; }
  public string Name { get; }
  public string Description => $"parameter '{Name}' (int)";

  public IntParameterMatcher(int position, string name)
  {
    Position = position;
    Name = name;
  }

  public bool Matches(string value) => int.TryParse(value, out _);
}

/// <summary>
/// Extracts and converts a parameter value from args.
/// </summary>
public sealed class ParameterExtractor
{
  public int Position { get; }
  public string Name { get; }
  public Func<string, object?> Convert { get; }

  public ParameterExtractor(int position, string name, Func<string, object?> convert)
  {
    Position = position;
    Name = name;
    Convert = convert;
  }
}

/// <summary>
/// Type conversion utilities.
/// </summary>
public static class TypeConverter
{
  public static object? ToInt32(string value) =>
    int.TryParse(value, out int result) ? result : null;

  public static object? ToString(string value) => value;
}

/// <summary>
/// The router that matches args against registered routes.
/// </summary>
public sealed class Router
{
  private readonly ImmutableArray<CompiledRoute> Routes;

  public Router(ImmutableArray<CompiledRoute> routes)
  {
    Routes = routes;
  }

  public MatchResult Match(string[] args)
  {
    foreach (CompiledRoute route in Routes)
    {
      RouteMatchAttempt attempt = route.TryMatch(args);
      if (attempt.IsSuccess)
      {
        return MatchResult.Matched(attempt.Route!, attempt.Parameters!);
      }
    }

    return MatchResult.NoMatch("No routes matched the input");
  }
}

/// <summary>
/// Result of router matching, with ability to execute.
/// </summary>
public sealed class MatchResult
{
  public bool IsMatch { get; }
  public string? FailureReason { get; }
  private readonly CompiledRoute? Route;
  private readonly Dictionary<string, object>? Parameters;

  private MatchResult
  (
    bool isMatch,
    string? failureReason,
    CompiledRoute? route,
    Dictionary<string, object>? parameters
  )
  {
    IsMatch = isMatch;
    FailureReason = failureReason;
    Route = route;
    Parameters = parameters;
  }

  public static MatchResult NoMatch(string reason) =>
    new(isMatch: false, failureReason: reason, route: null, parameters: null);

  public static MatchResult Matched(CompiledRoute route, Dictionary<string, object> parameters) =>
    new(isMatch: true, failureReason: null, route: route, parameters: parameters);

  public object? Execute()
  {
    if (!IsMatch || Route is null || Parameters is null)
    {
      throw new InvalidOperationException("Cannot execute a non-matched result");
    }
    return Route.Invoker(Parameters);
  }
}

// =============================================================================
// DESIGN-TIME MODEL TYPES (copied from step-1)
// =============================================================================

/// <summary>
/// Design-time representation of a complete route definition.
/// </summary>
public sealed record RouteDefinition
(
  string OriginalPattern,
  ImmutableArray<SegmentDefinition> Segments,
  string MessageType,
  string? Description,
  HandlerDefinition Handler,
  PipelineDefinition? Pipeline,
  ImmutableArray<string> Aliases,
  string? GroupPrefix,
  int ComputedSpecificity,
  int Order
)
{
  public static RouteDefinition Create
  (
    string originalPattern,
    ImmutableArray<SegmentDefinition> segments,
    HandlerDefinition handler,
    string messageType = "Unspecified",
    string? description = null,
    PipelineDefinition? pipeline = null,
    ImmutableArray<string>? aliases = null,
    string? groupPrefix = null,
    int computedSpecificity = 0,
    int order = 0
  )
  {
    return new RouteDefinition
    (
      OriginalPattern: originalPattern,
      Segments: segments,
      MessageType: messageType,
      Description: description,
      Handler: handler,
      Pipeline: pipeline,
      Aliases: aliases ?? [],
      GroupPrefix: groupPrefix,
      ComputedSpecificity: computedSpecificity,
      Order: order
    );
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
public sealed record ParameterDefinition
(
  int Position,
  string Name,
  string? TypeConstraint,
  string? Description,
  bool IsOptional,
  bool IsCatchAll,
  string? ResolvedClrTypeName,
  string? DefaultValue = null
)
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
public sealed record OptionDefinition
(
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
  string? ResolvedClrTypeName
)
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
public sealed record HandlerDefinition
(
  HandlerKind HandlerKind,
  string? FullTypeName,
  string? MethodName,
  ImmutableArray<ParameterBinding> Parameters,
  HandlerReturnType ReturnType,
  bool IsAsync,
  bool RequiresCancellationToken,
  bool RequiresServiceProvider
)
{
  public static HandlerDefinition ForDelegate
  (
    ImmutableArray<ParameterBinding> parameters,
    HandlerReturnType returnType,
    bool isAsync,
    bool requiresCancellationToken = false
  )
  {
    return new HandlerDefinition
    (
      HandlerKind: HandlerKind.Delegate,
      FullTypeName: null,
      MethodName: null,
      Parameters: parameters,
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: requiresCancellationToken,
      RequiresServiceProvider: false
    );
  }

  public static HandlerDefinition ForMediator
  (
    string fullTypeName,
    ImmutableArray<ParameterBinding> parameters,
    HandlerReturnType returnType
  )
  {
    return new HandlerDefinition
    (
      HandlerKind: HandlerKind.Mediator,
      FullTypeName: fullTypeName,
      MethodName: null,
      Parameters: parameters,
      ReturnType: returnType,
      IsAsync: true,
      RequiresCancellationToken: true,
      RequiresServiceProvider: true
    );
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
public sealed record HandlerReturnType
(
  string FullTypeName,
  string ShortTypeName,
  bool IsVoid,
  bool IsTask,
  string? UnwrappedTypeName
)
{
  public static readonly HandlerReturnType Void = new
  (
    FullTypeName: "void",
    ShortTypeName: "void",
    IsVoid: true,
    IsTask: false,
    UnwrappedTypeName: null
  );

  public static readonly HandlerReturnType Task = new
  (
    FullTypeName: "global::System.Threading.Tasks.Task",
    ShortTypeName: "Task",
    IsVoid: false,
    IsTask: true,
    UnwrappedTypeName: null
  );

  public static HandlerReturnType TaskOf(string innerTypeName, string shortInnerTypeName) => new
  (
    FullTypeName: $"global::System.Threading.Tasks.Task<{innerTypeName}>",
    ShortTypeName: $"Task<{shortInnerTypeName}>",
    IsVoid: false,
    IsTask: true,
    UnwrappedTypeName: innerTypeName
  );

  public static HandlerReturnType Of(string fullTypeName, string shortTypeName) => new
  (
    FullTypeName: fullTypeName,
    ShortTypeName: shortTypeName,
    IsVoid: false,
    IsTask: false,
    UnwrappedTypeName: null
  );

  public bool HasValue => !IsVoid && (!IsTask || UnwrappedTypeName is not null);
}

/// <summary>
/// Represents how a handler parameter is bound from route data or services.
/// </summary>
public sealed record ParameterBinding
(
  string ParameterName,
  string ParameterTypeName,
  BindingSource Source,
  string SourceName,
  bool IsOptional,
  bool IsArray,
  string? DefaultValueExpression,
  bool RequiresConversion,
  string? ConverterTypeName
)
{
  public static ParameterBinding FromParameter
  (
    string parameterName,
    string typeName,
    string segmentName,
    bool isOptional = false,
    string? defaultValue = null,
    bool requiresConversion = false
  )
  {
    return new ParameterBinding
    (
      ParameterName: parameterName,
      ParameterTypeName: typeName,
      Source: BindingSource.Parameter,
      SourceName: segmentName,
      IsOptional: isOptional,
      IsArray: false,
      DefaultValueExpression: defaultValue,
      RequiresConversion: requiresConversion,
      ConverterTypeName: null
    );
  }

  public static ParameterBinding FromOption
  (
    string parameterName,
    string typeName,
    string optionName,
    bool isOptional = false,
    bool isArray = false,
    string? defaultValue = null,
    bool requiresConversion = false
  )
  {
    return new ParameterBinding
    (
      ParameterName: parameterName,
      ParameterTypeName: typeName,
      Source: BindingSource.Option,
      SourceName: optionName,
      IsOptional: isOptional,
      IsArray: isArray,
      DefaultValueExpression: defaultValue,
      RequiresConversion: requiresConversion,
      ConverterTypeName: null
    );
  }

  public static ParameterBinding FromFlag(string parameterName, string optionName)
  {
    return new ParameterBinding
    (
      ParameterName: parameterName,
      ParameterTypeName: "global::System.Boolean",
      Source: BindingSource.Flag,
      SourceName: optionName,
      IsOptional: true,
      IsArray: false,
      DefaultValueExpression: "false",
      RequiresConversion: false,
      ConverterTypeName: null
    );
  }

  public static ParameterBinding ForCancellationToken(string parameterName = "cancellationToken")
  {
    return new ParameterBinding
    (
      ParameterName: parameterName,
      ParameterTypeName: "global::System.Threading.CancellationToken",
      Source: BindingSource.CancellationToken,
      SourceName: "CancellationToken",
      IsOptional: false,
      IsArray: false,
      DefaultValueExpression: null,
      RequiresConversion: false,
      ConverterTypeName: null
    );
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
public sealed record PipelineDefinition
(
  ImmutableArray<MiddlewareDefinition> Middleware,
  bool HasAuthorization,
  bool HasValidation,
  bool HasLogging
)
{
  public static readonly PipelineDefinition Empty = new
  (
    Middleware: [],
    HasAuthorization: false,
    HasValidation: false,
    HasLogging: false
  );

  public bool HasMiddleware => Middleware.Length > 0;
}

/// <summary>
/// Design-time representation of a single middleware in the pipeline.
/// </summary>
public sealed record MiddlewareDefinition
(
  string FullTypeName,
  MiddlewareKind Kind,
  ExecutionPhase ExecutionPhase,
  int Order,
  ImmutableDictionary<string, string>? Configuration
);

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
