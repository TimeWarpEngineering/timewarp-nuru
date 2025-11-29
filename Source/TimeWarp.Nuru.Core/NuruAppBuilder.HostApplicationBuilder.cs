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
  private Dictionary<object, object>? PropertiesDictionary;

  /// <summary>
  /// Gets the set of key/value configuration properties.
  /// </summary>
  /// <remarks>
  /// Returns a ConfigurationManager that can be used to configure the application.
  /// If AddConfiguration() was called, existing configuration values are copied to the manager.
  /// </remarks>
  public IConfigurationManager HostConfiguration
  {
    get
    {
      ConfigurationManager ??= new ConfigurationManager();

      // If we have existing configuration, copy it to the manager
      if (Configuration is not null)
      {
        foreach (KeyValuePair<string, string?> kvp in Configuration.AsEnumerable())
        {
          if (kvp.Value is not null)
          {
            ConfigurationManager[kvp.Key] = kvp.Value;
          }
        }
      }

      return ConfigurationManager;
    }
  }

  /// <summary>
  /// Gets the information about the hosting environment an application is running in.
  /// </summary>
  public IHostEnvironment HostEnvironment
  {
    get
    {
      if (NuruHostEnvironment is null)
      {
        string environmentName = ApplicationOptions?.EnvironmentName
          ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
          ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
          ?? "Production";

        string applicationName = ApplicationOptions?.ApplicationName
          ?? Assembly.GetEntryAssembly()?.GetName().Name
          ?? "NuruApp";

        string contentRootPath = ApplicationOptions?.ContentRootPath
          ?? AppContext.BaseDirectory;

        NuruHostEnvironment = new NuruHostEnvironment(
          environmentName,
          applicationName,
          contentRootPath);
      }

      return NuruHostEnvironment;
    }
  }

  /// <summary>
  /// Gets a collection of logging providers for the application to compose.
  /// </summary>
  public ILoggingBuilder Logging
  {
    get
    {
      NuruLoggingBuilder ??= new NuruLoggingBuilder(Services);
      return NuruLoggingBuilder;
    }
  }

  /// <summary>
  /// Gets a builder that allows enabling metrics and directing their output.
  /// </summary>
  public IMetricsBuilder Metrics
  {
    get
    {
      NuruMetricsBuilder ??= new NuruMetricsBuilder(Services);
      return NuruMetricsBuilder;
    }
  }

  /// <summary>
  /// Gets a central location for sharing state between components during the host building process.
  /// </summary>
  public IDictionary<object, object> Properties
  {
    get
    {
      PropertiesDictionary ??= [];
      return PropertiesDictionary;
    }
  }

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

/// <summary>
/// Simple IHostEnvironment implementation for Nuru applications.
/// </summary>
internal sealed class NuruHostEnvironment : IHostEnvironment
{
  public NuruHostEnvironment(string environmentName, string applicationName, string contentRootPath)
  {
    EnvironmentName = environmentName;
    ApplicationName = applicationName;
    ContentRootPath = contentRootPath;
    ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
  }

  public string EnvironmentName { get; set; }
  public string ApplicationName { get; set; }
  public string ContentRootPath { get; set; }
  public IFileProvider ContentRootFileProvider { get; set; }
}

/// <summary>
/// Simple ILoggingBuilder implementation that wraps the service collection.
/// </summary>
internal sealed class NuruLoggingBuilder : ILoggingBuilder
{
  public NuruLoggingBuilder(IServiceCollection services)
  {
    Services = services;
  }

  public IServiceCollection Services { get; }
}

/// <summary>
/// Simple IMetricsBuilder implementation that wraps the service collection.
/// </summary>
internal sealed class NuruMetricsBuilder : IMetricsBuilder
{
  public NuruMetricsBuilder(IServiceCollection services)
  {
    Services = services;
  }

  public IServiceCollection Services { get; }
}
