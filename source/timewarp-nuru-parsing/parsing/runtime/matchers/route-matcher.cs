#region Purpose
// Base class for route matchers (literals, parameters, and so on).
#endregion

#region Design
// The runtime matchers are the data model of a CompiledRoute. Their matching rules mirror the
// source-generated matcher path, which is what routes run through at runtime.
#endregion

#region Open Questions
// Q1 (2026-11-14, claude): No code outside the matcher classes calls RouteMatcher.TryMatch in
//     source/, tests/ or samples/ except parser-16 on OptionMatcher. Is TryMatch intended public
//     API for external consumers of CompiledRoute, or a leftover from before generator-emitted
//     matching?
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Base class for all route matchers (literals, parameters, etc.)
/// </summary>
public abstract class RouteMatcher
{
  /// <summary>
  /// Attempts to match this segment against an argument.
  /// </summary>
  /// <param name="arg">The argument to match against.</param>
  /// <param name="extractedValue">If this is a parameter segment, the extracted value; otherwise null.</param>
  /// <returns>True if the segment matches the argument; otherwise false.</returns>
  public abstract bool TryMatch(string arg, out string? extractedValue);
  /// <summary>
  /// Gets the display representation of this segment for debugging.
  /// </summary>
  public abstract string ToDisplayString();
}
