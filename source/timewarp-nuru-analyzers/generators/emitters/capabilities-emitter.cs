// Emits capabilities DTO construction code for AI agent discovery.
// Generates the PrintCapabilities method for --capabilities flag handling.
// Outputs a flat list of EndpointCapability objects with GroupPath arrays.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to build and serialize a CapabilitiesResponse DTO for AI agents.
/// Creates the PrintCapabilities method that outputs structured JSON endpoint information.
/// All routes are emitted as a flat list; group hierarchy is encoded in the GroupPath array.
/// </summary>
internal static class CapabilitiesEmitter
{
  /// <summary>
  /// Emits the PrintCapabilities method for an application.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="model">The application model containing routes and metadata.</param>
  /// <param name="methodSuffix">Suffix for method name (e.g., "_0" for multi-app assemblies).</param>
  public static void Emit(StringBuilder sb, AppModel model, string methodSuffix = "")
  {
    sb.AppendLine($"  private static void PrintCapabilities{methodSuffix}(ITerminal terminal)");
    sb.AppendLine("  {");

    EmitResponseConstruction(sb, model);

    sb.AppendLine("    string json = global::System.Text.Json.JsonSerializer.Serialize(response, global::TimeWarp.Nuru.CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);");
    sb.AppendLine("    terminal.WriteLine(json);");
    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the CapabilitiesResponse object construction code.
  /// </summary>
  private static void EmitResponseConstruction(StringBuilder sb, AppModel model)
  {
    string name = EscapeCSharpString(model.Name ?? "app");
    string version = EscapeCSharpString(model.Version ?? "0.0.0");

    sb.AppendLine("    global::TimeWarp.Nuru.CapabilitiesResponse response = new()");
    sb.AppendLine("    {");
    sb.AppendLine($"      Name = \"{name}\",");
    sb.AppendLine($"      Version = \"{version}\",");

    if (model.Description is not null)
    {
      string description = EscapeCSharpString(model.Description);
      sb.AppendLine($"      Description = \"{description}\",");
    }

    sb.AppendLine("      Endpoints =");
    sb.AppendLine("      [");

    ImmutableArray<RouteDefinition> routes = model.Routes;
    for (int i = 0; i < routes.Length; i++)
    {
      RouteDefinition route = routes[i];
      bool isLast = i == routes.Length - 1;
      EmitEndpointCapability(sb, route, isLast);
    }

    sb.AppendLine("      ]");
    sb.AppendLine("    };");
  }

  /// <summary>
  /// Emits a single EndpointCapability initializer.
  /// </summary>
  private static void EmitEndpointCapability(StringBuilder sb, RouteDefinition route, bool isLast)
  {
    string pattern = EscapeCSharpString(route.FullPattern);
    string[] groupPathParts = string.IsNullOrEmpty(route.GroupPrefix)
      ? []
      : route.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    string kindValue = MapMessageTypeToKind(route.MessageType);

    sb.AppendLine("        new global::TimeWarp.Nuru.EndpointCapability");
    sb.AppendLine("        {");
    sb.AppendLine($"          Pattern = \"{pattern}\",");

    // GroupPath
    sb.Append("          GroupPath = [");
    for (int i = 0; i < groupPathParts.Length; i++)
    {
      if (i > 0)
        sb.Append(", ");
      sb.Append($"\"{EscapeCSharpString(groupPathParts[i])}\"");
    }

    sb.AppendLine("],");

    // Aliases
    sb.Append("          Aliases = [");
    IReadOnlyList<string> aliases = [.. route.Aliases];
    for (int i = 0; i < aliases.Count; i++)
    {
      if (i > 0)
        sb.Append(", ");
      sb.Append($"\"{EscapeCSharpString(aliases[i])}\"");
    }

    sb.AppendLine("],");

    // Description
    if (route.Description is not null)
    {
      string description = EscapeCSharpString(route.Description);
      sb.AppendLine($"          Description = \"{description}\",");
    }

    // Kind
    sb.AppendLine($"          Kind = global::TimeWarp.Nuru.EndpointKind.{kindValue},");

    // Parameters
    sb.AppendLine("          Parameters =");
    sb.AppendLine("          [");
    IReadOnlyList<ParameterDefinition> parameters = [.. route.Parameters];
    for (int i = 0; i < parameters.Count; i++)
    {
      ParameterDefinition param = parameters[i];
      bool paramIsLast = i == parameters.Count - 1;
      EmitParameterCapability(sb, param, paramIsLast);
    }

    sb.AppendLine("          ],");

    // Options
    sb.AppendLine("          Options =");
    sb.AppendLine("          [");
    IReadOnlyList<OptionDefinition> options = [.. route.Options];
    for (int i = 0; i < options.Count; i++)
    {
      OptionDefinition option = options[i];
      bool optionIsLast = i == options.Count - 1;
      EmitOptionCapability(sb, option, optionIsLast);
    }

    sb.AppendLine("          ]");

    string comma = isLast ? "" : ",";
    sb.AppendLine($"        }}{comma}");
  }

  /// <summary>
  /// Emits a single ParameterCapability initializer.
  /// </summary>
  private static void EmitParameterCapability(StringBuilder sb, ParameterDefinition param, bool isLast)
  {
    string name = EscapeCSharpString(param.Name);
    string type = EscapeCSharpString(param.TypeConstraint ?? "string");
    string required = param.IsOptional ? "false" : "true";
    string isCatchAll = param.IsCatchAll ? "true" : "false";

    sb.AppendLine("            new global::TimeWarp.Nuru.ParameterCapability");
    sb.AppendLine("            {");
    sb.AppendLine($"              Name = \"{name}\",");
    sb.AppendLine($"              Type = \"{type}\",");
    sb.AppendLine($"              Required = {required},");
    sb.AppendLine($"              IsCatchAll = {isCatchAll},");

    if (param.Description is not null)
    {
      string description = EscapeCSharpString(param.Description);
      sb.AppendLine($"              Description = \"{description}\",");
    }

    if (param.DefaultValue is not null)
    {
      string defaultValue = EscapeCSharpString(param.DefaultValue);
      sb.AppendLine($"              DefaultValue = \"{defaultValue}\",");
    }

    string comma = isLast ? "" : ",";
    sb.AppendLine($"            }}{comma}");
  }

  /// <summary>
  /// Emits a single OptionCapability initializer.
  /// </summary>
  private static void EmitOptionCapability(StringBuilder sb, OptionDefinition option, bool isLast)
  {
    string name = EscapeCSharpString(option.LongForm ?? option.ShortForm ?? "");
    string type = EscapeCSharpString(option.TypeConstraint ?? (option.IsFlag ? "bool" : "string"));
    string required = option.IsOptional ? "false" : "true";
    string isFlag = option.IsFlag ? "true" : "false";
    string isRepeated = option.IsRepeated ? "true" : "false";

    sb.AppendLine("            new global::TimeWarp.Nuru.OptionCapability");
    sb.AppendLine("            {");
    sb.AppendLine($"              Name = \"{name}\",");

    if (option.ShortForm is not null)
    {
      string alias = EscapeCSharpString(option.ShortForm);
      sb.AppendLine($"              Alias = \"{alias}\",");
    }

    sb.AppendLine($"              Type = \"{type}\",");
    sb.AppendLine($"              Required = {required},");
    sb.AppendLine($"              IsFlag = {isFlag},");
    sb.AppendLine($"              IsRepeated = {isRepeated},");

    if (option.Description is not null)
    {
      string description = EscapeCSharpString(option.Description);
      sb.AppendLine($"              Description = \"{description}\",");
    }

    if (option.DefaultValueLiteral is not null)
    {
      string defaultValue = EscapeCSharpString(option.DefaultValueLiteral);
      sb.AppendLine($"              DefaultValue = \"{defaultValue}\",");
    }

    string comma = isLast ? "" : ",";
    sb.AppendLine($"            }}{comma}");
  }

  /// <summary>
  /// Maps a MessageType string (PascalCase) to an EndpointKind enum member name.
  /// </summary>
  private static string MapMessageTypeToKind(string messageType) =>
    messageType switch
    {
      "Query" => "Query",
      "Command" => "Command",
      "IdempotentCommand" => "IdempotentCommand",
      _ => "Unspecified"
    };

  /// <summary>
  /// Escapes a string for use in a C# string literal.
  /// </summary>
  private static string EscapeCSharpString(string value)
  {
    return value
      .Replace("\\", "\\\\", StringComparison.Ordinal)
      .Replace("\"", "\\\"", StringComparison.Ordinal)
      .Replace("\n", "\\n", StringComparison.Ordinal)
      .Replace("\r", "\\r", StringComparison.Ordinal)
      .Replace("\t", "\\t", StringComparison.Ordinal);
  }
}
