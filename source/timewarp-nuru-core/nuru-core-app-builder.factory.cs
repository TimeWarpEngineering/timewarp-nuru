namespace TimeWarp.Nuru;

/// <summary>
/// Defines the initialization mode for the builder, determining which features are included.
/// </summary>
public enum BuilderMode
{
  /// <summary>
  /// Empty builder - bare minimum with type converters only.
  /// User has total control over what features to add.
  /// </summary>
  Empty,

  /// <summary>
  /// Slim builder - lightweight with auto-help enabled.
  /// No DI container, Configuration, REPL, or Completion.
  /// Use for simple delegate-only CLIs that don't need dependency injection.
  /// </summary>
  Slim,

  /// <summary>
  /// Full builder - DI, Configuration, and auto-help enabled.
  /// For Mediator support, install Mediator.Abstractions + Mediator.SourceGenerator packages
  /// and call services.AddMediator() in ConfigureServices.
  /// Best for complex applications that need enterprise patterns.
  /// </summary>
  Full
}

/// <summary>
/// Factory methods for creating NuruCoreAppBuilder instances.
/// </summary>
public partial class NuruCoreAppBuilder
{
  private protected readonly NuruCoreApplicationOptions? ApplicationOptions;
  private protected readonly BuilderMode Mode;

  /// <summary>
  /// Initializes a new instance of the <see cref="NuruCoreAppBuilder"/> class with default settings.
  /// Uses Empty mode for backward compatibility - no auto-help or other features enabled.
  /// Use <see cref="NuruCoreApp.CreateSlimBuilder(string[])"/> or <see cref="NuruApp.CreateBuilder(string[])"/> factory methods instead.
  /// </summary>
  internal NuruCoreAppBuilder() : this(BuilderMode.Empty, null)
  {
  }

  /// <summary>
  /// Internal constructor for factory methods with specific builder mode.
  /// </summary>
#pragma warning disable CA2214 // Do not call overridable methods in constructors - intentional for derived class initialization
  internal NuruCoreAppBuilder(BuilderMode mode, NuruCoreApplicationOptions? options)
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
#pragma warning restore CA2214

  /// <summary>
  /// Initializes the builder based on the specified mode.
  /// </summary>
  protected virtual void InitializeForMode(BuilderMode mode, string[]? args)
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
