namespace TimeWarp.Nuru;

/// <summary>
/// Machine-readable metadata about CLI capabilities for AI agent discovery.
/// Returned by the <c>--capabilities</c> flag, and in process by <see cref="NuruApp.Capabilities"/>.
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

  /// <summary>
  /// Returns the endpoints whose group path starts with <paramref name="groupFilter"/>.
  /// </summary>
  /// <remarks>
  /// The filter is dot-separated (<c>admin.config</c>) and each segment is compared ignoring case,
  /// the same rule <c>--capabilities --group-filter</c> uses. The result carries
  /// <see cref="Filter"/> set to the filter.
  /// </remarks>
  /// <param name="groupFilter">Dot-separated group prefix.</param>
  /// <returns>A new response with the matching endpoints.</returns>
  public CapabilitiesResponse FilterByGroup(string groupFilter)
  {
    ArgumentNullException.ThrowIfNull(groupFilter);

    string[] groupParts = groupFilter.Split('.');
    EndpointCapability[] endpoints =
    [
      .. Endpoints.Where(e => e.GroupPath.Count >= groupParts.Length &&
        groupParts.Zip(e.GroupPath).All(p => string.Equals(p.First, p.Second, StringComparison.OrdinalIgnoreCase)))
    ];

    return new CapabilitiesResponse
    {
      Name = Name,
      Version = Version,
      Description = Description,
      Filter = new CapabilitiesFilter { Group = groupFilter },
      Endpoints = Array.AsReadOnly(endpoints),
      Invocation = Invocation
    };
  }
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

  // Routes without examples serialize with no examples member
  // (DefaultIgnoreCondition.WhenWritingNull on the serializer context omits it).
  public IReadOnlyList<ExampleCapability>? Examples { get; init; }

  // Last property, so it is the only addition to each endpoint in existing documents.
  // Nullable so a document from a CLI that predates it still loads.
  public ReturnTypeCapability? ReturnType { get; init; }
}

/// <summary>
/// What an endpoint's handler returns, so a host that maps the endpoint to another protocol
/// knows whether to expect no value, a scalar, or a JSON document.
/// </summary>
public sealed class ReturnTypeCapability
{
  /// <summary>
  /// CLR type name of the result, without <c>global::</c> (for example <c>string</c>,
  /// <c>MyApp.Report</c>, or <c>void</c>). For an async handler this is the awaited type.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>How the CLI writes this result.</summary>
  public required ReturnTypeKind Kind { get; init; }
}

/// <summary>
/// How the CLI writes an endpoint's result. Matches the generated output strategy.
/// </summary>
[JsonConverter(typeof(ReturnTypeKindConverter))]
public enum ReturnTypeKind
{
  /// <summary>No value: <c>void</c>, <c>Task</c>, or <c>TimeWarp.Mediator.Unit</c>.</summary>
  Unit,

  /// <summary>
  /// A scalar written as text: string, numeric, bool, char, Guid, date and time types, TimeSpan.
  /// </summary>
  Primitive,

  /// <summary>Any other type. The CLI serializes it as JSON.</summary>
  Json
}

/// <summary>
/// Serializes <see cref="ReturnTypeKind"/> enum values using camelCase naming.
/// </summary>
public sealed class ReturnTypeKindConverter : JsonStringEnumConverter<ReturnTypeKind>
{
  public ReturnTypeKindConverter() : base(JsonNamingPolicy.CamelCase) { }
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
