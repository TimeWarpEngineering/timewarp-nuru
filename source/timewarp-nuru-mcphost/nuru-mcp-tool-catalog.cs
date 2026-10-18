#region Purpose
// Project the Nuru capabilities catalog onto MCP tool definitions: name, description, input schema, annotations.
#endregion

#region Design
// Pure functions over CapabilitiesResponse, no app types and no reflection, so tools/list is the same
// contract --capabilities prints. Schema keys are the catalog names (the --json-args keys) and JSON types
// are the kinds the generated binder accepts: integers and floats as numbers, bool as boolean, a repeated
// option or catch-all as an array, everything else (enums, Guid, dates, converters) as string.
// additionalProperties is false because the binder rejects unknown keys.
#endregion

namespace TimeWarp.Nuru.McpHost;

/// <summary>
/// One route published as an MCP tool.
/// </summary>
/// <param name="Tool">The MCP tool definition (<c>tools/list</c> entry).</param>
/// <param name="Endpoint">The catalog endpoint the tool calls. <see cref="EndpointCapability.Pattern"/> is the route for <see cref="NuruApp.TryBind"/>.</param>
public sealed record NuruMcpToolDefinition(Tool Tool, EndpointCapability Endpoint);

/// <summary>
/// Builds MCP tool definitions from a Nuru capabilities catalog.
/// </summary>
public static class NuruMcpToolCatalog
{
  /// <summary>
  /// Builds one tool per endpoint in <paramref name="catalog"/>, after the group filter in <paramref name="options"/>.
  /// </summary>
  /// <param name="catalog">The app's catalog, usually <see cref="NuruApp.Capabilities"/>.</param>
  /// <param name="options">Host options; null uses the defaults.</param>
  /// <returns>The tools, in catalog order.</returns>
  /// <remarks>
  /// Tool names are unique: a route whose name is already taken gets <c>_2</c>, <c>_3</c>, and so on,
  /// in catalog order.
  /// </remarks>
  public static IReadOnlyList<NuruMcpToolDefinition> CreateTools(CapabilitiesResponse catalog, NuruMcpHostOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(catalog);
    options ??= new NuruMcpHostOptions();

    CapabilitiesResponse published = string.IsNullOrEmpty(options.GroupFilter)
      ? catalog
      : catalog.FilterByGroup(options.GroupFilter);

    HashSet<string> names = new(StringComparer.Ordinal);
    List<NuruMcpToolDefinition> tools = [];

    foreach (EndpointCapability endpoint in published.Endpoints)
    {
      string baseName = GetToolName(endpoint, options.ToolNameSeparator);
      string name = baseName;
      for (int suffix = 2; !names.Add(name); suffix++)
      {
        name = $"{baseName}_{suffix.ToString(CultureInfo.InvariantCulture)}";
      }

      Tool tool = new()
      {
        Name = name,
        Description = endpoint.Description,
        InputSchema = CreateInputSchema(endpoint),
        Annotations = CreateAnnotations(endpoint)
      };

      tools.Add(new NuruMcpToolDefinition(tool, endpoint));
    }

    return tools;
  }

  /// <summary>
  /// The tool name for a route: its literal words (group words first) joined by <paramref name="separator"/>.
  /// </summary>
  /// <param name="endpoint">The catalog endpoint.</param>
  /// <param name="separator">Separator between words; default <c>_</c>.</param>
  /// <returns>
  /// The name, with characters outside <c>A-Z a-z 0-9 _ - .</c> replaced by <c>_</c>.
  /// A route with no literal words is <see cref="NuruMcpHostOptions.DefaultRouteToolName"/>.
  /// </returns>
  public static string GetToolName(EndpointCapability endpoint, string separator = NuruMcpHostOptions.DefaultToolNameSeparator)
  {
    ArgumentNullException.ThrowIfNull(endpoint);
    ArgumentNullException.ThrowIfNull(separator);

    List<string> words = [];
    foreach (string token in endpoint.Pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
      // Parameters ({x}, {x:int}, {*args}) and options (--name, -n) are not part of the name.
      if (token.StartsWith('{') || token.StartsWith('-'))
        continue;

      words.Add(token);
    }

    // A grouped endpoint pattern may omit the group words (endpoint DSL); the catalog lists them in groupPath.
    if (endpoint.GroupPath.Count > 0 && !StartsWith(words, endpoint.GroupPath))
    {
      words.InsertRange(0, endpoint.GroupPath);
    }

    if (words.Count == 0)
      return NuruMcpHostOptions.DefaultRouteToolName;

    return Sanitize(string.Join(separator, words));
  }

  /// <summary>
  /// The JSON Schema for a route's arguments, built from its <c>parameters[]</c> and <c>options[]</c>.
  /// </summary>
  /// <param name="endpoint">The catalog endpoint.</param>
  /// <returns>An object schema whose property names are the <c>--json-args</c> keys.</returns>
  public static JsonElement CreateInputSchema(EndpointCapability endpoint)
  {
    ArgumentNullException.ThrowIfNull(endpoint);

    ArrayBufferWriter<byte> buffer = new();
    using (Utf8JsonWriter writer = new(buffer))
    {
      writer.WriteStartObject();
      writer.WriteString("type", "object");
      writer.WritePropertyName("properties");
      writer.WriteStartObject();

      List<string> required = [];

      foreach (ParameterCapability parameter in endpoint.Parameters)
      {
        writer.WritePropertyName(parameter.Name);
        WriteMemberSchema(writer, parameter.Type, isArray: parameter.IsCatchAll, parameter.Description, parameter.DefaultValue, parameter.AllowedValues);
        if (parameter.Required)
          required.Add(parameter.Name);
      }

      foreach (OptionCapability option in endpoint.Options)
      {
        writer.WritePropertyName(option.Name);
        string type = option.IsFlag ? "bool" : option.Type;
        WriteMemberSchema(writer, type, isArray: option.IsRepeated, option.Description, option.DefaultValue, option.AllowedValues);
        // A flag binds false when its key is absent, whatever the catalog's required says.
        if (option.Required && !option.IsFlag)
          required.Add(option.Name);
      }

      writer.WriteEndObject();

      if (required.Count > 0)
      {
        writer.WritePropertyName("required");
        writer.WriteStartArray();
        foreach (string name in required)
          writer.WriteStringValue(name);
        writer.WriteEndArray();
      }

      writer.WriteBoolean("additionalProperties", false);
      writer.WriteEndObject();
    }

    using JsonDocument document = JsonDocument.Parse(buffer.WrittenMemory);
    return document.RootElement.Clone();
  }

  /// <summary>
  /// Tool annotations for a route, from its kind and the <c>destructive</c> catalog member.
  /// </summary>
  /// <param name="endpoint">The catalog endpoint.</param>
  /// <returns>
  /// Query: read-only and idempotent. IdempotentCommand: idempotent, not read-only. Command: neither.
  /// Unspecified: no hints. <c>destructiveHint</c> is true only for a route marked <see cref="DestructiveAttribute"/>,
  /// and otherwise unset (never inferred).
  /// </returns>
  public static ToolAnnotations CreateAnnotations(EndpointCapability endpoint)
  {
    ArgumentNullException.ThrowIfNull(endpoint);

    (bool? readOnly, bool? idempotent) = endpoint.Kind switch
    {
      EndpointKind.Query => (true, true),
      EndpointKind.IdempotentCommand => (false, true),
      EndpointKind.Command => (false, false),
      _ => ((bool?)null, (bool?)null)
    };

    return new ToolAnnotations
    {
      ReadOnlyHint = readOnly,
      IdempotentHint = idempotent,
      DestructiveHint = endpoint.Destructive == true ? true : null
    };
  }

  /// <summary>
  /// Server instructions: the app description, then <see cref="NuruMcpHostOptions.Instructions"/>,
  /// separated by a blank line.
  /// </summary>
  /// <param name="catalog">The app's catalog.</param>
  /// <param name="options">Host options; null uses the defaults.</param>
  /// <returns>The instructions, or null when both parts are empty.</returns>
  public static string? CreateInstructions(CapabilitiesResponse catalog, NuruMcpHostOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(catalog);

    string[] parts =
    [
      .. new[] { catalog.Description, options?.Instructions }
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Select(p => p!.Trim())
    ];

    return parts.Length == 0 ? null : string.Join("\n\n", parts);
  }

  /// <summary>
  /// The JSON Schema <c>type</c> for a catalog type name (element type for arrays).
  /// </summary>
  /// <param name="catalogType">The <c>type</c> string from a parameter or option, for example <c>int</c> or <c>double?</c>.</param>
  /// <returns><c>integer</c>, <c>number</c>, <c>boolean</c>, or <c>string</c>.</returns>
  public static string GetJsonSchemaType(string catalogType)
  {
    ArgumentNullException.ThrowIfNull(catalogType);

    return NormalizeTypeName(catalogType) switch
    {
      "int" or "long" or "short" or "byte" or "sbyte" or "uint" or "ulong" or "ushort"
        or "int16" or "int32" or "int64" or "uint16" or "uint32" or "uint64" or "biginteger" => "integer",
      "double" or "float" or "single" or "decimal" or "half" => "number",
      "bool" or "boolean" => "boolean",
      _ => "string"
    };
  }

  private static void WriteMemberSchema(
    Utf8JsonWriter writer,
    string catalogType,
    bool isArray,
    string? description,
    string? defaultValue,
    IReadOnlyList<string>? allowedValues)
  {
    bool nullable = catalogType.EndsWith('?');
    bool typedArray = catalogType.EndsWith("[]", StringComparison.Ordinal);
    string itemType = typedArray ? catalogType[..^2] : catalogType;
    isArray = isArray || typedArray;
    string schemaType = GetJsonSchemaType(itemType);

    writer.WriteStartObject();

    if (isArray)
    {
      writer.WriteString("type", "array");
      writer.WritePropertyName("items");
      writer.WriteStartObject();
      WriteScalarSchema(writer, itemType, schemaType, nullable: false, allowedValues);
      writer.WriteEndObject();
    }
    else
    {
      WriteScalarSchema(writer, itemType, schemaType, nullable, allowedValues);
      WriteDefault(writer, schemaType, defaultValue, allowedValues);
    }

    if (!string.IsNullOrEmpty(description))
      writer.WriteString("description", description);

    writer.WriteEndObject();
  }

  private static void WriteScalarSchema(Utf8JsonWriter writer, string catalogType, string schemaType, bool nullable, IReadOnlyList<string>? allowedValues)
  {
    if (nullable)
    {
      writer.WritePropertyName("type");
      writer.WriteStartArray();
      writer.WriteStringValue(schemaType);
      writer.WriteStringValue("null");
      writer.WriteEndArray();
    }
    else
    {
      writer.WriteString("type", schemaType);
    }

    string? format = NormalizeTypeName(catalogType) switch
    {
      "guid" => "uuid",
      "datetime" or "datetimeoffset" => "date-time",
      "dateonly" => "date",
      "timeonly" => "time",
      "uri" => "uri",
      _ => null
    };

    if (format is not null && schemaType == "string")
      writer.WriteString("format", format);

    if (allowedValues is { Count: > 0 })
    {
      writer.WritePropertyName("enum");
      writer.WriteStartArray();
      foreach (string value in allowedValues)
        writer.WriteStringValue(value);
      if (nullable)
        writer.WriteNullValue();
      writer.WriteEndArray();
    }
  }

  private static void WriteDefault(Utf8JsonWriter writer, string schemaType, string? defaultValue, IReadOnlyList<string>? allowedValues)
  {
    // The catalog carries the C# initializer text: "\"nearest\"", "1.5m", "Env.Dev", "null".
    if (defaultValue is null || defaultValue == "null")
      return;

    string literal = defaultValue.Trim();
    switch (schemaType)
    {
      case "integer" when long.TryParse(literal.TrimEnd('L', 'l', 'U', 'u'), NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer):
        writer.WriteNumber("default", integer);
        break;
      case "number" when double.TryParse(literal.TrimEnd('M', 'm', 'F', 'f', 'D', 'd'), NumberStyles.Float, CultureInfo.InvariantCulture, out double number):
        writer.WriteNumber("default", number);
        break;
      case "boolean" when bool.TryParse(literal, out bool boolean):
        writer.WriteBoolean("default", boolean);
        break;
      case "string":
        writer.WriteString("default", StringDefault(literal, allowedValues));
        break;
    }
  }

  private static string StringDefault(string literal, IReadOnlyList<string>? allowedValues)
  {
    if (literal.Length >= 2 && ((literal[0] == '"' && literal[^1] == '"') || (literal[0] == '\'' && literal[^1] == '\'')))
    {
      string unquoted = literal[1..^1];
      return unquoted.Contains('\\', StringComparison.Ordinal) ? Regex.Unescape(unquoted) : unquoted;
    }

    // An enum default is written as "Env.Dev"; publish the member name when it is an allowed value.
    int dot = literal.LastIndexOf('.');
    if (allowedValues is { Count: > 0 } && !allowedValues.Contains(literal) && dot >= 0 && allowedValues.Contains(literal[(dot + 1)..]))
      return literal[(dot + 1)..];

    return literal;
  }

  private static string NormalizeTypeName(string catalogType)
  {
    string name = catalogType.Trim().TrimEnd('?');
    if (name.StartsWith("global::", StringComparison.Ordinal))
      name = name["global::".Length..];
    if (name.StartsWith("System.", StringComparison.Ordinal))
      name = name["System.".Length..];
    if (name.StartsWith("Numerics.", StringComparison.Ordinal))
      name = name["Numerics.".Length..];
    return name.ToLowerInvariant();
  }

  private static bool StartsWith(List<string> words, IReadOnlyList<string> prefix)
  {
    if (words.Count < prefix.Count)
      return false;

    for (int i = 0; i < prefix.Count; i++)
    {
      if (!string.Equals(words[i], prefix[i], StringComparison.Ordinal))
        return false;
    }

    return true;
  }

  private static string Sanitize(string name)
  {
    StringBuilder builder = new(name.Length);
    foreach (char c in name)
    {
      builder.Append(char.IsAsciiLetterOrDigit(c) || c is '_' or '-' or '.' ? c : '_');
    }

    return builder.ToString();
  }
}
