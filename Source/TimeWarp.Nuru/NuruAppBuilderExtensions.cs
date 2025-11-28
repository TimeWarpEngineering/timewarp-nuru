namespace TimeWarp.Nuru;

/// <summary>
/// Extension methods that auto-wire all Nuru extensions to an existing builder.
/// </summary>
public static class NuruAppBuilderExtensions
{
  /// <summary>
  /// Adds all Nuru extensions to the builder: Telemetry, Logging, REPL, and Completion.
  /// This is called automatically by <see cref="NuruFullApp.CreateBuilder"/>.
  /// </summary>
  /// <param name="builder">The builder to extend.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// This method adds:
  /// <list type="bullet">
  /// <item>OpenTelemetry integration via <c>UseTelemetry()</c></item>
  /// <item>REPL mode support via <c>AddReplSupport()</c></item>
  /// </list>
  /// Logging is already configured by <c>UseTelemetry()</c>.
  /// Completion is already included via the REPL package dependency.
  /// </remarks>
  public static NuruAppBuilder UseAllExtensions(this NuruAppBuilder builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    return builder
      .UseTelemetry()
      .AddReplSupport();
  }
}
