#region Purpose
// The one mapping from a Nuru failure (a bind error, an escaped exception) to SharedProblemDetails, so an app's
// IResponseRenderer<SharedProblemDetails> shows those failures in the same format as its handlers' problems.
#endregion

#region Design
// Every bind failure is Type "Invalid" with Status 400; the NuruBindErrorKind and the key go into extension
// members, so a renderer keyed on Type sees one type for every input mistake. An escaped exception is Type
// "Faulted" with Status 500. Detail drops the CLI's "Error: " prefix: the renderer owns the presentation.
// McpHost and the daemon server always use the mapping when the app has a problem renderer (task 508-018);
// the CLI uses it only after UseProblemRendererForErrors().
#endregion

namespace TimeWarp.Nuru;

using TimeWarp.Foundation.Types;

/// <summary>
/// Builds the <see cref="SharedProblemDetails"/> Nuru renders through an app's
/// <c>IResponseRenderer&lt;SharedProblemDetails&gt;</c> for a bind failure or an escaped exception.
/// </summary>
public static class NuruProblems
{
  /// <summary>The problem type of every binding failure.</summary>
  public const string InvalidType = "Invalid";

  /// <summary>The status of a binding failure (bad request).</summary>
  public const int InvalidStatus = 400;

  /// <summary>The problem type of an exception that escaped a handler.</summary>
  public const string FaultedType = "Faulted";

  /// <summary>The status of an exception that escaped a handler.</summary>
  public const int FaultedStatus = 500;

  /// <summary>The extension member that holds the <see cref="NuruBindErrorKind"/> name.</summary>
  public const string KindExtension = "kind";

  /// <summary>The extension member that holds the parameter or option name the failure is about.</summary>
  public const string KeyExtension = "key";

  private const string ErrorPrefix = "Error: ";

  /// <summary>
  /// The problem for a binding failure from <see cref="NuruApp.TryBind"/>.
  /// </summary>
  /// <param name="error">The binding failure.</param>
  /// <returns>
  /// Type <see cref="InvalidType"/>, Status <see cref="InvalidStatus"/>, Detail the message without its
  /// <c>Error: </c> prefix, and the <see cref="KindExtension"/> and <see cref="KeyExtension"/> extension members.
  /// </returns>
  public static SharedProblemDetails FromBindError(NuruBindError error)
  {
    ArgumentNullException.ThrowIfNull(error);
    return Invalid(error.Message, error.Kind, error.Key);
  }

  /// <summary>
  /// The problem for an input error: a binding failure, or a <c>--json-args</c> or argv value the app rejects.
  /// </summary>
  /// <param name="message">The CLI's message for the error.</param>
  /// <param name="kind">Why binding failed, or null when no <see cref="NuruBindErrorKind"/> applies.</param>
  /// <param name="key">The parameter or option name the error is about, or null.</param>
  /// <returns>Type <see cref="InvalidType"/> and Status <see cref="InvalidStatus"/>.</returns>
  public static SharedProblemDetails Invalid(string message, NuruBindErrorKind? kind = null, string? key = null)
  {
    ArgumentNullException.ThrowIfNull(message);
    SharedProblemDetails problem = new()
    {
      Type = InvalidType,
      Title = "Invalid arguments",
      Status = InvalidStatus,
      Detail = WithoutErrorPrefix(message)
    };
    if (kind is NuruBindErrorKind value)
      problem.Extensions[KindExtension] = value.ToString();
    if (key is not null)
      problem.Extensions[KeyExtension] = key;
    return problem;
  }

  /// <summary>
  /// The problem for an exception that escaped a handler.
  /// </summary>
  /// <param name="exception">The exception.</param>
  /// <returns>Type <see cref="FaultedType"/>, Status <see cref="FaultedStatus"/>, Detail the exception message.</returns>
  public static SharedProblemDetails FromException(Exception exception)
  {
    ArgumentNullException.ThrowIfNull(exception);
    return new SharedProblemDetails
    {
      Type = FaultedType,
      Title = "Unhandled error",
      Status = FaultedStatus,
      Detail = exception.Message
    };
  }

  private static string WithoutErrorPrefix(string message)
  {
    message = message.TrimEnd('\r', '\n');
    return message.StartsWith(ErrorPrefix, StringComparison.Ordinal) ? message[ErrorPrefix.Length..] : message;
  }
}
