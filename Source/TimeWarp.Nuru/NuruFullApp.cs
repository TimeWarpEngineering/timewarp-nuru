namespace TimeWarp.Nuru;

/// <summary>
/// Factory methods for creating fully-featured Nuru applications with all extensions auto-wired.
/// This is the recommended entry point for enterprise CLI applications.
/// </summary>
/// <remarks>
/// <para>
/// <c>NuruFullApp.CreateBuilder</c> provides a batteries-included experience with:
/// </para>
/// <list type="bullet">
/// <item>Dependency Injection via Microsoft.Extensions.DependencyInjection</item>
/// <item>Configuration via appsettings.json, environment variables, command line</item>
/// <item>Auto-generated help for all routes</item>
/// <item>OpenTelemetry integration (when OTEL_EXPORTER_OTLP_ENDPOINT is set)</item>
/// <item>Console logging with timestamps</item>
/// <item>REPL mode support</item>
/// <item>Shell tab completion</item>
/// </list>
/// <para>
/// For a minimal setup without extensions, use <see cref="NuruApp.CreateSlimBuilder"/>
/// from the TimeWarp.Nuru.Core package.
/// </para>
/// </remarks>
public static class NuruFullApp
{
  /// <summary>
  /// Creates a fully-featured builder with all extensions auto-wired.
  /// </summary>
  /// <param name="args">Command line arguments.</param>
  /// <param name="options">Optional application options.</param>
  /// <returns>A fully configured <see cref="NuruAppBuilder"/>.</returns>
  /// <example>
  /// <code>
  /// NuruApp app = NuruFullApp.CreateBuilder(args)
  ///     .ConfigureServices(services => services.AddMediator())
  ///     .Map&lt;DeployCommand&gt;("deploy {env}")
  ///     .Build();
  ///
  /// await app.RunAsync(args);
  /// </code>
  /// </example>
  public static NuruAppBuilder CreateBuilder(string[] args, NuruApplicationOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(args);

    // Start with Core's full builder (DI, Config, AutoHelp)
    return NuruApp.CreateBuilder(args, options)
      .UseAllExtensions();
  }

  /// <summary>
  /// Creates a lightweight builder without extensions.
  /// Delegates to <see cref="NuruApp.CreateSlimBuilder"/>.
  /// </summary>
  /// <param name="args">Optional command line arguments.</param>
  /// <param name="options">Optional application options.</param>
  /// <returns>A lightweight <see cref="NuruAppBuilder"/>.</returns>
  public static NuruAppBuilder CreateSlimBuilder(string[]? args = null, NuruApplicationOptions? options = null)
  {
    return NuruApp.CreateSlimBuilder(args, options);
  }

  /// <summary>
  /// Creates a bare minimum builder with only type converters.
  /// Delegates to <see cref="NuruApp.CreateEmptyBuilder"/>.
  /// </summary>
  /// <param name="args">Optional command line arguments.</param>
  /// <param name="options">Optional application options.</param>
  /// <returns>An empty <see cref="NuruAppBuilder"/>.</returns>
  public static NuruAppBuilder CreateEmptyBuilder(string[]? args = null, NuruApplicationOptions? options = null)
  {
    return NuruApp.CreateEmptyBuilder(args, options);
  }
}
