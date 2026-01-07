// Emits generated command classes for delegate routes.
// These classes hold route parameters and implement interfaces from .Implements<T>().
// See kanban task #316 for design.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits generated command classes for delegate routes.
/// Each delegate route gets a command class that:
/// - Contains properties for route parameters
/// - Implements interfaces declared via .Implements&lt;T&gt;()
/// - Provides strongly-typed command instances for behaviors
/// </summary>
internal static class CommandClassEmitter
{
  /// <summary>
  /// Emits all generated command classes for delegate routes.
  /// Should be called after the class declaration but before any methods.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="routes">All routes in the application.</param>
  public static void EmitCommandClasses(StringBuilder sb, IEnumerable<RouteDefinition> routes)
  {
    int routeIndex = 0;
    bool hasAny = false;

    foreach (RouteDefinition route in routes)
    {
      // Only generate command classes for delegate routes
      if (route.Handler.HandlerKind == HandlerKind.Delegate)
      {
        if (!hasAny)
        {
          sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
          sb.AppendLine("  // GENERATED COMMAND CLASSES");
          sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
          sb.AppendLine();
          hasAny = true;
        }

        EmitCommandClass(sb, route, routeIndex);
      }

      routeIndex++;
    }

    if (hasAny)
    {
      sb.AppendLine();
    }
  }

  /// <summary>
  /// Emits a single command class for a delegate route.
  /// </summary>
  private static void EmitCommandClass(StringBuilder sb, RouteDefinition route, int routeIndex)
  {
    string className = GetCommandClassName(routeIndex);
    string interfaces = BuildInterfaceList(route);

    // Class declaration with optional interface implementations
    if (string.IsNullOrEmpty(interfaces))
    {
      sb.AppendLine($"  private sealed class {className}");
    }
    else
    {
      sb.AppendLine($"  private sealed class {className} : {interfaces}");
    }

    sb.AppendLine("  {");

    // Route parameter properties
    EmitParameterProperties(sb, route);

    // Interface implementation properties (from .Implements<T>())
    EmitInterfaceProperties(sb, route);

    sb.AppendLine("  }");
    sb.AppendLine();
  }

  /// <summary>
  /// Gets the generated command class name for a route.
  /// </summary>
  internal static string GetCommandClassName(int routeIndex)
  {
    return $"__Route_{routeIndex}_Command";
  }

  /// <summary>
  /// Builds the interface list for the class declaration.
  /// </summary>
  private static string BuildInterfaceList(RouteDefinition route)
  {
    if (!route.HasImplements)
      return string.Empty;

    return string.Join(", ", route.Implements.Select(i => i.FullInterfaceTypeName));
  }

  /// <summary>
  /// Emits properties for route parameters.
  /// </summary>
  private static void EmitParameterProperties(StringBuilder sb, RouteDefinition route)
  {
    IEnumerable<ParameterDefinition> parameters = route.Parameters;
    bool hasParams = false;

    foreach (ParameterDefinition param in parameters)
    {
      if (!hasParams)
      {
        sb.AppendLine("    // Route parameters");
        hasParams = true;
      }

      string propertyName = ToPascalCase(param.Name);
      string propertyType = GetPropertyType(param);

      sb.AppendLine($"    public {propertyType} {propertyName} {{ get; init; }}");
    }

    // Also include options as properties
    IEnumerable<OptionDefinition> options = route.Options;

    foreach (OptionDefinition option in options)
    {
      if (!hasParams)
      {
        sb.AppendLine("    // Route parameters");
        hasParams = true;
      }

      // Use LongForm as property name, or ParameterName for value options
      string propertyName = ToPascalCase(option.LongForm ?? option.ParameterName ?? "option");
      string propertyType = option.IsFlag ? "bool" : (option.ResolvedClrTypeName ?? "string");

      sb.AppendLine($"    public {propertyType} {propertyName} {{ get; init; }}");
    }
  }

  /// <summary>
  /// Emits interface properties from .Implements&lt;T&gt;() declarations.
  /// Uses auto-properties with set accessors to satisfy interface contracts.
  /// </summary>
  private static void EmitInterfaceProperties(StringBuilder sb, RouteDefinition route)
  {
    if (!route.HasImplements)
      return;

    sb.AppendLine();
    sb.AppendLine("    // Interface implementations (from .Implements<T>())");

    foreach (InterfaceImplementationDefinition impl in route.Implements)
    {
      foreach (PropertyAssignment prop in impl.Properties)
      {
        // Emit as auto-property with set accessor and initializer
        // This satisfies interfaces with { get; set; } contracts
        // The set is effectively unused since the value is baked in at compile-time
        sb.AppendLine($"    public {prop.PropertyType} {prop.PropertyName} {{ get; set; }} = {prop.ValueExpression};");
      }
    }
  }

  /// <summary>
  /// Gets the C# type for a parameter.
  /// </summary>
  private static string GetPropertyType(ParameterDefinition param)
  {
    // If there's a resolved CLR type, use it
    if (!string.IsNullOrEmpty(param.ResolvedClrTypeName))
      return param.ResolvedClrTypeName;

    // Catch-all parameters are string arrays
    if (param.IsCatchAll)
      return "string[]";

    // Default to string
    return "string";
  }

  /// <summary>
  /// Converts a string to PascalCase.
  /// Handles kebab-case (dry-run -> DryRun) and snake_case (dry_run -> DryRun).
  /// </summary>
  private static string ToPascalCase(string value)
  {
    if (string.IsNullOrEmpty(value))
      return value;

    // Handle kebab-case and snake_case by splitting on delimiters
    string[] parts = value.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 1)
    {
      // No delimiters, just capitalize first letter
      return char.ToUpperInvariant(value[0]) + value[1..];
    }

    // Join parts with each part's first letter capitalized
    StringBuilder result = new();

    foreach (string part in parts)
    {
      if (part.Length > 0)
      {
        result.Append(char.ToUpperInvariant(part[0]));
        if (part.Length > 1)
          result.Append(part[1..]);
      }
    }

    return result.ToString();
  }
}
