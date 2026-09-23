namespace TimeWarp.Nuru.Search.Services;

/// <summary>
/// Source-generated JSON context for the <c>endpoints.endpoint_json</c> column. The search tool
/// is AOT-compatible, so reflection-based <see cref="JsonSerializer"/> overloads must not be used.
/// Default (PascalCase, unindented) naming is deliberate: it matches what the earlier
/// reflection-based serializer wrote, so rows in an existing <c>~/.nuru/index.db</c> stay readable.
/// </summary>
[JsonSerializable(typeof(EndpointCapability))]
internal sealed partial class SearchIndexJsonContext : JsonSerializerContext;
