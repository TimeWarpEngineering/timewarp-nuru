namespace TimeWarp.Nuru;

/// <summary>
/// Core CLI app that supports both direct execution and dependency injection.
/// For the full-featured experience, use <see cref="NuruCoreApp"/> from the TimeWarp.Nuru package.
/// </summary>
public partial class NuruCoreApp
{
  private readonly IServiceProvider? ServiceProvider;
  private readonly MediatorExecutor? MediatorExecutor;

  #region Static Factory Methods

  /// <summary>
  /// Creates a lightweight builder with Configuration, auto-help, and logging infrastructure.
  /// No DI container, Mediator, REPL, or Completion.
  /// </summary>
  /// <param name="args">Optional command line arguments.</param>
  /// <param name="options">Optional application options.</param>
  /// <returns>A lightweight <see cref="NuruCoreAppBuilder"/>.</returns>
  /// <remarks>
  /// Features included:
  /// - Type converters
  /// - Auto-help generation
  /// - Logging infrastructure
  ///
  /// Features NOT included:
  /// - DI Container
  /// - Mediator pattern
  /// - REPL
  /// - Completion
  /// - OpenTelemetry
  ///
  /// This builder is fully AOT-compatible.
  /// </remarks>
  /// <example>
  /// <code>
  /// NuruCoreAppBuilder builder = NuruCoreApp.CreateSlimBuilder();
  /// builder.Map("greet {name}", (string name) => $"Hello, {name}!");
  /// await builder.Build().RunAsync(args);
  /// </code>
  /// </example>
  public static NuruCoreAppBuilder CreateSlimBuilder(string[]? args = null, NuruCoreApplicationOptions? options = null)
  {
    options ??= new NuruCoreApplicationOptions();
    options.Args = args;
    return new NuruCoreAppBuilder(BuilderMode.Slim, options);
  }

  /// <summary>
  /// Creates a bare minimum builder with only type converters.
  /// User has total control over what features to add.
  /// </summary>
  /// <param name="args">Optional command line arguments.</param>
  /// <param name="options">Optional application options.</param>
  /// <returns>An empty <see cref="NuruCoreAppBuilder"/>.</returns>
  /// <remarks>
  /// Features included:
  /// - Type converters
  /// - Args storage
  ///
  /// Features NOT included (add manually if needed):
  /// - Configuration
  /// - Auto-help
  /// - Logging infrastructure
  /// - DI Container
  /// - Mediator pattern
  /// - REPL
  /// - Completion
  ///
  /// This builder is fully AOT-compatible.
  /// </remarks>
  /// <example>
  /// <code>
  /// NuruCoreAppBuilder builder = NuruCoreApp.CreateEmptyBuilder();
  /// builder.AddTypeConverter(new MyCustomConverter());
  /// builder.Map("cmd", () => "minimal");
  /// await builder.Build().RunAsync(args);
  /// </code>
  /// </example>
  public static NuruCoreAppBuilder CreateEmptyBuilder(string[]? args = null, NuruCoreApplicationOptions? options = null)
  {
    options ??= new NuruCoreApplicationOptions();
    options.Args = args;
    return new NuruCoreAppBuilder(BuilderMode.Empty, options);
  }

  #endregion

  /// <summary>
  /// Gets the terminal I/O provider for interactive operations like REPL.
  /// </summary>
  public ITerminal Terminal { get; }

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
  /// Gets the help configuration options.
  /// </summary>
  public HelpOptions HelpOptions { get; }

  /// <summary>
  /// Gets the session context for tracking REPL vs CLI execution state.
  /// </summary>
  public SessionContext SessionContext { get; }

  /// <summary>
  /// Direct constructor - used by NuruAppBuilder for non-DI path.
  /// </summary>
  internal NuruCoreApp
  (
    EndpointCollection endpoints,
    ITypeConverterRegistry typeConverterRegistry,
    ILoggerFactory loggerFactory,
    ITerminal terminal,
    ReplOptions? replOptions = null,
    ApplicationMetadata? appMetadata = null,
    HelpOptions? helpOptions = null,
    SessionContext? sessionContext = null
  )
  {
    Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    TypeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
    LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    ReplOptions = replOptions;
    AppMetadata = appMetadata;
    HelpOptions = helpOptions ?? new HelpOptions();
    SessionContext = sessionContext ?? new SessionContext();

    // Create a minimal service provider for delegate parameter injection
    // Resolves NuruCoreApp (for interactive mode), ILoggerFactory, and ILogger<T>
    ServiceProvider = new LightweightServiceProvider(this, loggerFactory);
  }

  /// <summary>
  /// DI constructor - with service provider for Mediator support.
  /// </summary>
  public NuruCoreApp(IServiceProvider serviceProvider)
  {
    ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    Endpoints = serviceProvider.GetRequiredService<EndpointCollection>();
    TypeConverterRegistry = serviceProvider.GetRequiredService<ITypeConverterRegistry>();
    MediatorExecutor = serviceProvider.GetService<MediatorExecutor>();
    LoggerFactory = serviceProvider.GetService<ILoggerFactory>() ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
    ReplOptions = serviceProvider.GetService<ReplOptions>();
    AppMetadata = serviceProvider.GetService<ApplicationMetadata>();
    HelpOptions = serviceProvider.GetService<HelpOptions>() ?? new HelpOptions();
    Terminal = serviceProvider.GetService<ITerminal>() ?? NuruTerminal.Default;
    SessionContext = serviceProvider.GetRequiredService<SessionContext>();
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
        await Terminal.WriteErrorLineAsync(
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
      await Terminal.WriteErrorLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
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
  {
    // When full DI is enabled (MediatorExecutor exists), route through Mediator
    // Pipeline behaviors will apply if registered, otherwise handler runs directly
    if (MediatorExecutor is not null)
    {
      return ExecuteDelegateWithPipelineAsync(del, extractedValues, endpoint);
    }

    // Direct execution path (no DI)
    return DelegateExecutor.ExecuteAsync(
      del,
      extractedValues,
      TypeConverterRegistry,
      ServiceProvider ?? EmptyServiceProvider.Instance,
      endpoint,
      Terminal
    );
  }

  /// <summary>
  /// Executes a delegate through Mediator when DI is enabled, allowing pipeline behaviors to apply.
  /// </summary>
  [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
      Justification = "Delegate execution requires reflection - delegate types are preserved through registration")]
  [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
      Justification = "Delegate invocation may require dynamic code generation")]
  private async Task<int> ExecuteDelegateWithPipelineAsync(
    Delegate del,
    Dictionary<string, string> extractedValues,
    Endpoint endpoint)
  {
    // Create a scope for this request to get fresh RouteExecutionContext
    using IServiceScope scope = ServiceProvider!.CreateScope();

    // Populate RouteExecutionContext for pipeline behaviors
    RouteExecutionContext? executionContext = scope.ServiceProvider.GetService<RouteExecutionContext>();
    if (executionContext is not null)
    {
      executionContext.RoutePattern = endpoint.RoutePattern;
      executionContext.StartedAt = DateTimeOffset.UtcNow;
      executionContext.Strategy = ExecutionStrategy.Delegate;
      executionContext.IsWrappedDelegate = true;
    }

    // Bind parameters first (same as direct execution)
    object?[] boundArgs = BindDelegateParameters(del, extractedValues, endpoint);

    // Create the delegate request
    DelegateRequest request = new()
    {
      RoutePattern = endpoint.RoutePattern,
      BoundArguments = boundArgs,
      Handler = del,
      Endpoint = endpoint
    };

    // Populate parameters in execution context
    if (executionContext is not null)
    {
      executionContext.Parameters = extractedValues;
    }

    try
    {
      // Execute through Mediator - pipeline behaviors will be invoked automatically
      IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
      DelegateResponse response = await mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

      // Display the response (if any)
      ResponseDisplay.Write(response.Result, Terminal);

      return response.ExitCode;
    }
#pragma warning disable CA1031 // Do not catch general exception types
    // We catch all exceptions here to provide consistent error handling for delegate execution.
    // The CLI should not crash due to handler exceptions.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      await Terminal.WriteErrorLineAsync($"Error executing handler: {ex.Message}").ConfigureAwait(false);
      return 1;
    }
  }

  /// <summary>
  /// Binds delegate parameters from extracted route values.
  /// </summary>
  [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
      Justification = "Delegate parameter binding uses reflection - types preserved through registration")]
  [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
      Justification = "Parameter binding may require dynamic code generation")]
  private object?[] BindDelegateParameters(
    Delegate del,
    Dictionary<string, string> extractedValues,
    Endpoint endpoint)
  {
    MethodInfo method = del.Method;
    ParameterInfo[] parameters = method.GetParameters();

    if (parameters.Length == 0)
      return [];

    return BindParameters(parameters, extractedValues, endpoint);
  }

  /// <summary>
  /// Binds parameters from extracted values and services.
  /// </summary>
  [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
      Justification = "Parameter types are preserved through delegate registration")]
  [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
      Justification = "Array type creation is safe for known parameter types")]
  private object?[] BindParameters(
    ParameterInfo[] parameters,
    Dictionary<string, string> extractedValues,
    Endpoint endpoint)
  {
    object?[] args = new object?[parameters.Length];

    for (int i = 0; i < parameters.Length; i++)
    {
      ParameterInfo param = parameters[i];

      // Try to get value from extracted values
      if (extractedValues.TryGetValue(param.Name!, out string? stringValue))
      {
        args[i] = ConvertParameter(param, stringValue);
      }
      else
      {
        // No value found - check if it's a service from DI
        if (IsServiceParameter(param))
        {
          args[i] = ServiceProvider?.GetService(param.ParameterType);
          if (args[i] is null && !param.HasDefaultValue)
          {
            throw new InvalidOperationException(
                $"Cannot resolve service of type {param.ParameterType} for parameter '{param.Name}'");
          }
        }
        else if (param.HasDefaultValue)
        {
          args[i] = param.DefaultValue;
        }
        else if (IsOptionalParameter(param.Name!, endpoint))
        {
          args[i] = null;
        }
        else
        {
          throw new InvalidOperationException(
              $"No value provided for required parameter '{param.Name}'");
        }
      }
    }

    return args;
  }

  /// <summary>
  /// Converts a string value to the parameter type.
  /// </summary>
  [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
      Justification = "Parameter types are preserved through delegate registration")]
  [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
      Justification = "Array type creation is safe for known parameter types")]
  private object? ConvertParameter(ParameterInfo param, string stringValue)
  {
    // Handle arrays (catch-all and repeated parameters)
    if (param.ParameterType.IsArray)
    {
      Type elementType = param.ParameterType.GetElementType()!;
      string[] parts = stringValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      if (elementType == typeof(string))
      {
        return parts;
      }

      Array typedArray = Array.CreateInstance(elementType, parts.Length);
      for (int j = 0; j < parts.Length; j++)
      {
        if (TypeConverterRegistry.TryConvert(parts[j], elementType, out object? convertedElement))
        {
          typedArray.SetValue(convertedElement, j);
        }
        else
        {
          object converted = Convert.ChangeType(parts[j], elementType, CultureInfo.InvariantCulture);
          typedArray.SetValue(converted, j);
        }
      }

      return typedArray;
    }

    // Convert single value
    if (TypeConverterRegistry.TryConvert(stringValue, param.ParameterType, out object? convertedValue))
    {
      return convertedValue;
    }

    if (param.ParameterType == typeof(string))
    {
      return stringValue;
    }

    return Convert.ChangeType(stringValue, param.ParameterType, CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Checks if a parameter is optional based on the endpoint configuration.
  /// </summary>
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
        return option.IsOptional || option.ParameterIsOptional;
      }
    }

    return false;
  }

  /// <summary>
  /// Determines if a parameter should be resolved from DI.
  /// </summary>
  [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
      Justification = "Type checking for service parameters uses safe type comparisons")]
  private static bool IsServiceParameter(ParameterInfo parameter)
  {
    Type type = parameter.ParameterType;

    // Simple heuristic: if it's not a common value type and not string/array,
    // it's likely a service
    if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
        type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid) ||
        type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
    {
      return false;
    }

    return true;
  }

  private void ShowAvailableCommands()
  {
    Terminal.WriteLine(HelpProvider.GetHelpText(Endpoints, AppMetadata?.Name, AppMetadata?.Description, HelpOptions, HelpContext.Cli));
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
    await Terminal.WriteErrorLineAsync("❌ Configuration validation failed:").ConfigureAwait(false);
    await Terminal.WriteErrorLineAsync("").ConfigureAwait(false);

    foreach (string failure in exception.Failures)
    {
      await Terminal.WriteErrorLineAsync($"  • {failure}").ConfigureAwait(false);
    }

    await Terminal.WriteErrorLineAsync("").ConfigureAwait(false);
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
