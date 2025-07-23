using TimeWarp.Mediator;
using System.Text.Json;

namespace TimeWarp.Nuru.CommandResolver;

/// <summary>
/// Executes command objects through Mediator after populating them from route parameters.
/// </summary>
public class CommandExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITypeConverterRegistry _typeConverterRegistry;
    
    public CommandExecutor(IServiceProvider serviceProvider, ITypeConverterRegistry typeConverterRegistry)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _typeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
    }
    
    /// <summary>
    /// Creates a command instance, populates it with extracted values, and executes it through Mediator.
    /// </summary>
    public async Task<object?> ExecuteCommandAsync(Type commandType, Dictionary<string, string> extractedValues, CancellationToken cancellationToken)
    {
        // Create instance of the command
        var command = Activator.CreateInstance(commandType) 
            ?? throw new InvalidOperationException($"Failed to create instance of {commandType.Name}");
        
        // Populate command properties from extracted values
        PopulateCommand(command, commandType, extractedValues);
        
        // Execute through Mediator (get from service provider to respect scoped lifetime)
        var mediator = _serviceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(command, cancellationToken);
        
        return result;
    }
    
    private void PopulateCommand(object command, Type commandType, Dictionary<string, string> extractedValues)
    {
        foreach (var (paramName, value) in extractedValues)
        {
            // Find property (case-insensitive)
            var property = commandType.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, paramName, StringComparison.OrdinalIgnoreCase));
            
            if (property == null || !property.CanWrite)
                continue;
            
            try
            {
                // Convert the string value to the property type
                if (_typeConverterRegistry.TryConvert(value, property.PropertyType, out var convertedValue))
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
                    var converted = Convert.ChangeType(value, property.PropertyType);
                    property.SetValue(command, converted);
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
        if (response == null)
            return;
        
        var responseType = response.GetType();
        
        // Check if the response has a custom ToString() implementation
        var toStringMethod = responseType.GetMethod("ToString", Type.EmptyTypes);
        if (toStringMethod != null && toStringMethod.DeclaringType != typeof(object))
        {
            // Use custom ToString
            System.Console.WriteLine(response.ToString());
        }
        else if (responseType.IsPrimitive || responseType == typeof(string) || responseType == typeof(decimal))
        {
            // Simple types - just display directly
            System.Console.WriteLine(response);
        }
        else
        {
            // Complex object - serialize to JSON for display
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            System.Console.WriteLine(json);
        }
    }
}