namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Design-time representation of a custom type converter.
/// This captures information from AddTypeConverter() calls for code generation.
/// </summary>
/// <param name="ConverterTypeName">Fully qualified converter type name (e.g., "EmailAddressConverter")</param>
/// <param name="TargetTypeName">Fully qualified target type name (e.g., "EmailAddress")</param>
/// <param name="ConstraintAlias">Optional alias for the constraint (e.g., "email"), null if only type name works</param>
public sealed record CustomConverterDefinition(
  string ConverterTypeName,
  string TargetTypeName,
  string? ConstraintAlias);
