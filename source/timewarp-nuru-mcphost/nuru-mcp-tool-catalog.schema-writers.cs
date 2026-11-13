#region Purpose
// JSON schema writers of NuruMcpToolCatalog: member, scalar and default rendering for tool input schemas.
#endregion

namespace TimeWarp.Nuru.McpHost;

public static partial class NuruMcpToolCatalog
{
  private static void WriteMemberSchema(
    Utf8JsonWriter writer,
    string catalogType,
    bool isArray,
    string? description,
    string? defaultValue,
    IReadOnlyList<string>? allowedValues,
    JsonElement? schema)
  {
    if (schema is not null || catalogType.StartsWith(JsonTypePrefix, StringComparison.Ordinal))
    {
      WriteJsonMemberSchema(writer, schema, description, defaultValue);
      return;
    }

    bool nullable = catalogType.EndsWith('?', StringComparison.Ordinal);
    bool typedArray = catalogType.EndsWith("[]", StringComparison.Ordinal);
    string itemType = typedArray ? catalogType[..^2] : catalogType;
    isArray = isArray || typedArray;
    string schemaType = GetJsonSchemaType(itemType);
    string? defaultJson = isArray ? null : DefaultJson(schemaType, defaultValue, allowedValues);

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
      if (defaultJson is not null)
      {
        writer.WritePropertyName("default");
        writer.WriteRawValue(defaultJson);
      }
    }

    WriteDescription(writer, description, defaultJson);
    writer.WriteEndObject();
  }

  /// <summary>
  /// A member that binds as one JSON value: the catalog's exported schema, or any object or array when
  /// the catalog carries none. The binder takes only an object or array (or null when the schema allows it).
  /// </summary>
  private static void WriteJsonMemberSchema(Utf8JsonWriter writer, JsonElement? schema, string? description, string? defaultValue)
  {
    writer.WriteStartObject();

    if (schema is { ValueKind: JsonValueKind.Object } exported)
    {
      foreach (JsonProperty property in exported.EnumerateObject())
      {
        // The member's own description replaces one the type schema may carry.
        if (property.NameEquals("description") && !string.IsNullOrEmpty(description))
          continue;
        if (property.NameEquals("default"))
          continue;
        property.WriteTo(writer);
      }
    }
    else
    {
      writer.WritePropertyName("type");
      writer.WriteStartArray();
      writer.WriteStringValue("object");
      writer.WriteStringValue("array");
      writer.WriteEndArray();
    }

    // A default is published only when the initializer is itself JSON (for example []).
    string? defaultJson = JsonDefault(defaultValue);
    if (defaultJson is not null)
    {
      writer.WritePropertyName("default");
      writer.WriteRawValue(defaultJson);
    }

    WriteDescription(writer, description, defaultJson);
    writer.WriteEndObject();
  }

  /// <summary>
  /// The member description, with <c>Default: X.</c> appended when the member has a default.
  /// </summary>
  private static void WriteDescription(Utf8JsonWriter writer, string? description, string? defaultJson)
  {
    string? text = DescribeDefault(description, defaultJson);
    if (!string.IsNullOrEmpty(text))
      writer.WriteString("description", text);
  }

  /// <summary>
  /// The description a harness shows the model: <paramref name="description"/> followed by
  /// <c>Default: X.</c>, where X is the default as JSON. Some harnesses (Codex) drop the
  /// <c>default</c> keyword, so the description carries it too.
  /// </summary>
  /// <param name="description">The member description, or null.</param>
  /// <param name="defaultJson">The default as JSON text, or null when the member has none.</param>
  /// <returns>The description, or null when both are empty.</returns>
  public static string? DescribeDefault(string? description, string? defaultJson)
  {
    string text = description?.Trim() ?? "";
    if (defaultJson is null)
      return text.Length == 0 ? null : text;

    string sentence = $"Default: {defaultJson}.";
    if (text.Length == 0)
      return sentence;

    return text[^1] is '.' or '!' or '?' ? $"{text} {sentence}" : $"{text}. {sentence}";
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

  /// <summary>
  /// The default as JSON text of <paramref name="schemaType"/>, or null when there is none or it does not convert.
  /// </summary>
  private static string? DefaultJson(string schemaType, string? defaultValue, IReadOnlyList<string>? allowedValues)
  {
    // The catalog carries the C# initializer text: "\"nearest\"", "1.5m", "Env.Dev", "null".
    if (defaultValue is null || defaultValue == "null")
      return null;

    string literal = defaultValue.Trim();
    ArrayBufferWriter<byte> buffer = new();
    using (Utf8JsonWriter writer = new(buffer))
    {
      switch (schemaType)
      {
        case "integer" when long.TryParse(literal.TrimEnd('L', 'l', 'U', 'u'), NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer):
          writer.WriteNumberValue(integer);
          break;
        case "number" when double.TryParse(literal.TrimEnd('M', 'm', 'F', 'f', 'D', 'd'), NumberStyles.Float, CultureInfo.InvariantCulture, out double number):
          writer.WriteNumberValue(number);
          break;
        case "boolean" when bool.TryParse(literal, out bool boolean):
          writer.WriteBooleanValue(boolean);
          break;
        case "string":
          writer.WriteStringValue(StringDefault(literal, allowedValues));
          break;
        default:
          return null;
      }
    }

    return Encoding.UTF8.GetString(buffer.WrittenSpan);
  }

  /// <summary>A JSON member's default when the catalog text is a JSON object or array, else null.</summary>
  private static string? JsonDefault(string? defaultValue)
  {
    if (string.IsNullOrWhiteSpace(defaultValue))
      return null;

    try
    {
      using JsonDocument document = JsonDocument.Parse(defaultValue);
      return document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array
        ? document.RootElement.GetRawText()
        : null;
    }
    catch (JsonException)
    {
      return null;
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
    int dot = literal.LastIndexOf('.', StringComparison.Ordinal);
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
