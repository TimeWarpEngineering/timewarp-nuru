namespace TimeWarp.Nuru;

/// <summary>
/// A unified CLI app that supports both direct execution and dependency injection.
/// </summary>
public partial class NuruApp
{
  private readonly IServiceProvider? ServiceProvider;
  private readonly MediatorExecutor? MediatorExecutor;
  private readonly IConsole Console;

  /// <summary>
  /// Gets the logger factory.
  /// </summary>
  public ILoggerFactory LoggerFactory { get; }

  /// <summary>
  /// Gets the collection of registered endpoints.
  /// </summary>
  public EndpointCollection Endpoints { get; }

  /// <summary>
  /// Gets the type converter registry.
  /// </summary>
  public ITypeConverterRegistry TypeConverterRegistry { get; }

  /// <summary>
  /// Gets the REPL configuration options.
  /// </summary>
  public ReplOptions? ReplOptions { get; }

  /// <summary>
  /// Gets the application metadata for help display.
  /// </summary>
  public ApplicationMetadata? AppMetadata { get; }

  /// <summary>
  /// Direct constructor - no dependency injection.
  /// </summary>
  public NuruApp
  (
    EndpointCollection endpoints,
    ITypeConverterRegistry typeConverterRegistry,
    ILoggerFactory? loggerFactory = null,
    ReplOptions? replOptions = null,
    ApplicationMetadata? appMetadata = null,
    IConsole? console = null
  )
  {
    Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    TypeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
    LoggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
    ReplOptions = replOptions;
    AppMetadata = appMetadata;
    Console = console ?? NuruConsole.Default;

    // If logging is configured but DI is not, create a minimal service provider
    // that can resolve ILoggerFactory and ILogger<T> for delegate parameter injection
    if (loggerFactory is not null && loggerFactory != Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)
    {
      ServiceProvider = new LoggerServiceProvider(loggerFactory);
    }
  }

  /// <summary>
  /// DI constructor - with service provider for Mediator support.
  /// </summary>
  public NuruApp(IServiceProvider serviceProvider)
  {
    ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    Endpoints = serviceProvider.GetRequiredService<EndpointCollection>();
    TypeConverterRegistry = serviceProvider.GetRequiredService<ITypeConverterRegistry>();
    MediatorExecutor = serviceProvider.GetRequiredService<MediatorExecutor>();
    LoggerFactory = serviceProvider.GetService<ILoggerFactory>() ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
    ReplOptions = serviceProvider.GetService<ReplOptions>();
    AppMetadata = serviceProvider.GetService<ApplicationMetadata>();
    Console = serviceProvider.GetService<IConsole>() ?? NuruConsole.Default;
  }

  public async Task<int> RunAsync(string[] args)
  {
    ArgumentNullException.ThrowIfNull(args);

    try
    {
      // Filter out configuration override args before route matching
      // Configuration overrides follow the pattern --Section:Key=value (must start with -- and contain :)
      // This allows legitimate values with colons (e.g., connection strings like //host:port/db)
      string[] routeArgs = [.. args.Where(arg => !(arg.StartsWith("--", StringComparison.Ordinal) && arg.Contains(':', StringComparison.Ordinal)))];

      // Parse and match route (using filtered args)
      ILogger logger = LoggerFactory.CreateLogger("RouteBasedCommandResolver");
      EndpointResolutionResult result = EndpointResolver.Resolve(routeArgs, Endpoints, TypeConverterRegistry, logger);

      // Exit early if route resolution failed
      if (!result.Success || result.MatchedEndpoint is null)
      {
        await Console.WriteErrorLineAsync(
          result.ErrorMessage ?? "No matching command found."
        ).ConfigureAwait(false);

        ShowAvailableCommands();
        return 1;
      }

      if (!await ValidateConfigurationAsync(args).ConfigureAwait(false)) return 1;

      // Execute based on endpoint strategy
      return result.MatchedEndpoint.Strategy switch
      {
        ExecutionStrategy.Mediator when ServiceProvider is null =>
          throw new InvalidOperationException
          (
            $"Command '{result.MatchedEndpoint.RoutePattern}' requires dependency injection. " +
            "Call AddDependencyInjection() before Build()."
          ),

        ExecutionStrategy.Mediator =>
          await ExecuteMediatorCommandAsync
          (
            result.MatchedEndpoint.CommandType!,
            result
          ).ConfigureAwait(false),

        ExecutionStrategy.Delegate =>
          await ExecuteDelegateAsync
          (
            result.MatchedEndpoint.Handler!,
            result.ExtractedValues!,
            result.MatchedEndpoint
          ).ConfigureAwait(false),

        ExecutionStrategy.Invalid =>
          throw new InvalidOperationException
          (
            $"Endpoint '{result.MatchedEndpoint.RoutePattern}' has invalid configuration. " +
            "This is a framework bug."
          ),

        _ => throw new InvalidOperationException("Unknown execution strategy")
      };
    }
#pragma warning disable CA1031 // Do not catch general exception types
    // This is the top-level exception handler for the CLI app. We need to catch all exceptions
    // to provide meaningful error messages to users rather than crashing.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      await Console.WriteErrorLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
      return 1;
    }
  }

  [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
      Justification = "Command type reflection is necessary for mediator pattern - users must preserve command types")]
  [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
      Justification = "Command instantiation through mediator pattern requires reflection")]
  private async Task<int> ExecuteMediatorCommandAsync(
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
      Type commandType,
      EndpointResolutionResult result)
  {
    if (MediatorExecutor is null)
    {
      throw new InvalidOperationException("MediatorExecutor is not available. Ensure DI is configured.");
    }

    object? returnValue = await MediatorExecutor.ExecuteCommandAsync(
      commandType,
      result.ExtractedValues!,
      CancellationToken.None
    ).ConfigureAwait(false);

    // Display the response (if any)
    MediatorExecutor.DisplayResponse(returnValue);

    // Handle int return values
    if (returnValue is int exitCode)
    {
      return exitCode;
    }

    return 0;
  }

  [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
      Justification = "Delegate execution requires reflection - delegate types are preserved through registration")]
  [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
      Justification = "Delegate invocation may require dynamic code generation")]
  private Task<int> ExecuteDelegateAsync(Delegate del, Dictionary<string, string> extractedValues, Endpoint endpoint)
    => DelegateExecutor.ExecuteAsync(
      del,
      extractedValues,
      TypeConverterRegistry,
      ServiceProvider ?? EmptyServiceProvider.Instance,
      endpoint,
      Console
    );

  private void ShowAvailableCommands()
  {
    Console.WriteLine(HelpProvider.GetHelpText(Endpoints, AppMetadata?.Name, AppMetadata?.Description));
  }

  /// <summary>
  /// Validates configuration if validation is enabled and not skipped.
  /// </summary>
  /// <param name="args">Command line arguments.</param>
  /// <returns>True if validation passed or was skipped, false if validation failed.</returns>
  private async Task<bool> ValidateConfigurationAsync(string[] args)
  {
    // Skip validation for help commands or if no ServiceProvider
    if (ShouldSkipValidation(args) || ServiceProvider is null)
      return true;

    try
    {
      IStartupValidator? validator = ServiceProvider.GetService<IStartupValidator>();
      validator?.Validate();
      return true;
    }
    catch (OptionsValidationException ex)
    {
      await DisplayValidationErrorsAsync(ex).ConfigureAwait(false);
      return false;
    }
  }

  /// <summary>
  /// Determines whether configuration validation should be skipped for the current command.
  /// Validation is skipped for help commands.
  /// </summary>
  private static bool ShouldSkipValidation(string[] args)
  {
    // Skip validation if help flag is present
    return args.Any(arg => arg == "--help" || arg == "-h");
  }

  /// <summary>
  /// Displays configuration validation errors in a clean, user-friendly format.
  /// </summary>
  private async Task DisplayValidationErrorsAsync(OptionsValidationException exception)
  {
    await Console.WriteErrorLineAsync("❌ Configuration validation failed:").ConfigureAwait(false);
    await Console.WriteErrorLineAsync("").ConfigureAwait(false);

    foreach (string failure in exception.Failures)
    {
      await Console.WriteErrorLineAsync($"  • {failure}").ConfigureAwait(false);
    }

    await Console.WriteErrorLineAsync("").ConfigureAwait(false);
  }

  /// <summary>
  /// Gets the default application name using the centralized app name detector.
  /// </summary>
  private static string GetDefaultAppName()
  {
    try
    {
      return AppNameDetector.GetEffectiveAppName();
    }
    catch (InvalidOperationException)
    {
      return "nuru-app";
    }
  }

  /// <summary>
  /// Gets the application name from metadata or falls back to default detection.
  /// </summary>
  private string GetEffectiveAppName()
  {
    return AppMetadata?.Name ?? GetDefaultAppName();
  }

  /// <summary>
  /// Gets the application description from metadata.
  /// </summary>
  private string? GetEffectiveDescription()
  {
    return AppMetadata?.Description;
  }

  [GeneratedRegex(@"^--[\w-]+:[\w:-]+", RegexOptions.Compiled)]
  private static partial Regex ConfigurationOverrideRegex();
}