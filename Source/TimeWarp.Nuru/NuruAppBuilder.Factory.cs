namespace TimeWarp.Nuru;

/// <summary>
/// Defines the initialization mode for the builder, determining which features are included.
/// </summary>
internal enum BuilderMode
{
  /// <summary>
  /// Empty builder - bare minimum with type converters only.
  /// User has total control over what features to add.
  /// </summary>
  Empty,

  /// <summary>
  /// Slim builder - lightweight with Configuration, auto-help, and logging infrastructure.
  /// No DI container, Mediator, REPL, or Completion.
  /// </summary>
  Slim,

  /// <summary>
  /// Full builder - all features enabled including DI, Mediator, REPL, Completion.
  /// Best for complex applications that need enterprise patterns.
  /// </summary>
  Full
}

/// <summary>
/// Factory methods for creating NuruAppBuilder instances.
/// </summary>
public partial class NuruAppBuilder
{
  private readonly NuruApplicationOptions? ApplicationOptions;
  private readonly BuilderMode Mode;

  /// <summary>
  /// Initializes a new instance of the <see cref="NuruAppBuilder"/> class with default settings.
  /// Uses Empty mode for backward compatibility - no auto-help or other features enabled.
  /// Use CreateSlimBuilder() or CreateBuilder() for auto-configured builders.
  /// </summary>
  public NuruAppBuilder() : this(BuilderMode.Empty, null)
  {
  }

  /// <summary>
  /// Internal constructor for factory methods with specific builder mode.
  /// </summary>
  internal NuruAppBuilder(BuilderMode mode, NuruApplicationOptions? options)
  {
    Mode = mode;
    ApplicationOptions = options;

    // Apply options if provided
    if (options?.ApplicationName is not null || options?.EnvironmentName is not null)
    {
      AppMetadata = new ApplicationMetadata(options.ApplicationName, null);
    }

    // Initialize based on mode
    InitializeForMode(mode, options?.Args);
  }

  /// <summary>
  /// Initializes the builder based on the specified mode.
  /// </summary>
  private void InitializeForMode(BuilderMode mode, string[]? args)
  {
    switch (mode)
    {
      case BuilderMode.Full:
        // Full featured: DI, Configuration, auto-help, logging infrastructure
        AddDependencyInjection();
        AddConfiguration(args);
        AddAutoHelp();
        break;

      case BuilderMode.Slim:
        // Lightweight: Configuration, auto-help, logging infrastructure (no DI)
        // Note: Configuration requires DI, so for Slim we skip Configuration
        // but keep auto-help enabled
        AddAutoHelp();
        break;

      case BuilderMode.Empty:
        // Bare minimum: only type converters (already initialized)
        // User has total control
        break;
    }
  }
}
