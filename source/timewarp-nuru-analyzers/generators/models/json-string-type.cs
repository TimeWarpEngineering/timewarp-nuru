#region Purpose
// A type inside a JSON member whose own converter writes a string, so its schema node publishes as a string.
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// A type inside a JSON member that the catalog schema publishes as <c>{"type":"string"}</c>.
/// </summary>
/// <remarks>
/// <c>JsonSchemaExporter</c> cannot see what a custom converter writes, so it publishes such a node with
/// no type. A type with its own <c>[JsonConverter]</c> that implements <c>IParsable&lt;TSelf&gt;</c>
/// (typed IDs) is written as a string, the same form Nuru parses for a parsable type.
/// </remarks>
/// <param name="TypeName">Fully qualified type name with <c>global::</c>, without a nullable <c>?</c>.</param>
/// <param name="IsUuid">True when the type wraps a Guid (a public <c>Guid Value</c> property), so the schema adds <c>"format":"uuid"</c>.</param>
public sealed record JsonStringType(string TypeName, bool IsUuid);
