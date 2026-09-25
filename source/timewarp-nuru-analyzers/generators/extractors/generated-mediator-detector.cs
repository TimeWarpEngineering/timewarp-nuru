// Detects whether the TimeWarp.Mediator source generator emits AddGeneratedMediator() into this compilation.
// A generator cannot see another generator's output, so detection uses the MSBuild properties that the
// TimeWarp.Mediator.Generators buildTransitive props expose as compiler-visible properties.

namespace TimeWarp.Nuru.Generators;

using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Detects the TimeWarp.Mediator generated host registration.
/// </summary>
internal static class GeneratedMediatorDetector
{
  private const string AssemblyPropertyName = "build_property.TimeWarpMediatorAssembly";
  private const string ProfilePropertyName = "build_property.TimeWarpMediatorProfile";
  private const string SenderMetadataName = "TimeWarp.Mediator.ISender";

  /// <summary>
  /// Returns true when the compilation is a TimeWarp.Mediator graph member whose profile emits
  /// <c>AddGeneratedMediator()</c> (the Aot and Link profiles do not).
  /// </summary>
  public static bool Detect(Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider)
  {
    AnalyzerConfigOptions options = optionsProvider.GlobalOptions;

    if (!options.TryGetValue(AssemblyPropertyName, out string? isMember) ||
        !string.Equals(isMember, "true", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    if (options.TryGetValue(ProfilePropertyName, out string? profile) &&
        (string.Equals(profile, "Aot", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(profile, "Link", StringComparison.OrdinalIgnoreCase)))
    {
      return false;
    }

    return compilation.GetTypeByMetadataName(SenderMetadataName) is not null;
  }
}
