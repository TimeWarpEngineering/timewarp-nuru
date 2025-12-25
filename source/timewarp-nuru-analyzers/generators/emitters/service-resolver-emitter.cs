// Emits service resolution code for handler parameters.
// Generates GetRequiredService calls for injected dependencies.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to resolve services from the service provider.
/// Generates GetRequiredService or GetService calls for each service parameter.
/// </summary>
internal static class ServiceResolverEmitter
{
  /// <summary>
  /// Emits service resolution code for all service parameters in a handler.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="handler">The handler definition containing service parameters.</param>
  /// <param name="indent">Number of spaces for indentation.</param>
  public static void Emit(StringBuilder sb, HandlerDefinition handler, int indent = 6)
  {
    string indentStr = new(' ', indent);

    foreach (ParameterBinding param in handler.ServiceParameters)
    {
      EmitServiceResolution(sb, param, indentStr);
    }
  }

  /// <summary>
  /// Emits service resolution code for a collection of service definitions.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="services">The service definitions to resolve.</param>
  /// <param name="indent">Number of spaces for indentation.</param>
  public static void Emit(StringBuilder sb, IEnumerable<ServiceDefinition> services, int indent = 6)
  {
    string indentStr = new(' ', indent);

    foreach (ServiceDefinition service in services)
    {
      EmitServiceResolution(sb, service, indentStr);
    }
  }

  /// <summary>
  /// Emits a single service resolution from a parameter binding.
  /// </summary>
  private static void EmitServiceResolution(StringBuilder sb, ParameterBinding param, string indent)
  {
    string typeName = param.ParameterTypeName;
    string varName = param.ParameterName;

    if (param.IsOptional)
    {
      // Optional services use GetService (can return null)
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{typeName}? {varName} = app.Services.GetService<{typeName}>();");
    }
    else
    {
      // Required services use GetRequiredService (throws if not registered)
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{typeName} {varName} = app.Services.GetRequiredService<{typeName}>();");
    }
  }

  /// <summary>
  /// Emits a single service resolution from a service definition.
  /// </summary>
  private static void EmitServiceResolution(StringBuilder sb, ServiceDefinition service, string indent)
  {
    string typeName = service.ServiceTypeName;
    string varName = GetVariableName(service.ServiceTypeName);

    // Services from ServiceDefinition are always required
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"{indent}{typeName} {varName} = app.Services.GetRequiredService<{typeName}>();");
  }

  /// <summary>
  /// Generates a variable name from a type name.
  /// </summary>
  private static string GetVariableName(string typeName)
  {
    // Remove namespace prefixes
    string name = typeName;

    if (name.StartsWith("global::", StringComparison.Ordinal))
    {
      name = name[8..];
    }

    int lastDot = name.LastIndexOf('.');
    if (lastDot >= 0)
    {
      name = name[(lastDot + 1)..];
    }

    // Remove generic parameters
    int genericStart = name.IndexOf('<', StringComparison.Ordinal);
    if (genericStart >= 0)
    {
      name = name[..genericStart];
    }

    // Remove 'I' prefix for interfaces and convert to camelCase
    if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
    {
      name = name[1..];
    }

    // Convert to camelCase
    return char.ToLowerInvariant(name[0]) + name[1..];
  }
}
