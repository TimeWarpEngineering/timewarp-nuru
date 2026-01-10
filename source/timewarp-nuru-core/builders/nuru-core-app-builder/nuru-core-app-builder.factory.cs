namespace TimeWarp.Nuru;

/// <summary>
/// Factory methods for creating NuruCoreAppBuilder instances.
/// </summary>
public partial class NuruCoreAppBuilder<TSelf>
{
  private protected readonly NuruCoreApplicationOptions? ApplicationOptions;

  /// <summary>
  /// Initializes a new instance of the <see cref="NuruCoreAppBuilder"/> class with default settings.
  /// Use <see cref="NuruCoreApp.CreateSlimBuilder(string[])"/> or <see cref="NuruApp.CreateBuilder(string[])"/> factory methods instead.
  /// </summary>
  internal NuruCoreAppBuilder() : this(null) { }

  /// <summary>
  /// Internal constructor for factory methods with specific builder mode.
  /// </summary>
#pragma warning disable CA2214 // Do not call overridable methods in constructors - intentional for derived class initialization
  internal NuruCoreAppBuilder(NuruCoreApplicationOptions? options)
  {
    ApplicationOptions = options;
  }
#pragma warning restore CA2214
}

/// <summary>
/// Non-generic NuruCoreAppBuilder for use with factory methods.
/// This is the concrete class returned by <see cref="NuruCoreApp.CreateSlimBuilder(string[])"/>
/// and <see cref="NuruCoreApp.CreateEmptyBuilder()"/>.
/// </summary>
/// <remarks>
/// This class exists to provide a non-generic entry point for the CRTP pattern.
/// For derived builders (like NuruAppBuilder), extend <see cref="NuruCoreAppBuilder{TSelf}"/> directly.
/// </remarks>
public class NuruCoreAppBuilder : NuruCoreAppBuilder<NuruCoreAppBuilder>
{
  /// <summary>
  /// Initializes a new instance of the <see cref="NuruCoreAppBuilder"/> class with default settings.
  /// </summary>
  internal NuruCoreAppBuilder() : base() { }

  /// <summary>
  /// Initializes a new instance of the <see cref="NuruCoreAppBuilder"/> class with the specified mode and options.
  /// </summary>
  /// <param name="options">Optional application options.</param>
  internal NuruCoreAppBuilder(NuruCoreApplicationOptions? options) : base(options) { }
}
