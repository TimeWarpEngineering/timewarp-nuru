namespace TimeWarp.Nuru;

/// <summary>
/// Provides shared response display functionality for command execution.
/// </summary>
public static class ResponseDisplay
{
  /// <summary>
  /// Formats and writes a command response to the terminal.
  /// </summary>
  /// <remarks>
  /// This method may serialize unknown response types to JSON, which requires reflection
  /// and is not fully compatible with Native AOT. For best AOT support, return primitive types
  /// or types with custom ToString implementations from commands.
  /// </remarks>
  /// <param name="response">The response object to display.</param>
  /// <param name="terminal">The terminal to write to.</param>
  [RequiresUnreferencedCode("Response serialization may require types not known at compile time")]
  [RequiresDynamicCode("JSON serialization of unknown response types may require dynamic code generation")]
  public static void Write(object? response, ITerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);

    if (response is null)
      return;

    Type responseType = response.GetType();

    // Check if this is Unit.Value (represents no return value)
    if (responseType.Name == "Unit" && responseType.Namespace == "TimeWarp.Nuru")
      return;

    // Simple types - display directly
    if (responseType.IsPrimitive || responseType == typeof(string) || responseType == typeof(decimal))
    {
      terminal.WriteLine(response.ToString());
      return;
    }

    // For complex objects, check if ToString is overridden by testing the output
    string stringValue = response.ToString() ?? "";

    // If ToString returns the default type name, use JSON instead
    if (stringValue == responseType.FullName || stringValue == responseType.Name)
    {
      // Complex object without custom ToString - serialize to JSON for display
      string json = JsonSerializer.Serialize(response, NuruJsonSerializerContext.Default.Options);
      terminal.WriteLine(json);
    }
    else
    {
      // Custom ToString - use it
      terminal.WriteLine(stringValue);
    }
  }
}
