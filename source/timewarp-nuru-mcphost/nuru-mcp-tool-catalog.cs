#region Purpose
// Project the Nuru capabilities catalog onto MCP tool definitions: name, description, input schema, annotations.
#endregion

#region Design
// Pure functions over CapabilitiesResponse, no app types and no reflection, so tools/list is the same
// contract --capabilities prints. Schema keys are the catalog names (the --json-args keys) and JSON types
// are the kinds the generated binder accepts: integers and floats as numbers, bool as boolean, a repeated
// option or catch-all as an array, everything else (enums, Guid, dates, converters) as string. A member
// that binds as one JSON value uses the schema the catalog carries for it (exported from the binder's
// type), and is an object or array when the catalog has none.
// additionalProperties is false because the binder rejects unknown keys.
// Defaults: both the JSON default keyword and "Default: X." at the end of the description. Codex drops
// default when it converts a schema for the model; Claude Code and Grok keep both (task 508-015).
// Annotations come from the catalog (kind, destructive, openWorld, title); a per-tool entry in
// NuruMcpHostOptions.ToolAnnotations overrides each hint it sets.
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
public static partial class NuruMcpToolCatalog
{
  // Catalog type of a member that binds as one JSON value: "json" plus the CLR type name.
  private const string JsonTypePrefix = "json ";

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

      ToolAnnotations annotations = CreateAnnotations(endpoint);
      if (options.ToolAnnotations.TryGetValue(name, out ToolAnnotations? overrides))
        annotations = MergeAnnotations(annotations, overrides);

      Tool tool = new()
      {
        Name = name,
        Title = annotations.Title,
        Description = endpoint.Description,
        InputSchema = CreateInputSchema(endpoint),
        Annotations = annotations
      };

      tools.Add(new NuruMcpToolDefinition(tool, endpoint));
    }

    return tools;
  }

  /// <summary>
  /// The tool name for a route: the literal words of its pattern joined by <paramref name="separator"/>.
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
      if (token.StartsWith('{', StringComparison.Ordinal) || token.StartsWith('-', StringComparison.Ordinal))
        continue;

      words.Add(token);
    }

    // The pattern already holds any route-group prefix. groupPath is metadata (an [Operation(Group)]
    // path is not a command prefix), so it never adds words to the name.

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
        WriteMemberSchema(writer, parameter.Type, isArray: parameter.IsCatchAll, parameter.Description, parameter.DefaultValue, parameter.AllowedValues, schema: null);
        if (parameter.Required)
          required.Add(parameter.Name);
      }

      foreach (OptionCapability option in endpoint.Options)
      {
        writer.WritePropertyName(option.Name);
        string type = option.IsFlag ? "bool" : option.Type;
        // A flag binds false when its key is absent.
        string? defaultValue = option.IsFlag ? option.DefaultValue ?? "false" : option.DefaultValue;
        WriteMemberSchema(writer, type, isArray: option.IsRepeated, option.Description, defaultValue, option.AllowedValues, option.Schema);
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
  /// Tool annotations for a route, from its kind and the <c>destructive</c>, <c>openWorld</c> and
  /// <c>title</c> catalog members.
  /// </summary>
  /// <param name="endpoint">The catalog endpoint.</param>
  /// <returns>
  /// Query: read-only and idempotent. IdempotentCommand: idempotent, not read-only. Command: neither.
  /// Unspecified: no hints. <c>destructiveHint</c> and <c>openWorldHint</c> are the values the route
  /// declares with <c>[TimeWarp.Contracts.Destructive]</c> and <c>[TimeWarp.Contracts.OpenWorld]</c>, and
  /// otherwise unset (never inferred). <c>title</c> is the operation's <c>Title</c>, or
  /// <see cref="System.ComponentModel.DisplayNameAttribute"/> on a route class with no operation.
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
      Title = endpoint.Title,
      ReadOnlyHint = readOnly,
      IdempotentHint = idempotent,
      DestructiveHint = endpoint.Destructive,
      OpenWorldHint = endpoint.OpenWorld
    };
  }

  /// <summary>
  /// <paramref name="derived"/> with every hint <paramref name="overrides"/> sets replaced by the override.
  /// </summary>
  /// <param name="derived">Annotations from the catalog.</param>
  /// <param name="overrides">Host overrides; a null member keeps the derived value.</param>
  /// <returns>The merged annotations.</returns>
  public static ToolAnnotations MergeAnnotations(ToolAnnotations derived, ToolAnnotations overrides)
  {
    ArgumentNullException.ThrowIfNull(derived);
    ArgumentNullException.ThrowIfNull(overrides);

    return new ToolAnnotations
    {
      Title = overrides.Title ?? derived.Title,
      ReadOnlyHint = overrides.ReadOnlyHint ?? derived.ReadOnlyHint,
      IdempotentHint = overrides.IdempotentHint ?? derived.IdempotentHint,
      DestructiveHint = overrides.DestructiveHint ?? derived.DestructiveHint,
      OpenWorldHint = overrides.OpenWorldHint ?? derived.OpenWorldHint
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
}
