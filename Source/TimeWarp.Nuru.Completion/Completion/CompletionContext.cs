namespace TimeWarp.Nuru.Completion;

using TimeWarp.Nuru; // EndpointCollection is in root namespace

/// <summary>
/// Represents the context for a completion request.
/// </summary>
/// <param name="Args">The arguments typed so far on the command line.</param>
/// <param name="CursorPosition">The index of the argument being completed (zero-based).</param>
/// <param name="Endpoints">The collection of all registered endpoints/routes.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
  "Performance",
  "CA1819:Properties should not return arrays",
  Justification = "Record type with array property is intentional for completion context")]
public record CompletionContext(
  string[] Args,
  int CursorPosition,
  EndpointCollection Endpoints
);
