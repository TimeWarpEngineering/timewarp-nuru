#region Purpose
// Source-generated JSON context for the capabilities response, keeping serialization AOT-compatible.
#endregion

#region Design
// A source-generated context is used so the `--capabilities` document serializes without
// reflection under Native AOT and trimming. The generated --capabilities handler serializes
// through CapabilitiesResponse's type info, and CapabilitiesClient deserializes with the same
// context so producer and consumer share one contract. CamelCase plus WhenWritingNull is part of
// that contract: absent optional members are omitted, which lets older documents still load.
// Every capability type must be registered here or serialization fails under AOT.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// JSON serialization context for capabilities response with source generation support.
/// Enables AOT-compatible JSON serialization of capabilities metadata.
/// </summary>
[JsonSerializable(typeof(CapabilitiesResponse))]
[JsonSerializable(typeof(CapabilitiesFilter))]
[JsonSerializable(typeof(EndpointCapability))]
[JsonSerializable(typeof(EndpointKind))]
[JsonSerializable(typeof(ParameterCapability))]
[JsonSerializable(typeof(OptionCapability))]
[JsonSerializable(typeof(ExampleCapability))]
[JsonSerializable(typeof(InvocationCapability))]
[JsonSerializable(typeof(ReturnTypeCapability))]
[JsonSerializable(typeof(ReturnTypeKind))]
[JsonSerializable(typeof(IReadOnlyList<EndpointCapability>))]
[JsonSerializable(typeof(IReadOnlyList<ParameterCapability>))]
[JsonSerializable(typeof(IReadOnlyList<OptionCapability>))]
[JsonSerializable(typeof(IReadOnlyList<ExampleCapability>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSourceGenerationOptions(
  WriteIndented = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class CapabilitiesJsonSerializerContext : JsonSerializerContext;
