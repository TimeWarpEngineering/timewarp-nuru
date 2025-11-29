namespace TimeWarp.Nuru;

/// <summary>
/// IHostApplicationBuilder implementation for NuruAppBuilder.
/// Enables seamless integration with Aspire and other .NET ecosystem extensions.
/// </summary>
public partial class NuruAppBuilder : IHostApplicationBuilder
{
  private ConfigurationManager? ConfigurationManager;
  private NuruHostEnvironment? NuruHostEnvironment;
  private NuruLoggingBuilder? NuruLoggingBuilder;
  private NuruMetricsBuilder? NuruMetricsBuilder;
  private Dictionary<object, object> PropertiesDictionary = [];

  /// <summary>
  /// Initializes IHostApplicationBuilder fields that depend on Services.
  /// Called after AddDependencyInjection() in Full mode.
  /// </summary>
  private void InitializeHostApplicationBuilder()
  {
    ConfigurationManager = new ConfigurationManager();

    string environmentName = ApplicationOptions?.EnvironmentName
      ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
      ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
      ?? "Production";

    string applicationName = ApplicationOptions?.ApplicationName
      ?? Assembly.GetEntryAssembly()?.GetName().Name
      ?? "NuruApp";

    string contentRootPath = ApplicationOptions?.ContentRootPath
      ?? AppContext.BaseDirectory;

    NuruHostEnvironment = new NuruHostEnvironment(environmentName, applicationName, contentRootPath);
    NuruLoggingBuilder = new NuruLoggingBuilder(Services);
    NuruMetricsBuilder = new NuruMetricsBuilder(Services);
  }

  /// <summary>
  /// Gets the set of key/value configuration properties.
  /// </summary>
  public IConfigurationManager HostConfiguration =>
    ConfigurationManager ?? throw new InvalidOperationException(
      "HostConfiguration is not available. Use NuruApp.CreateBuilder() for IHostApplicationBuilder support.");

  /// <summary>
  /// Gets the information about the hosting environment an application is running in.
  /// </summary>
  public IHostEnvironment HostEnvironment =>
    NuruHostEnvironment ?? throw new InvalidOperationException(
      "HostEnvironment is not available. Use NuruApp.CreateBuilder() for IHostApplicationBuilder support.");

  /// <summary>
  /// Gets a collection of logging providers for the application to compose.
  /// </summary>
  public ILoggingBuilder Logging =>
    NuruLoggingBuilder ?? throw new InvalidOperationException(
      "Logging is not available. Use NuruApp.CreateBuilder() for IHostApplicationBuilder support.");

  /// <summary>
  /// Gets a builder that allows enabling metrics and directing their output.
  /// </summary>
  public IMetricsBuilder Metrics =>
    NuruMetricsBuilder ?? throw new InvalidOperationException(
      "Metrics is not available. Use NuruApp.CreateBuilder() for IHostApplicationBuilder support.");

  /// <summary>
  /// Gets a central location for sharing state between components during the host building process.
  /// </summary>
  public IDictionary<object, object> Properties => PropertiesDictionary;

  // Explicit interface implementations that delegate to the public properties.
  // CA1033 is suppressed because we expose public properties with equivalent functionality
  // (HostConfiguration/HostEnvironment) to avoid naming conflicts with existing Nuru properties.
#pragma warning disable CA1033 // Interface methods should be callable by child types
  IConfigurationManager IHostApplicationBuilder.Configuration => HostConfiguration;
  IHostEnvironment IHostApplicationBuilder.Environment => HostEnvironment;
#pragma warning restore CA1033

  /// <summary>
  /// Registers a IServiceProviderFactory instance to be used to create the IServiceProvider.
  /// </summary>
  void IHostApplicationBuilder.ConfigureContainer<TContainerBuilder>(
    IServiceProviderFactory<TContainerBuilder> factory,
    Action<TContainerBuilder>? configure)
  {
    // For now, we don't support custom container builders
    // This is a no-op that allows the interface to be satisfied
    // Most Aspire extensions don't use this method
  }
}
