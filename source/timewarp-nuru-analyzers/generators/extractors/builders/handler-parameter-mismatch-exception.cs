namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Signals a recoverable handler-parameter / route-segment mismatch at <c>.Done()</c>.
/// Caught by the DSL interpreter to emit NURU_H005 without dropping sibling routes.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Assembly-internal signal type with a single structured throw site")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Only consumed within the analyzers assembly")]
internal sealed class HandlerParameterMismatchException : Exception
{
  public string ParameterName { get; }
  public string ParameterTypeName { get; }
  public string AvailableSegments { get; }

  public HandlerParameterMismatchException(
    string parameterName,
    string parameterTypeName,
    string availableSegments)
    : base($"Handler parameter '{parameterName}' ({parameterTypeName}) does not match any segment in route [{availableSegments}]")
  {
    ParameterName = parameterName;
    ParameterTypeName = parameterTypeName;
    AvailableSegments = availableSegments;
  }
}