namespace TimeWarp.Nuru.Completion;

using System;
using System.Collections.Generic;
using System.Linq;
using TimeWarp.Nuru;
using TimeWarp.Nuru.Parsing;

/// <summary>
/// Provides default completions based on registered routes and their patterns.
/// This source analyzes the endpoint collection to suggest commands, options, and parameter types.
/// </summary>
public sealed class DefaultCompletionSource : ICompletionSource
{
  /// <summary>
  /// Gets completions by analyzing registered routes.
  /// </summary>
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    // Get the word being completed (or empty if completing next word)
    string currentWord = context.CursorPosition < context.Args.Length
      ? context.Args[context.CursorPosition]
      : string.Empty;

    // Determine what we're completing based on position and current input
    if (context.CursorPosition == 0)
    {
      // First word - shouldn't happen in normal completion flow (it's the app name)
      return [];
    }

    // Check if we're completing an option (starts with -)
    if (currentWord.StartsWith('-'))
    {
      return GetOptionCompletions(context);
    }

    // Get command/subcommand completions based on what's been typed so far
    return GetCommandCompletions(context);
  }

  private static IEnumerable<CompletionCandidate> GetOptionCompletions(CompletionContext context)
  {
    // Extract all option matchers from routes
    var options = new HashSet<string>(StringComparer.Ordinal);

    foreach (Endpoint endpoint in context.Endpoints)
    {
      foreach (OptionMatcher optionMatcher in endpoint.CompiledRoute.OptionMatchers)
      {
        // Add the primary option form (already includes -- or -)
        options.Add(optionMatcher.MatchPattern);

        // Add alternate form if available
        if (optionMatcher.AlternateForm is not null)
        {
          options.Add(optionMatcher.AlternateForm);
        }
      }
    }

    return options
      .OrderBy(opt => opt, StringComparer.Ordinal)
      .Select(opt => new CompletionCandidate(
        Value: opt,
        Description: null,
        Type: CompletionType.Option
      ));
  }

  private static IEnumerable<CompletionCandidate> GetCommandCompletions(CompletionContext context)
  {
    // Get the word being completed (or empty if completing next word)
    string currentWord = context.CursorPosition < context.Args.Length
      ? context.Args[context.CursorPosition]
      : string.Empty;

    // Get the words fully typed so far (excluding the app name at index 0 and the partial word)
    string[] completedWords = [.. context.Args.Skip(1).Take(context.CursorPosition - 1)];

    // Find routes that match the completed prefix
    var commands = new HashSet<string>(StringComparer.Ordinal);

    foreach (Endpoint endpoint in context.Endpoints)
    {
      // Extract literal matchers from the positional matchers
      var literalSegments = new List<string>();

      foreach (RouteMatcher matcher in endpoint.CompiledRoute.PositionalMatchers)
      {
        if (matcher is LiteralMatcher literal)
        {
          literalSegments.Add(literal.Value);
        }
        else
        {
          // Stop at first parameter - we only want command literals
          break;
        }
      }

      if (literalSegments.Count == 0)
      {
        continue; // No literal segments to suggest
      }

      // Check if this route matches all completed words exactly
      bool matches = true;
      for (int i = 0; i < completedWords.Length && i < literalSegments.Count; i++)
      {
        if (!string.Equals(completedWords[i], literalSegments[i], StringComparison.Ordinal))
        {
          matches = false;
          break;
        }
      }

      // If matches so far, suggest the next literal segment (if it matches the partial word)
      if (matches && literalSegments.Count > completedWords.Length)
      {
        string nextSegment = literalSegments[completedWords.Length];

        // Only include if it starts with the current partial word (case-sensitive)
        if (string.IsNullOrEmpty(currentWord) || nextSegment.StartsWith(currentWord, StringComparison.Ordinal))
        {
          commands.Add(nextSegment);
        }
      }
    }

    return commands
      .OrderBy(cmd => cmd, StringComparer.Ordinal)
      .Select(cmd => new CompletionCandidate(
        Value: cmd,
        Description: null,
        Type: CompletionType.Command
      ));
  }
}
