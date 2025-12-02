namespace TimeWarp.Nuru;

/// <summary>
/// AOT-safe JSON converter for exceptions that avoids reflection-based properties.
/// Specifically handles Exception.TargetSite which requires reflection and is incompatible with AOT.
/// </summary>
public class SafeExceptionConverter : JsonConverter<Exception>
{
  /// <summary>
  /// Exception deserialization is not supported.
  /// </summary>
  public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    => throw new NotSupportedException("Exception deserialization is not supported");

  /// <summary>
  /// Serializes exception to JSON, excluding reflection-based properties like TargetSite.
  /// Includes special handling for PatternException to preserve its diagnostic properties.
  /// </summary>
  [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
      Justification = "Using source-generated context for ParseError and SemanticError types which are known at compile time")]
  [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
      Justification = "ParseError and SemanticError are registered in NuruJsonSerializerContext")]
  public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
  {
    ArgumentNullException.ThrowIfNull(writer);
    ArgumentNullException.ThrowIfNull(value);

    writer.WriteStartObject();

    // Write basic exception information
    writer.WriteString("type", value.GetType().Name);
    writer.WriteString("message", value.Message);

    if (value.StackTrace is not null)
    {
      writer.WriteString("stackTrace", value.StackTrace);
    }

    // Handle inner exceptions recursively
    if (value.InnerException is not null)
    {
      writer.WritePropertyName("innerException");
      Write(writer, value.InnerException, options);
    }

    // Handle PatternException custom properties (important for diagnostics)
    if (value is PatternException patternEx)
    {
      writer.WriteString("routePattern", patternEx.RoutePattern);

      if (patternEx.ParseErrors?.Count > 0)
      {
        writer.WritePropertyName("parseErrors");
        JsonSerializer.Serialize(writer, patternEx.ParseErrors, NuruJsonSerializerContext.Default.IReadOnlyListParseError);
      }

      if (patternEx.SemanticErrors?.Count > 0)
      {
        writer.WritePropertyName("semanticErrors");
        JsonSerializer.Serialize(writer, patternEx.SemanticErrors, NuruJsonSerializerContext.Default.IReadOnlyListSemanticError);
      }
    }

    // Deliberately skip these properties:
    // - TargetSite: Requires reflection (MethodBase), incompatible with AOT
    // - HResult: Internal error code, rarely useful for CLI apps
    // - Data: IDictionary can contain arbitrary types
    // - HelpLink: Rarely used in CLI context
    // - Source: Rarely useful for end users

    writer.WriteEndObject();
  }
}
