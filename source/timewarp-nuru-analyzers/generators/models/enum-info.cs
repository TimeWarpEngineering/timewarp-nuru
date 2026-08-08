namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Value-equatable, precomputed enum type information for the emit stage.
/// </summary>
/// <remarks>
/// The emitters previously resolved enum member names by holding the live Roslyn
/// <see cref="Compilation"/> in the RegisterSourceOutput input, which forced the heavy emit
/// stage to re-run on every keystroke (the compilation changes on every edit). This record
/// carries only the resolved metadata name and member names — all value types — so the emit
/// model compares equal whenever the enum shape is unchanged, letting the emit stage cache.
/// </remarks>
/// <param name="MetadataTypeName">
/// The normalized metadata type name (no <c>global::</c> prefix, no trailing <c>?</c>) used as
/// the lookup key by the emitters.
/// </param>
/// <param name="MemberNames">Enum member names in declaration order.</param>
public sealed record EnumInfo(
  string MetadataTypeName,
  EquatableArray<string> MemberNames);
