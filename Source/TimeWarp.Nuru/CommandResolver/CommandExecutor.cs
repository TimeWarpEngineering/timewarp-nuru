
namespace TimeWarp.Nuru.CommandResolver;

/// <summary>
/// Executes command objects through Mediator after populating them from route parameters.
/// </summary>
public class CommandExecutor
{

  private readonly IServiceProvider ServiceProvider;
  private readonly ITypeConverterRegistry TypeConverterRegistry;

  public CommandExecutor(IServiceProvider serviceProvider, ITypeConverterRegistry typeConverterRegistry)
  {
    ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    TypeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
  }

  /// <summary>
  /// Creates a command instance, populates it with extracted values, and executes it through Mediator.
  /// </summary>
  public Task<object?> ExecuteCommandAsync(Type commandType, Dictionary<string, string> extractedValues, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(commandType);
    ArgumentNullException.ThrowIfNull(extractedValues);

    // Create instance of the command
    object command = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException($"Failed to create instance of {commandType.Name}");

    // Populate command properties from extracted values
    PopulateCommand(command, commandType, extractedValues);

    // Execute through Mediator (get from service provider to respect scoped lifetime)
    IMediator mediator = ServiceProvider.GetRequiredService<IMediator>();
    return mediator.Send(command, cancellationToken);
  }

  private void PopulateCommand(object command, Type commandType, Dictionary<string, string> extractedValues)
  {
    foreach ((string paramName, string value) in extractedValues)
    {
      // Find property (case-insensitive)
      PropertyInfo? property = commandType.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, paramName, StringComparison.OrdinalIgnoreCase));

      if (property?.CanWrite != true)
        continue;

      try
      {
        // Handle string arrays (for catch-all parameters)
        if (property.PropertyType == typeof(string[]))
        {
          // Split the value by spaces to create an array
          string[] arrayValue = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
          property.SetValue(command, arrayValue);
        }
        // Convert the string value to the property type
        else if (TypeConverterRegistry.TryConvert(value, property.PropertyType, out object? convertedValue))
        {
          property.SetValue(command, convertedValue);
        }
        else if (property.PropertyType == typeof(string))
        {
          property.SetValue(command, value);
        }
        else
        {
          // Try basic conversion as fallback
          try
          {
            object converted = Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture);
            property.SetValue(command, converted);
          }
          catch
          {
            throw new InvalidOperationException(
                $"Cannot convert '{value}' to type {property.PropertyType} for parameter '{property.Name}'");
          }
        }
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(
            $"Failed to set property '{property.Name}' to value '{value}': {ex.Message}", ex);
      }
    }
  }

  /// <summary>
  /// Formats the command response for console output.
  /// </summary>
  public static void DisplayResponse(object? response)
  {
    if (response is null)
      return;

    Type responseType = response.GetType();

    // Check if this is Unit.Value (represents no return value)
    if (responseType.Name == "Unit" && responseType.Namespace == "TimeWarp.Mediator")
      return;

    // Check if the response has a custom ToString() implementation
    MethodInfo? toStringMethod = responseType.GetMethod("ToString", Type.EmptyTypes);
    if (toStringMethod is not null && toStringMethod.DeclaringType != typeof(object))
    {
      // Use custom ToString
      NuruConsole.WriteLine(response.ToString());
    }
    else if (responseType.IsPrimitive || responseType == typeof(string) || responseType == typeof(decimal))
    {
      // Simple types - just display directly
      NuruConsole.WriteLine(response.ToString());
    }
    else
    {
      // Complex object - serialize to JSON for display
      string json = JsonSerializer.Serialize(response, NuruJsonSerializerContext.Default.Options);
      NuruConsole.WriteLine(json);
    }
  }
}
