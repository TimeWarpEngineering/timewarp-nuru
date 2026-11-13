#region Purpose
// JSON Schema for a request member that binds as one JSON value, for the --capabilities catalog.
#endregion

#region Design
// The schema is exported from the JsonTypeInfo in the generated NuruJsonMemberContext, the same metadata
// the --json-args binder deserializes with, so names, required members (C# required, constructor
// parameters), nullable reference annotations and string-enum converters match what binds. The root is
// non-null unless the member is nullable: the binder rejects a JSON null for a non-nullable member.
// A [Description] on a nested property becomes its schema description. Generated capabilities code calls
// Create once per JSON member when the catalog is first built.
// The exporter cannot see what a custom converter writes and publishes that node with no type (an array of
// them gets no items). The generator passes the types whose own converter writes a string (a custom
// [JsonConverter] plus IParsable<TSelf>, such as typed IDs) as stringTypes, and the Guid-backed ones as
// uuidTypes. Those nodes publish {"type":"string"} (plus "format":"uuid"), and array items or dictionary
// values of those types are filled in. The generator finds them because this assembly is trim analyzed and
// cannot inspect interfaces by reflection. Only a node the exporter left untyped is rewritten. A nullable
// value type or a nullable property adds "null"; the member root takes nullable from the caller.
#endregion

namespace TimeWarp.Nuru;

using System.Buffers;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

/// <summary>
/// Builds the JSON Schema a catalog publishes for a member that binds as one JSON value.
/// </summary>
/// <remarks>Called by generated capabilities code. Apps do not call it.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NuruJsonMemberSchema
{
  private static readonly JsonSchemaExporterOptions ExporterOptions = new()
  {
    TreatNullObliviousAsNonNullable = true,
    TransformSchemaNode = AddDescription
  };

  private const string UuidFormat = "uuid";

  /// <summary>
  /// The JSON Schema of <paramref name="type"/> as <paramref name="context"/> deserializes it.
  /// </summary>
  /// <param name="context">The generated context the binder deserializes JSON members with.</param>
  /// <param name="type">The member type.</param>
  /// <param name="nullable">True when the member accepts a JSON <c>null</c>.</param>
  /// <param name="stringTypes">Types whose own converter writes a string, published as <c>{"type":"string"}</c>.</param>
  /// <param name="uuidTypes">The <paramref name="stringTypes"/> that wrap a Guid, published with <c>"format":"uuid"</c>.</param>
  /// <returns>The schema, or null when <paramref name="context"/> has no metadata for <paramref name="type"/>.</returns>
  public static JsonElement? Create(
    JsonSerializerContext context,
    Type type,
    bool nullable = false,
    IReadOnlyCollection<Type>? stringTypes = null,
    IReadOnlyCollection<Type>? uuidTypes = null)
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(type);

    if (context.GetTypeInfo(type) is not JsonTypeInfo typeInfo)
      return null;

    JsonSchemaExporterOptions options = ExporterOptions;
    if (stringTypes is { Count: > 0 })
    {
      StringTypes strings = new(stringTypes, uuidTypes ?? []);
      options = new JsonSchemaExporterOptions
      {
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = (exporterContext, node) => AddDescription(exporterContext, strings.Publish(exporterContext, node))
      };
    }

    JsonNode schema = typeInfo.GetJsonSchemaAsNode(options);
    if (nullable && schema is JsonObject root)
      AddNull(root);

    ArrayBufferWriter<byte> buffer = new();
    using (Utf8JsonWriter writer = new(buffer))
    {
      schema.WriteTo(writer);
    }

    using JsonDocument document = JsonDocument.Parse(buffer.WrittenMemory);
    return document.RootElement.Clone();
  }

  private static JsonNode AddDescription(JsonSchemaExporterContext context, JsonNode schema)
  {
    ICustomAttributeProvider? provider = context.PropertyInfo?.AttributeProvider;
    if (schema is JsonObject node
        && !node.ContainsKey("description")
        && provider?.GetCustomAttributes(typeof(DescriptionAttribute), inherit: true).FirstOrDefault() is DescriptionAttribute { Description.Length: > 0 } description)
    {
      node.Insert(0, "description", description.Description);
    }

    return schema;
  }

  private static JsonObject StringSchema(bool uuid, bool nullable)
  {
    JsonObject schema = new() { ["type"] = nullable ? new JsonArray("string", "null") : "string" };
    if (uuid)
      schema["format"] = UuidFormat;

    return schema;
  }

  /// <summary>
  /// Rewrites the untyped nodes the exporter writes for types whose own converter writes a string.
  /// </summary>
  private sealed class StringTypes(IReadOnlyCollection<Type> strings, IReadOnlyCollection<Type> uuids)
  {
    public JsonNode Publish(JsonSchemaExporterContext context, JsonNode schema)
    {
      Type type = context.TypeInfo.Type;
      Type? underlying = Nullable.GetUnderlyingType(type);
      Type target = underlying ?? type;
      if (strings.Contains(target) && IsUntyped(schema))
      {
        bool nullable = underlying is not null || (!type.IsValueType && context.PropertyInfo is { IsGetNullable: true });
        return StringSchema(uuids.Contains(target), nullable);
      }

      // The exporter omits items (or additionalProperties) whose schema is untyped.
      if (schema is JsonObject node && context.TypeInfo.ElementType is Type element)
      {
        Type? elementUnderlying = Nullable.GetUnderlyingType(element);
        Type elementTarget = elementUnderlying ?? element;
        string key = context.TypeInfo.Kind == JsonTypeInfoKind.Dictionary ? "additionalProperties" : "items";
        if (strings.Contains(elementTarget) && (!node.TryGetPropertyValue(key, out JsonNode? existing) || existing is null || IsUntyped(existing)))
          node[key] = StringSchema(uuids.Contains(elementTarget), elementUnderlying is not null);
      }

      return schema;
    }

    private static bool IsUntyped(JsonNode schema) =>
      schema.GetValueKind() == JsonValueKind.True
      || (schema is JsonObject node && !node.ContainsKey("type") && !node.ContainsKey("$ref") && !node.ContainsKey("enum") && !node.ContainsKey("anyOf") && !node.ContainsKey("oneOf"));
  }

  private static void AddNull(JsonObject schema)
  {
    switch (schema["type"])
    {
      case JsonValue value when value.TryGetValue(out string? single) && single != "null":
        schema["type"] = new JsonArray(single, "null");
        break;
      case JsonArray types when !types.Any(t => t?.GetValue<string>() == "null"):
        types.Add("null");
        break;
    }
  }
}
