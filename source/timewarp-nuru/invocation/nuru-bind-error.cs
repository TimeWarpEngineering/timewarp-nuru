#region Purpose
// Error kinds and details reported when TryBind cannot bind a JSON object to a route.
#endregion

#region Design
// Bind failures are values, not exceptions: TryBind is a try-pattern so JSON-driven callers
// (daemon, MCP) get a machine-readable kind and key instead of parsing exception text. Message is
// already the user-facing "Error: ..." line, so the same text is printed in argv mode and
// returned in JSON mode.
// IsJsonMemberFailure and ForJsonMember are public for generated binders: they classify the
// failures System.Text.Json and converters raise per member, and strip the serializer's trailing
// Path/LineNumber noise from the message.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Why <see cref="NuruApp.TryBind"/> could not bind a JSON object to a route.
/// </summary>
public enum NuruBindErrorKind
{
  /// <summary>No route has the given pattern.</summary>
  UnknownRoute,

  /// <summary>The arguments are not a JSON object.</summary>
  NotAnObject,

  /// <summary>A key is not a parameter or option name of the route.</summary>
  UnknownKey,

  /// <summary>A required parameter or option has no key.</summary>
  MissingValue,

  /// <summary>A key's JSON value has the wrong kind (string, number, bool, array, null) or is out of range.</summary>
  WrongType,

  /// <summary>A value has the right JSON kind but does not convert to the member's type.</summary>
  InvalidValue,

  /// <summary>An argument is left over after the route's segments. Only <c>--json-args</c> with argv can produce it.</summary>
  UnexpectedArgument
}

/// <summary>
/// A binding failure from <see cref="NuruApp.TryBind"/>.
/// </summary>
/// <remarks>
/// <see cref="Message"/> is the text <c>--json-args</c> prints to stderr for the same input.
/// </remarks>
public sealed class NuruBindError
{
  /// <summary>
  /// Creates a binding failure. Called by generated code.
  /// </summary>
  /// <param name="kind">Why binding failed.</param>
  /// <param name="key">The JSON key the failure is about, or null.</param>
  /// <param name="message">The message <c>--json-args</c> prints.</param>
  public NuruBindError(NuruBindErrorKind kind, string? key, string message)
  {
    Kind = kind;
    Key = key;
    Message = message;
  }

  /// <summary>Why binding failed.</summary>
  public NuruBindErrorKind Kind { get; }

  /// <summary>The JSON key the failure is about, or null when it is about the whole request.</summary>
  public string? Key { get; }

  /// <summary>The message <c>--json-args</c> prints for the same input.</summary>
  public string Message { get; }

  /// <summary>
  /// Whether an exception thrown while deserializing a JSON member means the value does not bind.
  /// Called by generated code.
  /// </summary>
  /// <remarks>
  /// System.Text.Json throws <see cref="System.Text.Json.JsonException"/> for a value it cannot convert,
  /// <see cref="NotSupportedException"/> or <see cref="InvalidOperationException"/> for a shape it cannot
  /// bind, and a converter that parses text (a typed ID) may throw <see cref="FormatException"/>.
  /// </remarks>
  /// <param name="exception">The exception the serializer threw.</param>
  /// <returns>True when the exception is a bind failure for the member.</returns>
  public static bool IsJsonMemberFailure(Exception exception) =>
    exception is System.Text.Json.JsonException
      or NotSupportedException
      or InvalidOperationException
      or FormatException;

  /// <summary>
  /// Creates the <see cref="NuruBindErrorKind.InvalidValue"/> failure for a JSON member whose value did
  /// not deserialize. Called by generated code.
  /// </summary>
  /// <remarks>
  /// The message names the member and, for a <see cref="System.Text.Json.JsonException"/>, the JSON path
  /// inside the member, for example <c>operations: $[0].tool</c>. It carries no stack trace.
  /// </remarks>
  /// <param name="key">The JSON key of the member.</param>
  /// <param name="pattern">The route pattern.</param>
  /// <param name="exception">The exception the serializer threw.</param>
  /// <returns>The failure to report.</returns>
  public static NuruBindError ForJsonMember(string key, string pattern, Exception exception)
  {
    ArgumentNullException.ThrowIfNull(key);
    ArgumentNullException.ThrowIfNull(pattern);
    ArgumentNullException.ThrowIfNull(exception);

    string detail = exception.Message;
    string location = key;
    if (exception is System.Text.Json.JsonException { Path: { Length: > 0 } path })
    {
      location = $"{key}: {path}";

      // The serializer appends " Path: ... | LineNumber: ... | BytePositionInLine: ..." to its message.
      int pathIndex = detail.IndexOf(" Path: ", StringComparison.Ordinal);
      if (pathIndex >= 0)
        detail = detail[..pathIndex];
    }

    return new NuruBindError(
      NuruBindErrorKind.InvalidValue,
      key,
      $"Error: Invalid value for '{key}' on '{pattern}'. {location}: {detail}");
  }

  /// <inheritdoc />
  public override string ToString() => Message;
}
