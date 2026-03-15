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
