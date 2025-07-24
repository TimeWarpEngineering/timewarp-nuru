
namespace TimeWarp.Nuru.ParameterBinding;

/// <summary>
/// Binds extracted route values to delegate parameters.
/// </summary>
public static class DelegateParameterBinder
{
  /// <summary>
  /// Invokes a delegate with parameters bound from extracted route values.
  /// </summary>
  public static object? InvokeWithParameters(
      Delegate handler,
      Dictionary<string, string> extractedValues,
      ITypeConverterRegistry typeConverterRegistry,
      IServiceProvider serviceProvider)
  {
    ArgumentNullException.ThrowIfNull(handler);
    ArgumentNullException.ThrowIfNull(extractedValues);
    ArgumentNullException.ThrowIfNull(typeConverterRegistry);
    ArgumentNullException.ThrowIfNull(serviceProvider);

    MethodInfo method = handler.Method;
    ParameterInfo[] parameters = method.GetParameters();

    if (parameters.Length == 0)
    {
      return handler.DynamicInvoke();
    }

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
        else
        {
          throw new InvalidOperationException(
              $"No value provided for required parameter '{param.Name}'");
        }
      }
    }

    return handler.DynamicInvoke(args);
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
