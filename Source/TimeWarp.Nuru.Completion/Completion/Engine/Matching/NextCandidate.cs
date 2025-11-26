namespace TimeWarp.Nuru.Completion;

/// <summary>
/// Represents what can come next at a particular position in a route.
/// </summary>
/// <param name="Kind">Type of candidate (Literal, Parameter, Option).</param>
/// <param name="Value">The value to complete (e.g., "status", "--verbose").</param>
/// <param name="AlternateValue">Alternate form if applicable (e.g., "-v" for "--verbose").</param>
/// <param name="Description">Description for UI display.</param>
/// <param name="ParameterType">For parameters, the expected type constraint.</param>
/// <param name="IsRequired">True if this is required to complete the route.</param>
public record NextCandidate(
  CandidateKind Kind,
  string Value,
  string? AlternateValue,
  string? Description,
  string? ParameterType,
  bool IsRequired
);

/// <summary>
/// Kind of completion candidate.
/// </summary>
public enum CandidateKind
{
  /// <summary>Command or subcommand literal.</summary>
  Literal,

  /// <summary>Required or optional parameter.</summary>
  Parameter,

  /// <summary>Long or short form option.</summary>
  Option
}
