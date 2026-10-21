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

  /// <inheritdoc />
  public override string ToString() => Message;
}
