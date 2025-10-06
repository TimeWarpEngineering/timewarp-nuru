
namespace TimeWarp.Nuru;

/// <summary>
/// Executes delegate-based endpoints with parameter binding and response display.
/// </summary>
public static class DelegateExecutor
{
  /// <summary>
  /// Executes a delegate with parameters bound from extracted route values.
  /// </summary>
  public static async Task<int> ExecuteAsync(
      Delegate handler,
      Dictionary<string, string> extractedValues,
      ITypeConverterRegistry typeConverterRegistry,
      IServiceProvider serviceProvider,
      Endpoint endpoint)
  {
    ArgumentNullException.ThrowIfNull(handler);
    ArgumentNullException.ThrowIfNull(extractedValues);
    ArgumentNullException.ThrowIfNull(typeConverterRegistry);
    ArgumentNullException.ThrowIfNull(serviceProvider);
    ArgumentNullException.ThrowIfNull(endpoint);

    try
    {
      MethodInfo method = handler.Method;
      ParameterInfo[] parameters = method.GetParameters();

      object?[] args = parameters.Length == 0
        ? []
        : BindParameters(parameters, extractedValues, typeConverterRegistry, serviceProvider, endpoint);

      object? returnValue = handler.DynamicInvoke(args);

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
      MediatorExecutor.DisplayResponse(returnValue);

      return 0;
    }
#pragma warning disable CA1031 // Do not catch general exception types
    // We catch all exceptions here to provide consistent error handling for delegate execution.
    // The CLI should not crash due to handler exceptions.
    catch (Exception ex)
#pragma warning restore CA1031
    {
      await NuruConsole.WriteErrorLineAsync(
        $"Error executing handler: {ex.Message}"
      ).ConfigureAwait(false);

      return 1;
    }
  }

  private static object?[] BindParameters(
      ParameterInfo[] parameters,
      Dictionary<string, string> extractedValues,
      ITypeConverterRegistry typeConverterRegistry,
      IServiceProvider serviceProvider,
      Endpoint endpoint)
  {
    object?[] args = new object?[parameters.Length];

    for (int i = 0; i < parameters.Length; i++)
    {
      ParameterInfo param = parameters[i];

      // Try to get value from extracted values
      if (extractedValues.TryGetValue(param.Name!, out string? stringValue))
      {
        // Handle arrays (catch-all parameters)
        if (param.ParameterType.IsArray)
        {
          Type elementType = param.ParameterType.GetElementType()!;
          if (elementType == typeof(string))
          {
            // Split the value for string arrays
            args[i] = stringValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
          }
          else
          {
            // For other array types, we'd need more complex handling
            throw new NotSupportedException($"Array type {param.ParameterType} is not supported yet");
          }
        }
        else
        {
          // Convert the value to the parameter type
          if (typeConverterRegistry.TryConvert(stringValue, param.ParameterType, out object? convertedValue))
          {
            args[i] = convertedValue;
          }
          else if (param.ParameterType == typeof(string))
          {
            args[i] = stringValue;
          }
          else
          {
            // Try basic conversion as fallback
            try
            {
              args[i] = Convert.ChangeType(stringValue, param.ParameterType, CultureInfo.InvariantCulture);
            }
            catch
            {
              throw new InvalidOperationException(
                  $"Cannot convert '{stringValue}' to type {param.ParameterType} for parameter '{param.Name}'");
            }
          }
        }
      }
      else
      {
        // No value found - check if it's a service from DI
        if (IsServiceParameter(param))
        {
          args[i] = serviceProvider.GetService(param.ParameterType);
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
          // Optional parameter without a value - set to null
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
        // Option parameter is optional if either:
        // 1. The option flag itself is optional (--config? means entire option can be omitted)
        // 2. The parameter value is optional (--config {mode?} means value can be omitted)
        return option.IsOptional || option.ParameterIsOptional;
      }
    }

    return false;
  }

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
}
