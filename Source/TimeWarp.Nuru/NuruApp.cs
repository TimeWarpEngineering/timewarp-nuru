namespace TimeWarp.Nuru;

/// <summary>
/// Full-featured CLI app with all extensions auto-wired.
/// Inherits from <see cref="NuruCoreApp"/> and adds Telemetry, REPL, and Completion.
/// </summary>
public class NuruApp : NuruCoreApp
{
  public NuruApp(IServiceProvider serviceProvider) : base(serviceProvider)
  {
  }

  /// <summary>
  /// Creates a full-featured builder with DI, Configuration, and all extensions auto-wired.
  /// </summary>
  public static NuruAppBuilder CreateBuilder(string[] args, NuruCoreApplicationOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(args);
    options ??= new NuruCoreApplicationOptions();
    options.Args = args;

    // Create full builder with DI, Config, AutoHelp
    NuruAppBuilder builder = new(BuilderMode.Full, options);

    // Add all extensions (telemetry, REPL, completion)
    return builder.UseAllExtensions();
  }

  public new static NuruAppBuilder CreateSlimBuilder(string[]? args = null, NuruCoreApplicationOptions? options = null)
    => NuruCoreApp.CreateSlimBuilder(args, options);

  public new static NuruAppBuilder CreateEmptyBuilder(string[]? args = null, NuruCoreApplicationOptions? options = null)
    => NuruCoreApp.CreateEmptyBuilder(args, options);
}
