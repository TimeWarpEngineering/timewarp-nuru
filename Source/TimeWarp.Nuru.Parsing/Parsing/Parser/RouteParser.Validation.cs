namespace TimeWarp.Nuru.Parsing;

using System.Collections.Generic;

// Partial class containing the refactored validation logic
public sealed partial class RouteParser
{
  /// <summary>
  /// Validates the semantic correctness of the parsed route syntax tree.
  /// </summary>
  private void ValidateSemantics(RouteSyntax ast)
  {
    var context = new ValidationContext();

    // First pass: collect all segments and their metadata
    CollectSegmentMetadata(ast, context);

    // Run validation rules
    ValidateDuplicateParameters(context);
    ValidateConsecutiveOptionalParameters(context);
    ValidateCatchAllPosition(context);
    ValidateEndOfOptionsSeparator(context);
    ValidateDuplicateOptionAliases(context);
    ValidateMixedCatchAllWithOptional(context);
  }

  private sealed class ValidationContext
  {
    public Dictionary<string, List<SegmentSyntax>> ParametersByName { get; } = [];
    public Dictionary<string, OptionSyntax> OptionAliases { get; } = [];
    public List<ParameterSyntax> Parameters { get; } = [];
    public List<OptionSyntax> Options { get; } = [];
    public List<LiteralSyntax> Literals { get; } = [];
    public List<SegmentSyntax> AllSegments { get; } = [];
    public int? EndOfOptionsIndex { get; set; }
    public bool HasOptionalParameters { get; set; }
    public bool HasCatchAllParameter { get; set; }
  }

  private static void CollectSegmentMetadata(RouteSyntax ast, ValidationContext context)
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

            paramList.Add(option);
          }

          // Track aliases
          if (option.ShortForm is not null)
            context.OptionAliases[option.ShortForm] = option;
          break;

        case LiteralSyntax literal:
          context.Literals.Add(literal);
          if (literal.Value == "--")
            context.EndOfOptionsIndex = i;
          break;
      }
    }
  }

  // NURU006: Check for duplicate parameter names (including across positional and options)
  private void ValidateDuplicateParameters(ValidationContext context)
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

        AddError($"Duplicate parameter name '{kvp.Key}' found in {firstLocation} and {secondLocation}",
          second.Position,
          second.Length,
          ParseErrorType.DuplicateParameterNames);
      }
    }
  }

  // NURU007: Check for consecutive optional positional parameters
  private void ValidateConsecutiveOptionalParameters(ValidationContext context)
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
            AddError($"Multiple consecutive optional positional parameters create ambiguity: {lastOptionalParam.Name}? {param.Name}?",
              param.Position,
              param.Length,
              ParseErrorType.ConflictingOptionalParameters);
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

  // NURU005: Check if catch-all has positional segments after it
  private void ValidateCatchAllPosition(ValidationContext context)
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
            (nextSegment is LiteralSyntax lit && lit.Value != "--");

          if (isPositional)
          {
            AddError($"Catch-all parameter '{param.Name}' must be the last positional segment in the route",
              param.Position,
              param.Length,
              ParseErrorType.CatchAllNotAtEnd);
            break;
          }
        }
      }
    }
  }

  // Validate end-of-options separator usage
  private void ValidateEndOfOptionsSeparator(ValidationContext context)
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
        AddError("Options cannot appear after '--' separator",
          option.Position,
          option.Length,
          ParseErrorType.InvalidParameterSyntax);
      }
    }

    // Check if -- is followed by catch-all
    if (index == context.AllSegments.Count - 1)
    {
      AddError("End-of-options separator '--' must be followed by a catch-all parameter",
        separator.Position,
        separator.Length,
        ParseErrorType.InvalidParameterSyntax);
    }
    else if (index + 1 < context.AllSegments.Count)
    {
      SegmentSyntax nextSegment = context.AllSegments[index + 1];
      if (!(nextSegment is ParameterSyntax nextParam) || !nextParam.IsCatchAll)
      {
        AddError("End-of-options separator '--' must be followed by a catch-all parameter",
          separator.Position,
          separator.Length,
          ParseErrorType.InvalidParameterSyntax);
      }
    }
  }

  // NURU009: Check for duplicate option aliases
  private void ValidateDuplicateOptionAliases(ValidationContext context)
  {
    var seen = new Dictionary<string, OptionSyntax>();

    foreach (OptionSyntax option in context.Options)
    {
      if (option.ShortForm is not null)
      {
        if (seen.TryGetValue(option.ShortForm, out OptionSyntax? existing))
        {
          AddError($"Duplicate option short form '-{option.ShortForm}' already used by '{existing.LongForm ?? existing.ShortForm}'",
            option.Position,
            option.Length,
            ParseErrorType.DuplicateOptionAlias);
        }
        else
        {
          seen[option.ShortForm] = option;
        }
      }
    }
  }

  // NURU008: Check for mixed catch-all with optional parameters
  private void ValidateMixedCatchAllWithOptional(ValidationContext context)
  {
    if (context.HasOptionalParameters && context.HasCatchAllParameter)
    {
      AddError("Cannot mix optional parameters with catch-all parameter in the same route",
        0,
        0,
        ParseErrorType.MixedCatchAllWithOptional);
    }
  }
}
