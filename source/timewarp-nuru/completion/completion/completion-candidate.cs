namespace TimeWarp.Nuru;

/// <summary>
/// Represents a single completion candidate.
/// </summary>
/// <param name="Value">The value to complete to (e.g., "deploy", "--force", "production").</param>
/// <param name="Description">Optional description to display in completion menu.</param>
/// <param name="Type">
/// The type of completion (Command, Option, Parameter, etc.).
/// <see cref="CompletionType.File"/> and <see cref="CompletionType.Directory"/> candidates enable the
/// shell's own path completion; an empty <paramref name="Value"/> makes the candidate a pure delegation marker.
/// </param>
/// <param name="ParameterType">For parameters, the type constraint name (e.g., "int", "environment").</param>
/// <param name="Directive">
/// Directive flags this candidate requests (e.g., <see cref="CompletionDirective.NoSpace"/>,
/// <see cref="CompletionDirective.KeepOrder"/>). Flags from all candidates are combined into the
/// directive line the shell script reads.
/// </param>
public record CompletionCandidate(
  string Value,
  string? Description,
  CompletionType Type,
  string? ParameterType = null,
  CompletionDirective Directive = CompletionDirective.None
);
