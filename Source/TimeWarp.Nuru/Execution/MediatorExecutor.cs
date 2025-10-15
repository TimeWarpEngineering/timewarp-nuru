
namespace TimeWarp.Nuru;

/// <summary>
/// Executes command objects through Mediator after populating them from route parameters.
/// </summary>
public class MediatorExecutor
{

  private readonly IServiceProvider ServiceProvider;
  private readonly ITypeConverterRegistry TypeConverterRegistry;

  public MediatorExecutor(IServiceProvider serviceProvider, ITypeConverterRegistry typeConverterRegistry)
  {
    ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    TypeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
  }

  /// <summary>
  /// Creates a command instance, populates it with extracted values, and executes it through Mediator.
  /// </summary>
  /// <remarks>
  /// This method uses reflection to create command instances and populate properties.
  /// When using NativeAOT, ensure command types are preserved with [DynamicDependency] or similar attributes.
  /// </remarks>
  [RequiresUnreferencedCode("Command types are created and populated dynamically. Ensure command constructors and properties are preserved.")]
  [RequiresDynamicCode("Command instantiation may require dynamic code generation.")]
  public Task<object?> ExecuteCommandAsync(
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
      Type commandType,
      Dictionary<string, string> extractedValues,
      CancellationToken cancellationToken)
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

  [UnconditionalSuppressMessage("Trimming", "IL2072:UnrecognizedReflectionPattern",
      Justification = "Command properties are preserved through DynamicallyAccessedMembers annotation on commandType parameter")]
  private void PopulateCommand(
      object command,
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
      Type commandType,
      Dictionary<string, string> extractedValues)
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
  [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
      Justification = "ToString method lookup is safe - all types have ToString")]
  [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
      Justification = "JSON serialization uses source-generated context for known types")]
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
