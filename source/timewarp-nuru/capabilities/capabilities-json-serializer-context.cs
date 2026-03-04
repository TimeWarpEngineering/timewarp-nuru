namespace TimeWarp.Nuru;

/// <summary>
/// JSON serialization context for capabilities response with source generation support.
/// Enables AOT-compatible JSON serialization of capabilities metadata.
/// </summary>
[JsonSerializable(typeof(CapabilitiesResponse))]
[JsonSerializable(typeof(EndpointCapability))]
[JsonSerializable(typeof(EndpointKind))]
[JsonSerializable(typeof(ParameterCapability))]
[JsonSerializable(typeof(OptionCapability))]
[JsonSerializable(typeof(IReadOnlyList<EndpointCapability>))]
[JsonSerializable(typeof(IReadOnlyList<ParameterCapability>))]
[JsonSerializable(typeof(IReadOnlyList<OptionCapability>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSourceGenerationOptions(
  WriteIndented = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class CapabilitiesJsonSerializerContext : JsonSerializerContext;
