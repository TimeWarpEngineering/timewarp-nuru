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
  /// <param name="compilation">Optional Roslyn compilation for enum value extraction.</param>
  public static void Emit(StringBuilder sb, AppModel model, string methodSuffix = "", Compilation? compilation = null)
  {
    sb.AppendLine($"  private static void PrintCapabilities{methodSuffix}(ITerminal terminal, string? groupFilter = null)");
    sb.AppendLine("  {");

    EmitResponseConstruction(sb, model, compilation);

    sb.AppendLine("    string json = global::System.Text.Json.JsonSerializer.Serialize(response, global::TimeWarp.Nuru.CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);");
    sb.AppendLine("    terminal.WriteLine(json);");
    sb.AppendLine("  }");

    EmitSearchCapabilities(sb, model, methodSuffix);
  }

  /// <summary>
  /// Emits the SearchCapabilitiesAsync method for calling nuru-search subprocess.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="model">The application model containing name and version.</param>
  /// <param name="methodSuffix">Suffix for method name (e.g., "_0" for multi-app assemblies).</param>
  private static void EmitSearchCapabilities(StringBuilder sb, AppModel model, string methodSuffix)
  {
    string? nameLiteral = model.Name is not null ? $"\"{EscapeCSharpString(model.Name)}\"" : null;

    sb.AppendLine();
    sb.AppendLine($"  private static async global::System.Threading.Tasks.Task<int> SearchCapabilitiesAsync{methodSuffix}(ITerminal terminal, string query, string? groupFilter = null)");
    sb.AppendLine("  {");
    sb.AppendLine("    // Build nuru-search arguments");
    sb.AppendLine("    global::System.Collections.Generic.List<string> args = new()");
    sb.AppendLine("    {");
    sb.AppendLine("      \"search\",");
    sb.AppendLine($"      \"--cli\", {nameLiteral ?? "global::System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name!"},");
    sb.AppendLine("    };");
    sb.AppendLine();
    sb.AppendLine("    if (groupFilter is not null)");
    sb.AppendLine("    {");
    sb.AppendLine("      args.Add(\"--group\");");
    sb.AppendLine("      args.Add(groupFilter);");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    args.Add(\"--query\");");
    sb.AppendLine("    args.Add(query);");
    sb.AppendLine();
    sb.AppendLine("    // Execute nuru-search via Amuru (wrap in try-catch for missing tool)");
    sb.AppendLine("    global::TimeWarp.Amuru.CommandOutput output;");
    sb.AppendLine("    try");
    sb.AppendLine("    {");
    sb.AppendLine("      output = await global::TimeWarp.Amuru.Shell.Builder(\"nuru-search\")");
    sb.AppendLine("        .WithArguments([.. args])");
    sb.AppendLine("        .WithNoValidation()");
    sb.AppendLine("        .CaptureAsync().ConfigureAwait(false);");
    sb.AppendLine("    }");
    sb.AppendLine("    catch (global::System.ComponentModel.Win32Exception)");
    sb.AppendLine("    {");
    sb.AppendLine("      // nuru-search not found - show install instructions");
    sb.AppendLine("      await terminal.WriteErrorLineAsync(\"Search requires timewarp-nuru-search to be installed.\").ConfigureAwait(false);");
    sb.AppendLine("      await terminal.WriteErrorLineAsync(\"Install with: dotnet tool install --global TimeWarp.Nuru.Search\").ConfigureAwait(false);");
    sb.AppendLine("      return 1;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    if (!output.Success)");
    sb.AppendLine("    {");
    sb.AppendLine("      await terminal.WriteErrorLineAsync(\"Search requires timewarp-nuru-search to be installed.\").ConfigureAwait(false);");
    sb.AppendLine("      await terminal.WriteErrorLineAsync(\"Install with: dotnet tool install --global TimeWarp.Nuru.Search\").ConfigureAwait(false);");
    sb.AppendLine("      return 1;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    await terminal.WriteLineAsync(output.Stdout).ConfigureAwait(false);");
    sb.AppendLine("    return 0;");
    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the CapabilitiesResponse object construction code.
  /// </summary>
  private static void EmitResponseConstruction(StringBuilder sb, AppModel model, Compilation? compilation)
  {
    string? nameLiteral = model.Name is not null ? $"\"{EscapeCSharpString(model.Name)}\"" : null;
    string version = EscapeCSharpString(model.Version ?? "0.0.0");

    sb.AppendLine("    global::System.Collections.Generic.List<global::TimeWarp.Nuru.EndpointCapability> __endpoints = new();");
    sb.AppendLine();

    ImmutableArray<RouteDefinition> routes = model.Routes;
    for (int i = 0; i < routes.Length; i++)
    {
      RouteDefinition route = routes[i];
      EmitEndpointCapabilityAdd(sb, route, compilation);
    }

    sb.AppendLine();
    sb.AppendLine("    // Filter endpoints by group prefix if groupFilter is provided");
    sb.AppendLine("    global::System.Collections.Generic.IReadOnlyList<global::TimeWarp.Nuru.EndpointCapability> __filteredEndpoints = __endpoints;");
    sb.AppendLine("    if (groupFilter is not null)");
    sb.AppendLine("    {");
    sb.AppendLine("      string[] __groupParts = groupFilter.Split('.');");
    sb.AppendLine("      __filteredEndpoints = __endpoints");
    sb.AppendLine("        .Where(e => e.GroupPath.Count >= __groupParts.Length &&");
    sb.AppendLine("          global::System.Linq.Enumerable.Zip(__groupParts, e.GroupPath)");
    sb.AppendLine("            .All(p => string.Equals(p.First, p.Second, global::System.StringComparison.OrdinalIgnoreCase)))");
    sb.AppendLine("        .ToList();");
    sb.AppendLine("    }");
    sb.AppendLine();

    sb.AppendLine("    global::TimeWarp.Nuru.CapabilitiesResponse response = new()");
    sb.AppendLine("    {");
    sb.AppendLine($"      Name = {nameLiteral ?? "global::System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name!"},");
    sb.AppendLine($"      Version = \"{version}\",");

    if (model.Description is not null)
    {
      string description = EscapeCSharpString(model.Description);
      sb.AppendLine($"      Description = \"{description}\",");
    }

    sb.AppendLine("      Filter = groupFilter is null ? null : new global::TimeWarp.Nuru.CapabilitiesFilter { Group = groupFilter },");
    sb.AppendLine("      Endpoints = __filteredEndpoints");
    sb.AppendLine("    };");
  }

  /// <summary>
  /// Emits code to add a single EndpointCapability to the endpoints list.
  /// </summary>
  private static void EmitEndpointCapabilityAdd(StringBuilder sb, RouteDefinition route, Compilation? compilation)
  {
    string pattern = EscapeCSharpString(route.FullPattern);
    string[] groupPathParts = string.IsNullOrEmpty(route.GroupPrefix)
      ? []
      : route.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    string kindValue = MapMessageTypeToKind(route.MessageType);

    sb.AppendLine("    __endpoints.Add(");
    sb.AppendLine("      new global::TimeWarp.Nuru.EndpointCapability");
    sb.AppendLine("      {");
    sb.AppendLine($"        Pattern = \"{pattern}\",");

    // GroupPath
    sb.Append("        GroupPath = [");
    for (int i = 0; i < groupPathParts.Length; i++)
    {
      if (i > 0)
        sb.Append(", ");
      sb.Append($"\"{EscapeCSharpString(groupPathParts[i])}\"");
    }

    sb.AppendLine("],");

    // Aliases
    sb.Append("        Aliases = [");
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
      sb.AppendLine($"        Description = \"{description}\",");
    }

    // Kind
    sb.AppendLine($"        Kind = global::TimeWarp.Nuru.EndpointKind.{kindValue},");

    // Parameters
    sb.AppendLine("        Parameters =");
    sb.AppendLine("        [");
    IReadOnlyList<ParameterDefinition> parameters = [.. route.Parameters];
    for (int i = 0; i < parameters.Count; i++)
    {
      ParameterDefinition param = parameters[i];
      bool paramIsLast = i == parameters.Count - 1;
      string? handlerTypeName = route.Handler.Parameters
        .FirstOrDefault(p => p.Source == BindingSource.Parameter &&
          string.Equals(p.SourceName, param.Name, StringComparison.OrdinalIgnoreCase))
        ?.ParameterTypeName;
      EmitParameterCapability(sb, param, paramIsLast, compilation, handlerTypeName);
    }

    sb.AppendLine("        ],");

    // Options
    sb.AppendLine("        Options =");
    sb.AppendLine("        [");
    IReadOnlyList<OptionDefinition> options = [.. route.Options];
    for (int i = 0; i < options.Count; i++)
    {
      OptionDefinition option = options[i];
      bool optionIsLast = i == options.Count - 1;
      string? handlerTypeName = route.Handler.Parameters
        .FirstOrDefault(p => p.Source == BindingSource.Option &&
          string.Equals(p.SourceName, option.LongForm ?? option.ShortForm, StringComparison.OrdinalIgnoreCase))
        ?.ParameterTypeName;
      EmitOptionCapability(sb, option, optionIsLast, compilation, handlerTypeName);
    }

    sb.AppendLine("        ]");
    sb.AppendLine("      });");
  }

  /// <summary>
  /// Emits a single ParameterCapability initializer.
  /// </summary>
  private static void EmitParameterCapability(StringBuilder sb, ParameterDefinition param, bool isLast, Compilation? compilation, string? handlerTypeName = null)
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

    // Use resolved CLR type name first; fall back to handler's parameter type when pattern has no type annotation
    string? typeNameForEnum = param.ResolvedClrTypeName ?? handlerTypeName;
    string[]? allowedValues = ExtractEnumValues(typeNameForEnum, compilation);
    if (allowedValues is not null)
    {
      sb.Append("              AllowedValues = [");
      for (int i = 0; i < allowedValues.Length; i++)
      {
        if (i > 0)
          sb.Append(", ");

        sb.Append($"\"{EscapeCSharpString(allowedValues[i])}\"");
      }

      sb.AppendLine("],");
    }

    string comma = isLast ? "" : ",";
    sb.AppendLine($"            }}{comma}");
  }

  /// <summary>
  /// Emits a single OptionCapability initializer.
  /// </summary>
  private static void EmitOptionCapability(StringBuilder sb, OptionDefinition option, bool isLast, Compilation? compilation, string? handlerTypeName = null)
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

    // Use resolved CLR type name first; fall back to handler's parameter type when pattern has no type annotation
    string? typeNameForEnum = option.ResolvedClrTypeName ?? handlerTypeName;
    string[]? allowedValues = ExtractEnumValues(typeNameForEnum, compilation);
    if (allowedValues is not null)
    {
      sb.Append("              AllowedValues = [");
      for (int i = 0; i < allowedValues.Length; i++)
      {
        if (i > 0)
          sb.Append(", ");

        sb.Append($"\"{EscapeCSharpString(allowedValues[i])}\"");
      }

      sb.AppendLine("],");
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
  /// Extracts enum member names for a given CLR type name using the Roslyn compilation.
  /// Returns null if the type is not an enum or if compilation is unavailable.
  /// </summary>
  private static string[]? ExtractEnumValues(string? resolvedClrTypeName, Compilation? compilation)
  {
    if (compilation is null || resolvedClrTypeName is null)
      return null;

    string typeName = resolvedClrTypeName;
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      typeName = typeName[8..];
    if (typeName.EndsWith('?'))
      typeName = typeName[..^1];

    INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName(typeName);
    if (typeSymbol?.TypeKind != TypeKind.Enum)
      return null;

    string[] values = [.. typeSymbol.GetMembers()
      .OfType<IFieldSymbol>()
      .Where(f => f.HasConstantValue)
      .Select(f => f.Name)];

    return values.Length > 0 ? values : null;
  }

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
