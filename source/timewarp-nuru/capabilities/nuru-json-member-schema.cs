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

  /// <summary>
  /// The JSON Schema of <paramref name="type"/> as <paramref name="context"/> deserializes it.
  /// </summary>
  /// <param name="context">The generated context the binder deserializes JSON members with.</param>
  /// <param name="type">The member type.</param>
  /// <param name="nullable">True when the member accepts a JSON <c>null</c>.</param>
  /// <returns>The schema, or null when <paramref name="context"/> has no metadata for <paramref name="type"/>.</returns>
  public static JsonElement? Create(JsonSerializerContext context, Type type, bool nullable = false)
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(type);

    if (context.GetTypeInfo(type) is not JsonTypeInfo typeInfo)
      return null;

    JsonNode schema = typeInfo.GetJsonSchemaAsNode(ExporterOptions);
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
