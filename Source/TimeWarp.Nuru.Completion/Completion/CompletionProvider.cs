namespace TimeWarp.Nuru.Completion;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru; // Endpoint, EndpointCollection, ITypeConverterRegistry, IRouteTypeConverter
using TimeWarp.Nuru.Parsing; // CompiledRoute, RouteMatcher, LiteralMatcher, ParameterMatcher, OptionMatcher

/// <summary>
/// Provides completion candidates by analyzing route patterns and current input.
/// </summary>
public class CompletionProvider
{
  private readonly ILoggerFactory? LoggerFactory;
  private readonly ITypeConverterRegistry TypeConverterRegistry;
  public CompletionProvider(ITypeConverterRegistry typeConverterRegistry, ILoggerFactory? loggerFactory = null)
  {
    TypeConverterRegistry = typeConverterRegistry;
    LoggerFactory = loggerFactory;
  }

  /// <summary>
  /// Get completion candidates for the given context.
  /// </summary>
  public ReadOnlyCollection<CompletionCandidate> GetCompletions
  (
    CompletionContext context,
    EndpointCollection endpoints
  )
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(endpoints);

    var candidates = new List<CompletionCandidate>();

    // If we're completing the first command (0 or 1 args), use command completions
    if (context.Args.Length <= 1)
    {
      string partialCommand = context.Args.ElementAtOrDefault(0) ?? "";

      // If there's a trailing space and we have exactly one arg, the user has finished
      // typing a command. Check if the command exactly matches any registered command.
      // If so, move to argument/option completion instead of re-suggesting the same command.
      if (context.HasTrailingSpace && context.Args.Length == 1)
      {
        bool isCompleteCommand = IsExactCommandMatch(endpoints, partialCommand);
        if (isCompleteCommand)
        {
          // Move on to argument/subcommand completions for this command
          return GetCompletionsAfterCommand(endpoints, partialCommand);
        }
      }

      candidates.AddRange(GetCommandCompletions(endpoints, partialCommand));
      return [.. candidates];
    }

    // Get all possible routes and try to match against current args
    foreach (Endpoint endpoint in endpoints.Endpoints)
    {
      CompiledRoute route = endpoint.CompiledRoute;

      // Try to determine what kind of completion is expected at cursor position
      IEnumerable<CompletionCandidate> routeCandidates = GetCompletionsForRoute(route, context.Args, context.CursorPosition);
      candidates.AddRange(routeCandidates);
    }

    // Remove duplicates and sort by type priority, then alphabetically
    // Priority: Command/Enum/Parameter first, Options last
    return
    [
      .. candidates
      .GroupBy(c => c.Value)
      .Select(g => g.First())
      .OrderBy(c => GetTypeSortOrder(c.Type))
      .ThenBy(c => c.Value)
    ];
  }

  /// <summary>
  /// Get sort order for completion types.
  /// Commands and Enum values come first, Options come last.
  /// </summary>
  private static int GetTypeSortOrder(CompletionType type)
  {
    return type switch
    {
      CompletionType.Command => 0,
      CompletionType.Enum => 1,
      CompletionType.Parameter => 2,
      CompletionType.File => 3,
      CompletionType.Directory => 4,
      CompletionType.Custom => 5,
      CompletionType.Option => 6, // Options come last
      _ => 99
    };
  }

  /// <summary>
  /// Get command literal completions (first segment matching).
  /// </summary>
  private static IEnumerable<CompletionCandidate> GetCommandCompletions(
    EndpointCollection endpoints,
    string partialCommand)
  {
    var commands = new HashSet<string>();

    foreach (Endpoint endpoint in endpoints.Endpoints)
    {
      CompiledRoute route = endpoint.CompiledRoute;

      // Get first literal segment
      RouteMatcher? firstSegment = route.Segments.Count > 0 ? route.Segments[0] : null;
      if (firstSegment is LiteralMatcher literal)
      {
        // Match if partial command is empty or literal starts with partial
        if (string.IsNullOrEmpty(partialCommand) ||
            literal.Value.StartsWith(partialCommand, StringComparison.OrdinalIgnoreCase))
        {
          commands.Add(literal.Value);
        }
      }
    }

    return commands.Select(cmd => new CompletionCandidate(
      cmd,
      Description: null,
      CompletionType.Command
    ));
  }

  /// <summary>
  /// Check if the given command exactly matches a registered command.
  /// </summary>
  private static bool IsExactCommandMatch(EndpointCollection endpoints, string command)
  {
    foreach (Endpoint endpoint in endpoints.Endpoints)
    {
      CompiledRoute route = endpoint.CompiledRoute;

      // Get first literal segment
      RouteMatcher? firstSegment = route.Segments.Count > 0 ? route.Segments[0] : null;
      if (firstSegment is LiteralMatcher literal)
      {
        if (string.Equals(literal.Value, command, StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }
    }

    return false;
  }

  /// <summary>
  /// Get completions after a complete command (subcommands, parameters, options).
  /// </summary>
  private ReadOnlyCollection<CompletionCandidate> GetCompletionsAfterCommand(
    EndpointCollection endpoints,
    string command)
  {
    var candidates = new List<CompletionCandidate>();

    foreach (Endpoint endpoint in endpoints.Endpoints)
    {
      CompiledRoute route = endpoint.CompiledRoute;

      // Check if first segment matches command
      RouteMatcher? firstSegment = route.Segments.Count > 0 ? route.Segments[0] : null;
      if (firstSegment is not LiteralMatcher literal ||
          !string.Equals(literal.Value, command, StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      // Command matches - what comes next?
      if (route.Segments.Count > 1)
      {
        RouteMatcher secondSegment = route.Segments[1];

        if (secondSegment is LiteralMatcher subcommand)
        {
          // Next segment is a literal (subcommand or option)
          // Don't suggest options from this route yet - user needs to complete the subcommand first
          CompletionType completionType = subcommand.Value.StartsWith('-')
            ? CompletionType.Option
            : CompletionType.Command;

          candidates.Add(new CompletionCandidate(
            subcommand.Value,
            Description: null,
            completionType
          ));
        }
        else if (secondSegment is ParameterMatcher parameter)
        {
          // Next segment is a parameter - user can provide parameter value OR options
          // Provide type hints for the parameter
          candidates.AddRange(GetParameterCompletions(parameter));

          // Also suggest options for this route (since parameters can be followed by options)
          foreach (OptionMatcher option in route.OptionMatchers)
          {
            candidates.Add(new CompletionCandidate(
              option.MatchPattern,
              option.Description,
              CompletionType.Option
            ));

            if (!string.IsNullOrEmpty(option.AlternateForm))
            {
              candidates.Add(new CompletionCandidate(
                option.AlternateForm,
                option.Description,
                CompletionType.Option
              ));
            }
          }
        }
      }
      else
      {
        // No more segments after command - suggest options for this route
        foreach (OptionMatcher option in route.OptionMatchers)
        {
          candidates.Add(new CompletionCandidate(
            option.MatchPattern,
            option.Description,
            CompletionType.Option
          ));

          if (!string.IsNullOrEmpty(option.AlternateForm))
          {
            candidates.Add(new CompletionCandidate(
              option.AlternateForm,
              option.Description,
              CompletionType.Option
            ));
          }
        }
      }
    }

    // Remove duplicates and sort by type priority, then alphabetically
    return
    [
      .. candidates
      .GroupBy(c => c.Value)
      .Select(g => g.First())
      .OrderBy(c => GetTypeSortOrder(c.Type))
      .ThenBy(c => c.Value)
    ];
  }

  /// <summary>
  /// Get completions for a specific route at the given cursor position.
  /// </summary>
  private List<CompletionCandidate> GetCompletionsForRoute
  (
    CompiledRoute route,
    string[] args,
    int cursorPosition
  )
  {
    var candidates = new List<CompletionCandidate>();

    // Track which segments have been consumed
    int segmentIndex = 0;
    int argIndex = 0;

    // First, check if we can match the route up to cursor position
    while (argIndex < cursorPosition && segmentIndex < route.Segments.Count)
    {
      RouteMatcher segment = route.Segments[segmentIndex];

      if (segment is OptionMatcher)
      {
        // Options can appear anywhere, skip for now
        segmentIndex++;
        continue;
      }

      if (segment is LiteralMatcher literal)
      {
        // For completion, we need partial matching when at cursor position
        if (argIndex >= args.Length)
        {
          // We're at the end of args, this route could match
          break;
        }

        // If we're at cursor position, allow partial match
        if (argIndex == cursorPosition - 1)
        {
          // Check if current arg starts with the literal
          if (!args[argIndex].StartsWith(literal.Value, StringComparison.OrdinalIgnoreCase))
          {
            return []; // Route doesn't match partial input
          }
        }
        else
        {
          // For non-cursor positions, require exact match
          if (!literal.TryMatch(args[argIndex], out _))
          {
            return []; // Route doesn't match
          }
        }

        // If we're at the cursor position, allow partial match
        if (argIndex == cursorPosition - 1)
        {
          // Check if current arg starts with the literal
          if (!args[argIndex].StartsWith(literal.Value, StringComparison.OrdinalIgnoreCase))
          {
            return []; // Route doesn't match partial input
          }
        }
        else
        {
          // For non-cursor positions, require exact match
          if (!literal.TryMatch(args[argIndex], out _))
          {
            return []; // Route doesn't match
          }
        }

        argIndex++;
        segmentIndex++;
      }
      else if (segment is ParameterMatcher parameter)
      {
        // If we're at the last arg before cursor and there's no trailing space,
        // this parameter is being completed - don't consume it, break to provide completions
        if (argIndex == args.Length - 1 && argIndex == cursorPosition - 1)
        {
          // User is typing a partial value for this parameter
          break;
        }

        // Parameter consumes one arg
        if (argIndex < args.Length)
        {
          argIndex++;
        }

        segmentIndex++;
      }
    }

// Now we're at cursor position - what can complete here?

// Get current word being completed
    string currentWord = cursorPosition < args.Length ? args[cursorPosition] :
                          (cursorPosition > 0 && args.Length > cursorPosition ? args[cursorPosition - 1] : "");

    // Only offer options if current word starts with option indicator
    if (currentWord.StartsWith('-'))
    {
      candidates.AddRange(GetOptionCompletions(route, args, currentWord));
    }

    // If we have a remaining segment, offer completions for it
    if (segmentIndex < route.Segments.Count)
    {
      RouteMatcher segment = route.Segments[segmentIndex];
      // Get the partial word being completed (if user is typing a word)
      // When argIndex < args.Length, use args[argIndex] as it's the partial word being typed
      // Otherwise, user wants completions for a new word (no partial)
      string partial = argIndex < args.Length ? args[argIndex] : "";

      if (segment is LiteralMatcher literal)
      {
        if (string.IsNullOrEmpty(partial) ||
            literal.Value.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
        {
          candidates.Add(new CompletionCandidate(
            literal.Value,
            Description: null,
            CompletionType.Command
          ));
        }
      }
      else if (segment is ParameterMatcher parameter)
      {
        candidates.AddRange(GetParameterCompletions(parameter, partial));
      }
    }

    return candidates;
  }

  /// <summary>
  /// Get option completions (--long and -short forms).
  /// </summary>
  private static IEnumerable<CompletionCandidate> GetOptionCompletions
  (
    CompiledRoute route,
    string[] args,
    string currentWord
  )
  {
    var usedOptions = new HashSet<string>(args);

    foreach (OptionMatcher option in route.OptionMatchers)
    {
      // Only suggest if not already used (unless repeatable) AND matches current word
      bool matchesCurrentWord = string.IsNullOrEmpty(currentWord) ||
        option.MatchPattern.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase) ||
        (!string.IsNullOrEmpty(option.AlternateForm) && option.AlternateForm.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase));

      if ((!usedOptions.Contains(option.MatchPattern) || option.IsRepeated) && matchesCurrentWord)
      {
        yield return new CompletionCandidate(
          option.MatchPattern,
          option.Description,
          CompletionType.Option
        );

        // Also suggest alternate form if present and matches current word
        if (!string.IsNullOrEmpty(option.AlternateForm) &&
            (!usedOptions.Contains(option.AlternateForm) || option.IsRepeated) &&
            option.AlternateForm.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
        {
          yield return new CompletionCandidate(
            option.AlternateForm,
            option.Description,
            CompletionType.Option
          );
        }
      }
    }
  }

  /// <summary>
  /// Get parameter completions based on type constraint.
  /// </summary>
  /// <param name="parameter">The parameter matcher containing constraint information.</param>
  /// <param name="partialWord">The partial word typed by the user for filtering completions.</param>
  private IEnumerable<CompletionCandidate> GetParameterCompletions(
    ParameterMatcher parameter,
    string partialWord = "")
  {
    if (string.IsNullOrEmpty(parameter.Constraint))
    {
      yield break;
    }

    // Get the target type for this constraint
    Type? targetType = GetTypeForConstraint(parameter.Constraint);

    if (targetType is null)
    {
      yield break;
    }

    // Handle FileInfo/DirectoryInfo - delegate to shell
    if (targetType == typeof(System.IO.FileInfo))
    {
      yield return new CompletionCandidate(
        "<file>",
        "File path",
        CompletionType.File
      );
    }
    else if (targetType == typeof(System.IO.DirectoryInfo))
    {
      yield return new CompletionCandidate(
        "<directory>",
        "Directory path",
        CompletionType.Directory
      );
    }
    // Handle enum types - enumerate values filtered by partial input
    else if (targetType.IsEnum)
    {
      foreach (string enumName in Enum.GetNames(targetType))
      {
        // Filter by partial word if provided (case-insensitive)
        if (string.IsNullOrEmpty(partialWord) ||
            enumName.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
        {
          yield return new CompletionCandidate(
            enumName,
            $"{targetType.Name}.{enumName}",
            CompletionType.Enum
          );
        }
      }
    }
  }

  /// <summary>
  /// Get the Type for a given constraint name.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
    "Trimming",
    "IL2057:Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.",
    Justification = "Type.GetType is a fallback for custom types. Standard types are handled by TypeConverterRegistry.")]
  private Type? GetTypeForConstraint(string constraint)
  {
    // Try to get converter from registry
    IRouteTypeConverter? converter = TypeConverterRegistry.GetConverterByConstraint(constraint);
    if (converter is not null)
    {
      return converter.TargetType;
    }

    // Fallback: try direct type lookup (for custom types)
    // This path is only hit for custom types not registered in the converter registry
    return Type.GetType(constraint);
  }
}
