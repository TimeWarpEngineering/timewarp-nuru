namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Validates the semantic correctness of route patterns.
/// Checks for logical errors that are syntactically valid but semantically incorrect.
/// </summary>
public sealed class SemanticValidator
{
  private const string EndOfOptionsSeparator = "--";

  /// <summary>
  /// Validates a route syntax tree for semantic correctness.
  /// </summary>
  /// <param name="ast">The syntax tree to validate.</param>
  /// <returns>List of semantic errors found, null if valid.</returns>
  public static IReadOnlyList<SemanticError>? Validate(Syntax ast)
  {
    ArgumentNullException.ThrowIfNull(ast);

    List<SemanticError> semanticErrors = [];
    var context = new ValidationContext();

    // First pass: collect all segments and their metadata
    CollectSegmentMetadata(ast, context);

    // Run validation rules
    ValidateDuplicateParameters(context, semanticErrors);
    ValidateOptionalBeforeRequired(context, semanticErrors);
    ValidateConsecutiveOptionalParameters(context, semanticErrors);
    ValidateCatchAllPosition(context, semanticErrors);
    ValidateEndOfOptionsSeparator(context, semanticErrors);
    ValidateDuplicateOptionAliases(context, semanticErrors);
    ValidateMixedCatchAllWithOptional(context, semanticErrors);

    return semanticErrors.Count > 0 ? [.. semanticErrors] : null;
  }

  private static void CollectSegmentMetadata(Syntax ast, ValidationContext context)
  {
    for (int i = 0; i < ast.Segments.Count; i++)
    {
      SegmentSyntax segment = ast.Segments[i];
      context.AllSegments.Add(segment);

      switch (segment)
      {
        case ParameterSyntax param:
          context.Parameters.Add(param);

          // Track parameter name (for both positional and in options)
          if (!context.ParametersByName.TryGetValue(param.Name, out List<SegmentSyntax>? list))
          {
            list = [];
            context.ParametersByName[param.Name] = list;
          }

          list.Add(param);

          // Track parameter types
          if (param.IsOptional && !param.IsCatchAll)
            context.HasOptionalParameters = true;
          if (param.IsCatchAll)
            context.HasCatchAllParameter = true;
          break;

        case OptionSyntax option:
          context.Options.Add(option);

          // Track option parameter names
          if (option.Parameter is not null)
          {
            if (!context.ParametersByName.TryGetValue(option.Parameter.Name, out List<SegmentSyntax>? paramList))
            {
              paramList = [];
              context.ParametersByName[option.Parameter.Name] = paramList;
            }

            // Store the option, not just its parameter, for better error messages
            paramList.Add(option);
          }

          // Track aliases
          if (option.ShortForm is not null)
            context.OptionAliases[option.ShortForm] = option;
          break;

        case LiteralSyntax literal:
          context.Literals.Add(literal);
          if (literal.Value == EndOfOptionsSeparator)
            context.EndOfOptionsIndex = i;
          break;
      }
    }
  }

  // NURU_S001: Check for duplicate parameter names (including across positional and options)
  private static void ValidateDuplicateParameters(ValidationContext context, List<SemanticError> semanticErrors)
  {
    foreach (KeyValuePair<string, List<SegmentSyntax>> kvp in context.ParametersByName)
    {
      if (kvp.Value.Count > 1)
      {
        // Found duplicate - report on the second occurrence
        SegmentSyntax first = kvp.Value[0];
        SegmentSyntax second = kvp.Value[1];

        string firstLocation = first switch
        {
          ParameterSyntax p => $"parameter '{p.Name}'",
          OptionSyntax o => $"option '{o.LongForm ?? o.ShortForm}'",
          _ => "unknown"
        };

        string secondLocation = second switch
        {
          ParameterSyntax p => $"parameter '{p.Name}'",
          OptionSyntax o => $"option '{o.LongForm ?? o.ShortForm}'",
          _ => "unknown"
        };

        semanticErrors.Add(new DuplicateParameterNamesError(
          second.Position,
          second.Length,
          kvp.Key));
      }
    }
  }

  // NURU_S006: Check for optional parameters before required ones
  private static void ValidateOptionalBeforeRequired(ValidationContext context, List<SemanticError> semanticErrors)
  {
    bool foundOptional = false;
    ParameterSyntax? lastOptionalParam = null;

    foreach (SegmentSyntax segment in context.AllSegments)
    {
      if (segment is ParameterSyntax param && !param.IsCatchAll)
      {
        if (param.IsOptional)
        {
          foundOptional = true;
          lastOptionalParam = param;
        }
        else if (foundOptional)
        {
          // Found a required parameter after an optional one
          semanticErrors.Add(new OptionalBeforeRequiredError(
            lastOptionalParam!.Position,
            lastOptionalParam.Length,
            lastOptionalParam.Name,
            param.Name));
          break; // Only report the first occurrence
        }
      }
      else if (segment is OptionSyntax || segment is LiteralSyntax)
      {
        // Reset when we hit non-parameter segments (options or literals break the positional sequence)
        foundOptional = false;
        lastOptionalParam = null;
      }
    }
  }

  // NURU_S002: Check for consecutive optional positional parameters
  private static void ValidateConsecutiveOptionalParameters(ValidationContext context, List<SemanticError> semanticErrors)
  {
    ParameterSyntax? lastOptionalParam = null;

    foreach (SegmentSyntax segment in context.AllSegments)
    {
      if (segment is ParameterSyntax param)
      {
        if (param.IsOptional && !param.IsCatchAll)
        {
          if (lastOptionalParam is not null)
          {
            semanticErrors.Add(new ConflictingOptionalParametersError(
              param.Position,
              param.Length,
              [lastOptionalParam.Name, param.Name]));
          }

          lastOptionalParam = param;
        }
        else
        {
          lastOptionalParam = null;
        }
      }
      else if (segment is OptionSyntax || segment is LiteralSyntax)
      {
        // Reset when we hit non-parameter segments
        lastOptionalParam = null;
      }
    }
  }

  // NURU_S003: Check if catch-all has positional segments after it
  private static void ValidateCatchAllPosition(ValidationContext context, List<SemanticError> semanticErrors)
  {
    for (int i = 0; i < context.AllSegments.Count; i++)
    {
      if (context.AllSegments[i] is ParameterSyntax param && param.IsCatchAll)
      {
        // Check if there are positional segments after this catch-all
        for (int j = i + 1; j < context.AllSegments.Count; j++)
        {
          SegmentSyntax nextSegment = context.AllSegments[j];

          // Check if this is a positional segment (not an option or end-of-options)
          bool isPositional = nextSegment is ParameterSyntax ||
            (nextSegment is LiteralSyntax lit && lit.Value != EndOfOptionsSeparator);

          if (isPositional)
          {
            // Get a description of what follows the catch-all
            string followingSegmentDescription = nextSegment switch
            {
              ParameterSyntax p => p.Name,
              LiteralSyntax l => $"'{l.Value}'",
              _ => nextSegment.GetType().Name
            };

            semanticErrors.Add(new CatchAllNotAtEndError(
              param.Position,
              param.Length,
              param.Name,
              followingSegmentDescription));
            break;
          }
        }
      }
    }
  }

  // NURU_S007, NURU_S008: Validate end-of-options separator usage
  private static void ValidateEndOfOptionsSeparator(ValidationContext context, List<SemanticError> semanticErrors)
  {
    if (context.EndOfOptionsIndex is null)
      return;

    int index = context.EndOfOptionsIndex.Value;
    SegmentSyntax separator = context.AllSegments[index];

    // Check if options appear after --
    for (int i = index + 1; i < context.AllSegments.Count; i++)
    {
      if (context.AllSegments[i] is OptionSyntax option)
      {
        semanticErrors.Add(new OptionsAfterEndOfOptionsSeparatorError(
          option.Position,
          option.Length,
          option.LongForm ?? $"-{option.ShortForm}"));
      }
    }

    // Check if -- is followed by catch-all
    if (index == context.AllSegments.Count - 1)
    {
      semanticErrors.Add(new InvalidEndOfOptionsSeparatorError(
        separator.Position,
        separator.Length,
        "must be followed by a catch-all parameter"));
    }
    else if (index + 1 < context.AllSegments.Count)
    {
      SegmentSyntax nextSegment = context.AllSegments[index + 1];
      if (!(nextSegment is ParameterSyntax nextParam) || !nextParam.IsCatchAll)
      {
        semanticErrors.Add(new InvalidEndOfOptionsSeparatorError(
          separator.Position,
          separator.Length,
          "must be followed by a catch-all parameter"));
      }
    }
  }

  // NURU_S005: Check for duplicate option aliases
  private static void ValidateDuplicateOptionAliases(ValidationContext context, List<SemanticError> semanticErrors)
  {
    var seen = new Dictionary<string, OptionSyntax>();

    foreach (OptionSyntax option in context.Options)
    {
      if (option.ShortForm is not null)
      {
        if (seen.TryGetValue(option.ShortForm, out OptionSyntax? existing))
        {
          semanticErrors.Add(new DuplicateOptionAliasError(
            option.Position,
            option.Length,
            option.ShortForm!,
            [existing.LongForm ?? existing.ShortForm!]));
        }
        else
        {
          seen[option.ShortForm] = option;
        }
      }
    }
  }

  // NURU_S004: Check for mixed catch-all with optional parameters
  private static void ValidateMixedCatchAllWithOptional(ValidationContext context, List<SemanticError> semanticErrors)
  {
    if (context.HasOptionalParameters && context.HasCatchAllParameter)
    {
      ParameterSyntax? catchAllParam = context.Parameters.FirstOrDefault(p => p.IsCatchAll);

      // This should never happen if HasCatchAllParameter is true, but let's be defensive
      if (catchAllParam is null)
      {
        throw new InvalidOperationException(
          "Internal error: HasCatchAllParameter is true but no catch-all parameter found in context");
      }

      var optionalParams = context.Parameters.Where(p => p.IsOptional && !p.IsCatchAll).Select(p => p.Name).ToList();

      semanticErrors.Add(new MixedCatchAllWithOptionalError(
        catchAllParam.Position,
        catchAllParam.Length,
        catchAllParam.Name,
        optionalParams));
    }
  }

  // AddError method removed - create specific error types directly
}
