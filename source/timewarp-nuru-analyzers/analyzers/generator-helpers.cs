namespace TimeWarp.Nuru;

/// <summary>
/// Shared helper methods for source generators.
/// </summary>
internal static class GeneratorHelpers
{
  /// <summary>
  /// Gets an incremental value provider that indicates whether UseNewGen is enabled.
  /// When true, V1 generators should skip and V2 generators should run.
  /// </summary>
  /// <param name="context">The generator initialization context.</param>
  /// <returns>A provider that yields true if UseNewGen=true, false otherwise.</returns>
  public static IncrementalValueProvider<bool> GetUseNewGenProvider(
    IncrementalGeneratorInitializationContext context)
  {
    return context.AnalyzerConfigOptionsProvider
      .Select(static (provider, _) =>
      {
        provider.GlobalOptions.TryGetValue("build_property.UseNewGen", out string? value);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
      });
  }
}
