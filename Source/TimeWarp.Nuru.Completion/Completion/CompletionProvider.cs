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
      candidates.AddRange(GetCommandCompletions(endpoints, context.Args.ElementAtOrDefault(0) ?? ""));
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

    // Remove duplicates
    return
    [
      .. candidates
      .GroupBy(c => c.Value)
      .Select(g => g.First())
      .OrderBy(c => c.Value)
    ];
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

      if (segment is LiteralMatcher literal)
      {
        string partial = cursorPosition < args.Length ? args[cursorPosition] : "";
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
        candidates.AddRange(GetParameterCompletions(parameter));
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
  private IEnumerable<CompletionCandidate> GetParameterCompletions(
    ParameterMatcher parameter)
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
    // Handle enum types - enumerate all values
    else if (targetType.IsEnum)
    {
      foreach (string enumName in Enum.GetNames(targetType))
      {
        yield return new CompletionCandidate(
          enumName,
          $"{targetType.Name}.{enumName}",
          CompletionType.Enum
        );
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
