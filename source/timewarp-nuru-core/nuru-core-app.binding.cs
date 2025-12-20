namespace TimeWarp.Nuru;

/// <summary>
/// Parameter binding and conversion methods for NuruCoreApp.
/// </summary>
public partial class NuruCoreApp
{
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

      // Special case: ITerminal - use this.Terminal (which respects TestTerminalContext)
      if (param.ParameterType == typeof(ITerminal))
      {
        args[i] = Terminal;
        continue;
      }

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
}
