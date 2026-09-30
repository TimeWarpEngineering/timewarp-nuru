namespace TimeWarp.Nuru;

/// <summary>
/// Machine-readable metadata about CLI capabilities for AI agent discovery.
/// Returned by the <c>--capabilities</c> flag.
/// </summary>
public sealed class CapabilitiesResponse
{
  public required string Name { get; init; }
  public required string Version { get; init; }
  public string? Description { get; init; }
  public CapabilitiesFilter? Filter { get; init; }
  public required IReadOnlyList<EndpointCapability> Endpoints { get; init; }

  // Omitted from JSON when null (WhenWritingNull), so a document without this property still loads.
  public InvocationCapability? Invocation { get; init; }
}

/// <summary>
/// Transport an agent uses to pass argument values for every generated app.
/// The same object is emitted on every <c>--capabilities</c> document.
/// </summary>
public sealed class InvocationCapability
{
  /// <summary>Flag an agent passes. The value is <c>-</c>, <c>@path</c>, or an inline JSON object.</summary>
  public const string JsonArgsFlag = "--json-args";

  /// <summary>Value of <see cref="JsonArgs"/> that reads stdin to EOF.</summary>
  public const string StdinValue = "-";

  /// <summary>Prefix on a <see cref="JsonArgs"/> value that names a filesystem path.</summary>
  public const string FilePrefixValue = "@";

  /// <summary>Merge rule: an argv value for the same name replaces the JSON value.</summary>
  public const string ArgvOverridesJsonMerge = "argvOverridesJson";

  /// <summary>Unknown JSON keys are an error.</summary>
  public const string UnknownKeysError = "error";

  public required string JsonArgs { get; init; }
  public required string Stdin { get; init; }
  public required string FilePrefix { get; init; }
  public required string Merge { get; init; }
  public required string UnknownKeys { get; init; }

  /// <summary>
  /// The invocation object every generated app emits.
  /// </summary>
  public static InvocationCapability Standard { get; } = new()
  {
    JsonArgs = JsonArgsFlag,
    Stdin = StdinValue,
    FilePrefix = FilePrefixValue,
    Merge = ArgvOverridesJsonMerge,
    UnknownKeys = UnknownKeysError
  };
}

/// <summary>
/// Filter metadata indicating which endpoints were included in the response.
/// </summary>
public sealed class CapabilitiesFilter
{
  public string? Group { get; init; }
}

/// <summary>
/// Metadata for a single CLI endpoint (command or query).
/// </summary>
public sealed class EndpointCapability
{
  public required string Pattern { get; init; }
  public required IReadOnlyList<string> GroupPath { get; init; }
  public IReadOnlyList<string> Aliases { get; init; } = [];
  public string? Description { get; init; }
  public required EndpointKind Kind { get; init; }
  public required IReadOnlyList<ParameterCapability> Parameters { get; init; }
  public required IReadOnlyList<OptionCapability> Options { get; init; }

  // Last property: routes without examples must serialize byte-identically to before this
  // feature existed (DefaultIgnoreCondition.WhenWritingNull on the serializer context omits it).
  public IReadOnlyList<ExampleCapability>? Examples { get; init; }
}

/// <summary>
/// The kind of endpoint, indicating AI agent safety level.
/// </summary>
[JsonConverter(typeof(EndpointKindConverter))]
public enum EndpointKind
{
  Query,
  Command,
  IdempotentCommand,
  Unspecified
}

/// <summary>
/// Serializes <see cref="EndpointKind"/> enum values using camelCase naming.
/// </summary>
public sealed class EndpointKindConverter : JsonStringEnumConverter<EndpointKind>
{
  public EndpointKindConverter() : base(JsonNamingPolicy.CamelCase) { }
}

/// <summary>
/// Metadata for a positional parameter.
/// </summary>
public sealed class ParameterCapability
{
  public required string Name { get; init; }
  public required string Type { get; init; }
  public bool Required { get; init; } = true;
  public bool IsCatchAll { get; init; }
  public string? Description { get; init; }
  public string? DefaultValue { get; init; }
  public IReadOnlyList<string>? AllowedValues { get; init; }
}

/// <summary>
/// Metadata for an option (flag or named argument).
/// </summary>
public sealed class OptionCapability
{
  public required string Name { get; init; }
  public string? Alias { get; init; }
  public required string Type { get; init; }
  public bool Required { get; init; }
  public bool IsFlag { get; init; }
  public bool IsRepeated { get; init; }
  public string? Description { get; init; }
  public string? DefaultValue { get; init; }
  public IReadOnlyList<string>? AllowedValues { get; init; }
}

/// <summary>
/// A concrete usage example for an endpoint, declared via <c>[NuruRouteExample]</c>
/// (Endpoint DSL) or <c>.WithExample()</c> (Fluent DSL).
/// </summary>
public sealed class ExampleCapability
{
  public required string Command { get; init; }
  public string? Description { get; init; }
}
