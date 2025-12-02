namespace TimeWarp.Nuru;

/// <summary>
/// Represents a single completion candidate.
/// </summary>
/// <param name="Value">The value to complete to (e.g., "deploy", "--force", "production").</param>
/// <param name="Description">Optional description to display in completion menu.</param>
/// <param name="Type">The type of completion (Command, Option, Parameter, etc.).</param>
/// <param name="ParameterType">For parameters, the type constraint name (e.g., "int", "environment").</param>
public record CompletionCandidate(
  string Value,
  string? Description,
  CompletionType Type,
  string? ParameterType = null
);
