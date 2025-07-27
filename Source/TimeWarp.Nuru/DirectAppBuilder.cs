namespace TimeWarp.Nuru;

/// <summary>
/// Lightweight builder for CLI applications without dependency injection.
/// Perfect for simple CLI tools that don't need DI or Mediator support.
/// </summary>
public class DirectAppBuilder
{
  private readonly EndpointCollection EndpointCollection = [];
  private readonly TypeConverterRegistry TypeConverterRegistry = new();

  /// <summary>
  /// Adds a delegate-based route without DI support.
  /// </summary>
  public DirectAppBuilder AddRoute
  (
    string pattern,
    Delegate handler,
    string? description = null
  )
  {
    ArgumentNullException.ThrowIfNull(pattern);
    ArgumentNullException.ThrowIfNull(handler);

    RouteEndpoint endpoint = new()
    {
      RoutePattern = pattern,
      ParsedRoute = RoutePatternParser.Parse(pattern),
      Handler = handler,
      Method = handler.Method,
      Description = description
    };

    EndpointCollection.Add(endpoint);
    return this;
  }

  /// <summary>
  /// Builds and returns a runnable DirectApp without DI.
  /// </summary>
  public DirectApp Build()
  {
    EndpointCollection.Sort();
    return new DirectApp
    (
      EndpointCollection,
      TypeConverterRegistry
    );
  }
}

/// <summary>
/// A lightweight CLI app without dependency injection.
/// </summary>
public class DirectApp
{
  private readonly EndpointCollection Endpoints;
  private readonly ITypeConverterRegistry TypeConverterRegistry;
  private readonly RouteBasedCommandResolver Resolver;

  public DirectApp
  (
    EndpointCollection endpoints,
    ITypeConverterRegistry typeConverterRegistry
  )
  {
    Endpoints = endpoints;
    TypeConverterRegistry = typeConverterRegistry;
    Resolver = new RouteBasedCommandResolver(endpoints, typeConverterRegistry);
  }

  public async Task<int> RunAsync(string[] args)
  {
    ArgumentNullException.ThrowIfNull(args);

    try
    {
      // Parse and match route
      ResolverResult result = Resolver.Resolve(args);

      if (!result.Success || result.MatchedEndpoint is null)
      {
        await Console.Error.WriteLineAsync
        (
          result.ErrorMessage ?? "No matching command found."
        ).ConfigureAwait(false);

        ShowAvailableCommands();
        return 1;
      }

      // Direct delegates only - no Mediator support
      if (result.MatchedEndpoint.Handler is Delegate del)
      {
        return await ExecuteDelegateAsync(del, result.ExtractedValues).ConfigureAwait(false);
      }

      await Console.Error.WriteLineAsync
      (
        "DirectApp only supports delegate handlers. Use AppBuilder for Mediator support."
      ).ConfigureAwait(false);

      return 1;
    }
    catch (Exception ex)
    {
      await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
      return 1;
    }
  }

  private async Task<int> ExecuteDelegateAsync
  (
    Delegate del,
    Dictionary<string, string> extractedValues
  )
  {
    try
    {
      // Direct invocation - no DI parameter injection
      object?[] args = BindParameters(del.Method, extractedValues);
      object? returnValue = del.DynamicInvoke(args);

      // Handle async delegates
      if (returnValue is Task task)
      {
        await task.ConfigureAwait(false);
      }

      return 0;
    }
    catch (Exception ex)
    {
      await Console.Error.WriteLineAsync
      (
        $"Error executing handler: {ex.Message}"
      ).ConfigureAwait(false);

      return 1;
    }
  }

  private object?[] BindParameters
  (
    MethodInfo method,
    Dictionary<string, string> extractedValues
  )
  {
    ParameterInfo[] parameters = method.GetParameters();
    object?[] args = new object?[parameters.Length];

    for (int i = 0; i < parameters.Length; i++)
    {
      ParameterInfo param = parameters[i];

      if (extractedValues.TryGetValue(param.Name!, out string? stringValue))
      {
        // Simple type conversion only - no DI
        if (TypeConverterRegistry.TryConvert
        (
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
          throw new InvalidOperationException
          (
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
        throw new InvalidOperationException
        (
          $"No value provided for required parameter '{param.Name}'"
        );
      }
    }

    return args;
  }

  private void ShowAvailableCommands()
  {
    Console.WriteLine("\nAvailable commands:");
    foreach (RouteEndpoint endpoint in Endpoints)
    {
      Console.WriteLine($"  {endpoint.RoutePattern}  {endpoint.Description ?? ""}");
    }
  }
}