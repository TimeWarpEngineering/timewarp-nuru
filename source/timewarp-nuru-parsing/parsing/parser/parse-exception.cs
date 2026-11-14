#region Purpose
// Exception thrown when parsing fails and cannot continue.
#endregion

#region Design
// Control flow inside the pattern parser: segment parsing throws it and the parser catches it to
// resynchronize and keep reading, so one bad segment does not hide later errors. It is sealed and
// has a [JsonConstructor] constructor because NuruJsonSerializerContext registers it for AOT-safe
// serialization.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Exception thrown when parsing fails and cannot continue.
/// </summary>
public sealed class ParseException : Exception
{
  /// <summary>
  /// Initializes a new instance of the <see cref="ParseException"/> class.
  /// </summary>
  public ParseException() { }
  /// <summary>
  /// Initializes a new instance of the <see cref="ParseException"/> class with a specified error message.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  public ParseException(string message) : base(message) { }
  /// <summary>
  /// Initializes a new instance of the <see cref="ParseException"/> class with a specified error message
  /// and a reference to the inner exception that is the cause of this exception.
  /// </summary>
  /// <param name="message">The error message that explains the reason for the exception.</param>
  /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
  [JsonConstructor]
  public ParseException(string? message, Exception? innerException) : base(message, innerException) { }
}
