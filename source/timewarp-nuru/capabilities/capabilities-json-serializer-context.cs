namespace TimeWarp.Nuru;

/// <summary>
/// JSON serialization context for capabilities response with source generation support.
/// Enables AOT-compatible JSON serialization of capabilities metadata.
/// </summary>
[JsonSerializable(typeof(CapabilitiesResponse))]
[JsonSerializable(typeof(GroupCapability))]
[JsonSerializable(typeof(CommandCapability))]
[JsonSerializable(typeof(ParameterCapability))]
[JsonSerializable(typeof(OptionCapability))]
[JsonSerializable(typeof(IReadOnlyList<GroupCapability>))]
[JsonSerializable(typeof(IReadOnlyList<CommandCapability>))]
[JsonSerializable(typeof(IReadOnlyList<ParameterCapability>))]
[JsonSerializable(typeof(IReadOnlyList<OptionCapability>))]
[JsonSourceGenerationOptions(
  WriteIndented = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class CapabilitiesJsonSerializerContext : JsonSerializerContext;
