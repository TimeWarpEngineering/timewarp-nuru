namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Constants for built-in CLI flags (--help, -h, --version, --capabilities).
/// Centralizes flag definitions to avoid hardcoding in multiple emitters.
/// </summary>
internal static class BuiltInFlags
{
  /// <summary>
  /// Help flag forms: --help, -h
  /// </summary>
  public static readonly string[] HelpForms = ["--help", "-h"];

  /// <summary>
  /// Version flag form: --version
  /// </summary>
  public static readonly string[] VersionForms = ["--version"];

  /// <summary>
  /// Capabilities flag form: --capabilities (for AI tools)
  /// </summary>
  public static readonly string[] CapabilitiesForms = ["--capabilities"];

  /// <summary>
  /// All built-in flag forms combined.
  /// </summary>
  public static readonly string[] All = ["--help", "-h", "--version", "--capabilities"];

  /// <summary>
  /// Pattern match expression for checking if a single-element array is a built-in flag.
  /// Used in generated code: if (routeArgs is [PatternMatchExpression])
  /// </summary>
  public const string PatternMatchExpression = "[\"--help\" or \"-h\"] or [\"--version\"] or [\"--capabilities\"]";

  /// <summary>
  /// Pattern match expression for checking if route OriginalPattern is a built-in flag route.
  /// Used in source generator analysis: route.OriginalPattern is IsBuiltInFlagRoutePattern
  /// </summary>
  public const string IsBuiltInFlagRoutePattern = "\"--help\" or \"-h\" or \"--version\" or \"--capabilities\"";
}
