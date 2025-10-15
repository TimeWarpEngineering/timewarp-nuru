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

      if (!result.Success || result.MatchedEndpoint is null)
      {
        await NuruConsole.WriteErrorLineAsync(
          result.ErrorMessage ?? "No matching command found."
        ).ConfigureAwait(false);

        ShowAvailableCommands();
        return 1;
      }

      // Check if this is a Mediator command (CommandType is set)
      Type? commandType = result.MatchedEndpoint.CommandType;

      if (commandType is not null && ServiceProvider is not null)
      {
        // Execute through Mediator
        return await ExecuteMediatorCommandAsync(commandType, result).ConfigureAwait(false);
      }

      // Execute as delegate
      if (result.MatchedEndpoint.Handler is Delegate del)
      {
        if (result.ExtractedValues is null)
        {
          throw new InvalidOperationException("ExtractedValues cannot be null for a successful match.");
        }

        return await ExecuteDelegateAsync(del, result.ExtractedValues, result.MatchedEndpoint).ConfigureAwait(false);
      }

      await NuruConsole.WriteErrorLineAsync(
        "No valid handler found for the matched route."
      ).ConfigureAwait(false);

      return 1;
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

    if (result.ExtractedValues is null)
    {
      throw new InvalidOperationException("ExtractedValues cannot be null for a successful match.");
    }

    object? returnValue = await MediatorExecutor.ExecuteCommandAsync(
      commandType,
      result.ExtractedValues,
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

  private static bool IsOptionalParameter(string parameterName, Endpoint endpoint)
  {
    // Check positional parameters
    foreach (RouteMatcher segment in endpoint.CompiledRoute.PositionalMatchers)
    {
      if (segment is ParameterMatcher param && param.Name == parameterName)
      {
        return param.IsOptional;
      }
    }

    // Check option parameters
    foreach (OptionMatcher option in endpoint.CompiledRoute.OptionMatchers)
    {
      if (option.ParameterName == parameterName)
      {
        // Option parameters are optional if the parameter is marked as optional
        // We need to check the route pattern for this
        return endpoint.RoutePattern.Contains($"{{{parameterName}?", StringComparison.Ordinal) ||
               (endpoint.RoutePattern.Contains($"{{{parameterName}:", StringComparison.Ordinal) &&
                endpoint.RoutePattern.Contains("?}", StringComparison.Ordinal));
      }
    }

    return false;
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
