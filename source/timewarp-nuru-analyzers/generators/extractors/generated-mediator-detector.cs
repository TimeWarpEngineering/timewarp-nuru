#region Purpose
// Detects whether the TimeWarp.Mediator source generator emits AddGeneratedMediator() into this
// compilation, and in which namespace.
#endregion

#region Design
// A generator cannot see another generator's output, so the [assembly: GeneratedMediator] attribute
// that TimeWarp.Mediator 15 emits is not visible here. Detection uses the compiler-visible properties
// that the TimeWarp.Mediator.Generators buildTransitive props set instead:
// - TimeWarpMediatorGenerator is true only when the generator package is referenced. In 15 the
//   generator always emits AddGeneratedMediator() when it runs; TimeWarpMediatorAssembly=false is
//   analyzer membership only and does not stop emission, and there is no Aot/Link profile.
// - TimeWarpMediatorNamespace is the generated namespace (default TimeWarp.Mediator.Generated).
//   The registration class is that namespace with '.' removed plus ServiceCollectionExtensions.
// TimeWarp.Mediator.ISender must also resolve, so the contracts are referenced.
#endregion

namespace TimeWarp.Nuru.Generators;

using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Detects the TimeWarp.Mediator generated host registration.
/// </summary>
internal static class GeneratedMediatorDetector
{
  private const string GeneratorPropertyName = "build_property.TimeWarpMediatorGenerator";
  private const string NamespacePropertyName = "build_property.TimeWarpMediatorNamespace";
  private const string SenderMetadataName = "TimeWarp.Mediator.ISender";

  /// <summary>
  /// The default namespace the TimeWarp.Mediator generator emits into.
  /// </summary>
  public const string DefaultNamespace = "TimeWarp.Mediator.Generated";

  /// <summary>
  /// Returns the namespace of the generated mediator when the TimeWarp.Mediator generator runs in this
  /// compilation and emits <c>AddGeneratedMediator()</c>; otherwise null.
  /// </summary>
  public static string? Detect(Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider)
  {
    AnalyzerConfigOptions options = optionsProvider.GlobalOptions;

    if (!options.TryGetValue(GeneratorPropertyName, out string? isGenerator) ||
        !string.Equals(isGenerator, "true", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    if (compilation.GetTypeByMetadataName(SenderMetadataName) is null)
      return null;

    return options.TryGetValue(NamespacePropertyName, out string? generatedNamespace) &&
      !string.IsNullOrWhiteSpace(generatedNamespace)
        ? generatedNamespace.Trim()
        : DefaultNamespace;
  }

  /// <summary>
  /// The fully qualified <c>AddGeneratedMediator</c> registration method for <paramref name="generatedNamespace"/>.
  /// </summary>
  public static string RegistrationMethod(string generatedNamespace) =>
    $"global::{generatedNamespace}.{string.Concat(generatedNamespace.Split('.'))}ServiceCollectionExtensions.AddGeneratedMediator";
}
