namespace TimeWarp.Nuru.Extensions;

/// <summary>
/// Extension methods for adding configuration support to Nuru applications.
/// </summary>
public static class ConfigurationExtensions
{
  /// <summary>
  /// Adds standard .NET configuration sources to the application.
  /// This includes appsettings.json, environment-specific settings, environment variables, and command line arguments.
  /// </summary>
  /// <param name="builder">The Nuru app builder.</param>
  /// <param name="args">Optional command line arguments to include in configuration.</param>
  /// <returns>The builder for chaining.</returns>
  public static NuruAppBuilder AddConfiguration(this NuruAppBuilder builder, string[]? args = null)
  {
    ArgumentNullException.ThrowIfNull(builder);

    // Ensure DI is enabled
    if (builder.Services is null)
    {
      throw new InvalidOperationException("Configuration requires dependency injection. Call AddDependencyInjection() first.");
    }

    IConfigurationBuilder configuration = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
      .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
      .AddEnvironmentVariables();

    if (args?.Length > 0)
    {
      configuration.AddCommandLine(args);
    }

    IConfigurationRoot configurationRoot = configuration.Build();
    builder.Services.AddSingleton<IConfiguration>(configurationRoot);

    return builder;
  }
}
