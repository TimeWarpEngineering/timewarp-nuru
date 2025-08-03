namespace TimeWarp.Nuru;

/// <summary>
/// A unified CLI app that supports both direct execution and dependency injection.
/// </summary>
public class NuruApp
{
  private readonly IServiceProvider? ServiceProvider;
  private readonly ITypeConverterRegistry TypeConverterRegistry;
  private readonly CommandExecutor? CommandExecutor;

  /// <summary>
  /// Gets the collection of registered endpoints.
  /// </summary>
  public EndpointCollection Endpoints { get; }

  /// <summary>
  /// Direct constructor - no dependency injection.
  /// </summary>
  public NuruApp(EndpointCollection endpoints, ITypeConverterRegistry typeConverterRegistry)
  {
    Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    TypeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
  }

  /// <summary>
  /// DI constructor - with service provider for Mediator support.
  /// </summary>
  public NuruApp(IServiceProvider serviceProvider)
  {
    ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    Endpoints = serviceProvider.GetRequiredService<EndpointCollection>();
    TypeConverterRegistry = serviceProvider.GetRequiredService<ITypeConverterRegistry>();
    CommandExecutor = serviceProvider.GetRequiredService<CommandExecutor>();
  }

  public async Task<int> RunAsync(string[] args)
  {
    ArgumentNullException.ThrowIfNull(args);

    try
    {
      // Parse and match route
      ResolverResult result = RouteBasedCommandResolver.Resolve(args, Endpoints, TypeConverterRegistry);

      if (!result.Success || result.MatchedEndpoint is null)
      {
        await Console.Error.WriteLineAsync(
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

        return await ExecuteDelegateAsync(del, result.ExtractedValues).ConfigureAwait(false);
      }

      await Console.Error.WriteLineAsync(
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
      await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
      return 1;
    }
  }

  private async Task<int> ExecuteMediatorCommandAsync(Type commandType, ResolverResult result)
  {
    if (CommandExecutor is null)
    {
      throw new InvalidOperationException("CommandExecutor is not available. Ensure DI is configured.");
    }

    if (result.ExtractedValues is null)
    {
      throw new InvalidOperationException("ExtractedValues cannot be null for a successful match.");
    }

    object? returnValue = await CommandExecutor.ExecuteCommandAsync(
      commandType,
      result.ExtractedValues,
      CancellationToken.None
    ).ConfigureAwait(false);

    // Display the response (if any)
    CommandExecutor.DisplayResponse(returnValue);

    // Handle int return values
    if (returnValue is int exitCode)
    {
      return exitCode;
    }

    return 0;
  }

  private async Task<int> ExecuteDelegateAsync(Delegate del, Dictionary<string, string> extractedValues)
  {
    try
    {
      object?[] args = ServiceProvider is not null
        ? BindParametersWithDI(del.Method, extractedValues)
        : BindParameters(del.Method, extractedValues);

      object? returnValue = del.DynamicInvoke(args);

      // Handle async delegates
      if (returnValue is Task task)
      {
        await task.ConfigureAwait(false);

        // For Task<T>, get the result
        Type taskType = task.GetType();
        if (taskType.IsGenericType)
        {
          PropertyInfo? resultProperty = taskType.GetProperty("Result");
          if (resultProperty is not null)
          {
            object? result = resultProperty.GetValue(task);
            // Check if this is VoidTaskResult (used internally for void async methods)
            if (result?.GetType().Name == "VoidTaskResult")
            {
              returnValue = null;
            }
            else
            {
              returnValue = result;
            }
          }
        }
        else
        {
          // For non-generic Task (void async), set to null to avoid displaying VoidTaskResult
          returnValue = null;
        }
      }

      // Display the response (if any)
      CommandExecutor.DisplayResponse(returnValue);

      return 0;
    }
#pragma warning disable CA1031 // Do not catch general exception types
    // We catch all exceptions here to provide consistent error handling for delegate execution.
    // The CLI should not crash due to handler exceptions.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      await Console.Error.WriteLineAsync(
        $"Error executing handler: {ex.Message}"
      ).ConfigureAwait(false);

      return 1;
    }
  }

  private object?[] BindParameters(MethodInfo method, Dictionary<string, string> extractedValues)
  {
    ParameterInfo[] parameters = method.GetParameters();
    object?[] args = new object?[parameters.Length];

    for (int i = 0; i < parameters.Length; i++)
    {
      ParameterInfo param = parameters[i];

      if (extractedValues.TryGetValue(param.Name!, out string? stringValue))
      {
        // Simple type conversion only - no DI
        if (TypeConverterRegistry.TryConvert(
          stringValue,
          param.ParameterType,
          out object? convertedValue
        ))
        {
          args[i] = convertedValue;
        }
        else if (param.ParameterType == typeof(string))
        {
          args[i] = stringValue;
        }
        else if (param.ParameterType == typeof(string[]))
        {
          // Split space-delimited string into array for catch-all parameters
          args[i] = stringValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
          throw new InvalidOperationException(
            $"Cannot convert '{stringValue}' to type {param.ParameterType} for parameter '{param.Name}'"
          );
        }
      }
      else if (param.HasDefaultValue)
      {
        args[i] = param.DefaultValue;
      }
      else
      {
        throw new InvalidOperationException(
          $"No value provided for required parameter '{param.Name}'"
        );
      }
    }

    return args;
  }

  private object?[] BindParametersWithDI(MethodInfo method, Dictionary<string, string> extractedValues)
  {
    // Use DelegateParameterBinder when DI is available
    // This handles DI injection for parameters
    ParameterInfo[] parameters = method.GetParameters();
    object?[] args = new object?[parameters.Length];

    for (int i = 0; i < parameters.Length; i++)
    {
      ParameterInfo param = parameters[i];

      // First try extracted values
      if (extractedValues.TryGetValue(param.Name!, out string? stringValue))
      {
        if (TypeConverterRegistry.TryConvert(
          stringValue,
          param.ParameterType,
          out object? convertedValue
        ))
        {
          args[i] = convertedValue;
        }
        else if (param.ParameterType == typeof(string))
        {
          args[i] = stringValue;
        }
        else if (param.ParameterType == typeof(string[]))
        {
          args[i] = stringValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
      }
      // Then try DI for non-value parameters
      else if (ServiceProvider!.GetService(param.ParameterType) is object service)
      {
        args[i] = service;
      }
      else if (param.HasDefaultValue)
      {
        args[i] = param.DefaultValue;
      }
      else
      {
        throw new InvalidOperationException(
          $"No value provided for required parameter '{param.Name}'"
        );
      }
    }

    return args;
  }

  private void ShowAvailableCommands()
  {
    Console.WriteLine(RouteHelpProvider.GetHelpText(Endpoints));
  }
}
