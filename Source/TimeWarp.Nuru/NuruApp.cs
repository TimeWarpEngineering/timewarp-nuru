namespace TimeWarp.Nuru;

using Microsoft.Extensions.Logging;

/// <summary>
/// A unified CLI app that supports both direct execution and dependency injection.
/// </summary>
public class NuruApp
{
  private readonly IServiceProvider? ServiceProvider;
  private readonly ITypeConverterRegistry TypeConverterRegistry;
  private readonly MediatorExecutor? MediatorExecutor;
  private readonly ILoggerFactory LoggerFactory;

  /// <summary>
  /// Gets the collection of registered endpoints.
  /// </summary>
  public EndpointCollection Endpoints { get; }

  /// <summary>
  /// Direct constructor - no dependency injection.
  /// </summary>
  public NuruApp
  (
    EndpointCollection endpoints,
    ITypeConverterRegistry typeConverterRegistry,
    ILoggerFactory? loggerFactory = null
  )
  {
    Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    TypeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
    LoggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

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
  }

  public async Task<int> RunAsync(string[] args)
  {
    ArgumentNullException.ThrowIfNull(args);

    try
    {
      // Parse and match route
      ILogger logger = LoggerFactory.CreateLogger("RouteBasedCommandResolver");
      EndpointResolutionResult result = EndpointResolver.Resolve(args, Endpoints, TypeConverterRegistry, logger);

      // Validate configuration after route resolution but before execution
      // Skip validation for help commands or if route resolution failed
      if (!ShouldSkipValidation(args, result) && ServiceProvider is not null)
      {
        try
        {
          IStartupValidator? validator = ServiceProvider.GetService<IStartupValidator>();
          validator?.Validate();
        }
        catch (OptionsValidationException ex)
        {
          await DisplayValidationErrorsAsync(ex).ConfigureAwait(false);
          return 1;
        }
      }

      if (!result.Success || result.MatchedEndpoint is null)
      {
        await NuruConsole.WriteErrorLineAsync(
          result.ErrorMessage ?? "No matching command found."
        ).ConfigureAwait(false);

        ShowAvailableCommands();
        return 1;
      }

      // Execute based on endpoint strategy
      return result.MatchedEndpoint.Strategy switch
      {
        ExecutionStrategy.Mediator when ServiceProvider is null =>
          throw new InvalidOperationException(
            $"Command '{result.MatchedEndpoint.RoutePattern}' requires dependency injection. " +
            "Call AddDependencyInjection() before Build()."),

        ExecutionStrategy.Mediator =>
          await ExecuteMediatorCommandAsync(
            result.MatchedEndpoint.CommandType!,
            result).ConfigureAwait(false),

        ExecutionStrategy.Delegate =>
          await ExecuteDelegateAsync(
            result.MatchedEndpoint.Handler!,
            result.ExtractedValues!,
            result.MatchedEndpoint).ConfigureAwait(false),

        ExecutionStrategy.Invalid =>
          throw new InvalidOperationException(
            $"Endpoint '{result.MatchedEndpoint.RoutePattern}' has invalid configuration. " +
            "This is a framework bug."),

        _ => throw new InvalidOperationException("Unknown execution strategy")
      };
    }
#pragma warning disable CA1031 // Do not catch general exception types
    // This is the top-level exception handler for the CLI app. We need to catch all exceptions
    // to provide meaningful error messages to users rather than crashing.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      await NuruConsole.WriteErrorLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
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
      endpoint
    );

  private void ShowAvailableCommands()
  {
    NuruConsole.WriteLine(HelpProvider.GetHelpText(Endpoints));
  }

  /// <summary>
  /// Determines whether configuration validation should be skipped for the current command.
  /// Validation is skipped for help commands and when route resolution fails.
  /// </summary>
  private static bool ShouldSkipValidation(string[] args, EndpointResolutionResult result)
  {
    // Skip validation if help flag is present
    if (args.Any(arg => arg == "--help" || arg == "-h"))
      return true;

    // Skip validation if route resolution failed (shows "command not found" + available routes)
    if (!result.Success)
      return true;

    return false;
  }

  /// <summary>
  /// Displays configuration validation errors in a clean, user-friendly format.
  /// </summary>
  private static async Task DisplayValidationErrorsAsync(OptionsValidationException exception)
  {
    await NuruConsole.WriteErrorLineAsync("❌ Configuration validation failed:").ConfigureAwait(false);
    await NuruConsole.WriteErrorLineAsync("").ConfigureAwait(false);

    foreach (string failure in exception.Failures)
    {
      await NuruConsole.WriteErrorLineAsync($"  • {failure}").ConfigureAwait(false);
    }

    await NuruConsole.WriteErrorLineAsync("").ConfigureAwait(false);
  }
}

/// <summary>
/// Provides an empty service provider for scenarios without dependency injection.
/// </summary>
internal sealed class EmptyServiceProvider : IServiceProvider
{
  public static readonly EmptyServiceProvider Instance = new();

  private EmptyServiceProvider() { }

  public object? GetService(Type serviceType) => null;
}

/// <summary>
/// Provides a minimal service provider that can resolve ILoggerFactory and ILogger&lt;T&gt;.
/// Used when logging is configured but dependency injection is not enabled.
/// </summary>
internal sealed class LoggerServiceProvider : IServiceProvider
{
  private readonly ILoggerFactory LoggerFactory;

  public LoggerServiceProvider(ILoggerFactory loggerFactory)
  {
    LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
  }

  public object? GetService(Type serviceType)
  {
    if (serviceType == typeof(ILoggerFactory))
    {
      return LoggerFactory;
    }

    // Handle ILogger<T> requests by creating Logger<T> instances
    if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(ILogger<>))
    {
      Type categoryType = serviceType.GetGenericArguments()[0];
      Type loggerType = typeof(Logger<>).MakeGenericType(categoryType);
      return Activator.CreateInstance(loggerType, LoggerFactory)!;
    }

    return null;
  }
}
