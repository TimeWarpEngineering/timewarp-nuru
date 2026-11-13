#region Purpose
// Emits the per-route JSON attempt body, literal checks, and option scans.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class JsonArgsEmitter
{
  /// <summary>
  /// The argv a named route matches with no parameter or option tokens: group words, literal
  /// segments and <c>--</c> separators, in pattern order.
  /// </summary>
  private static List<string> LiteralWords(RouteDefinition route)
  {
    List<string> words = [.. GroupWords(route)];
    foreach (SegmentDefinition segment in route.Segments)
    {
      if (segment is LiteralDefinition literal)
        words.Add(literal.Value);
      else if (segment is EndOfOptionsSeparatorDefinition)
        words.Add("--");
    }

    return words;
  }

  private static void EmitAttemptBody(
    StringBuilder sb,
    RouteDefinition route,
    List<Slot> slots,
    string pattern,
    IReadOnlyList<string> prefix,
    bool aliasMode,
    bool bind,
    int indent)
  {
    string ind = new(' ', indent);
    sb.AppendLine($"{ind}int __end = global::System.Array.IndexOf(routeArgs, \"--\");");
    sb.AppendLine($"{ind}if (__end < 0) __end = routeArgs.Length;");
    sb.AppendLine($"{ind}global::System.Collections.Generic.HashSet<int> __consumed = new();");
    List<string> forms = [];
    foreach (Slot slot in slots)
    {
      if (slot.Option?.LongForm is not null)
        forms.Add($"\"--{Escape(slot.Option.LongForm)}\"");

      if (slot.Option?.ShortForm is not null)
        forms.Add($"\"-{Escape(slot.Option.ShortForm)}\"");
    }

    string formInit = forms.Count == 0 ? "" : string.Join(", ", forms);
    sb.AppendLine($"{ind}global::System.Collections.Generic.HashSet<string> __forms = new() {{ {formInit} }};");

    foreach (Slot slot in slots)
    {
      sb.AppendLine($"{ind}bool __av_{slot.Id} = false;");
      if (slot.IsList)
      {
        sb.AppendLine($"{ind}global::System.Collections.Generic.List<string> __al_{slot.Id} = new();");
      }
      else if (!slot.IsFlag)
      {
        sb.AppendLine($"{ind}string? __ar_{slot.Id} = null;");
      }
    }

    foreach (Slot slot in slots.Where(s => s.IsOption && s.Option is not null))
    {
      EmitOptionScan(sb, slot, indent);
    }

    sb.AppendLine($"{ind}global::System.Collections.Generic.List<string> __positionalList = new();");
    sb.AppendLine($"{ind}for (int __i = 0; __i < routeArgs.Length; __i++)");
    sb.AppendLine($"{ind}{{");
    if (!route.HasEndOfOptions)
    {
      sb.AppendLine($"{ind}  if (__i == __end && routeArgs[__i] == \"--\") continue;");
    }

    sb.AppendLine($"{ind}  if (__consumed.Contains(__i)) continue;");
    sb.AppendLine($"{ind}  __positionalList.Add(routeArgs[__i]);");
    sb.AppendLine($"{ind}}}");
    sb.AppendLine($"{ind}string[] __pos = [.. __positionalList];");
    sb.AppendLine($"{ind}int __pi = 0;");

    foreach (string word in prefix)
    {
      EmitLiteral(sb, word, indent);
    }

    List<SegmentDefinition> segments = [.. route.Segments];
    for (int i = 0; i < segments.Count; i++)
    {
      int reserved = ReservedAfter(segments, i, aliasMode);
      switch (segments[i])
      {
        case LiteralDefinition literal when !aliasMode:
          sb.AppendLine($"{ind}if (__litOk)");
          sb.AppendLine($"{ind}{{");
          EmitLiteral(sb, literal.Value, indent + 2);
          sb.AppendLine($"{ind}}}");
          break;

        case ParameterDefinition param:
          Slot? slot = slots.FirstOrDefault(s => !s.IsOption && ReferenceEquals(s.Parameter, param));
          if (slot is null)
            break;

          sb.AppendLine($"{ind}if (__litOk && __ok)");
          sb.AppendLine($"{ind}{{");
          if (param.IsCatchAll)
          {
            sb.AppendLine($"{ind}  if (__pos.Length > __pi)");
            sb.AppendLine($"{ind}  {{");
            sb.AppendLine($"{ind}    __av_{slot.Id} = true;");
            sb.AppendLine($"{ind}    while (__pi < __pos.Length) __al_{slot.Id}.Add(__pos[__pi++]);");
            sb.AppendLine($"{ind}  }}");
          }
          else
          {
            sb.AppendLine($"{ind}  if (__pos.Length - __pi > {reserved})");
            sb.AppendLine($"{ind}  {{");
            sb.AppendLine($"{ind}    __av_{slot.Id} = true;");
            sb.AppendLine($"{ind}    __ar_{slot.Id} = __pos[__pi];");
            sb.AppendLine($"{ind}    __pi++;");
            sb.AppendLine($"{ind}  }}");
            sb.AppendLine($"{ind}  else if (!__jsonMap.ContainsKey(\"{Escape(slot.Key)}\"))");
            sb.AppendLine($"{ind}  {{");
            if (!slot.Required)
            {
              sb.AppendLine($"{ind}    // Optional parameter stays absent.");
            }
            else
            {
              Fail missing = new("MissingValue", KeyLiteral(slot.Key), $"\"Error: Missing required value '{Escape(slot.Key)}' for '{pattern}'.\"");
              sb.AppendLine($"{ind}    __ok = false;");
              sb.AppendLine($"{ind}    {missing.Assign()}");
              // Later literals still run. They clear __litOk when they do not match,
              // so this failure is reported only when the route's literals match.
            }

            sb.AppendLine($"{ind}  }}");
          }

          sb.AppendLine($"{ind}}}");
          break;

        case EndOfOptionsSeparatorDefinition:
          sb.AppendLine($"{ind}if (__litOk)");
          sb.AppendLine($"{ind}{{");
          sb.AppendLine($"{ind}  if (__pi >= __pos.Length || __pos[__pi] != \"--\")");
          sb.AppendLine($"{ind}  {{");
          sb.AppendLine($"{ind}    __ok = false;");
          sb.AppendLine($"{ind}    __litOk = false;");
          sb.AppendLine($"{ind}  }}");
          sb.AppendLine($"{ind}  else");
          sb.AppendLine($"{ind}  {{");
          sb.AppendLine($"{ind}    __pi++;");
          sb.AppendLine($"{ind}  }}");
          sb.AppendLine($"{ind}}}");
          break;
      }
    }

    sb.AppendLine($"{ind}if (__litOk && __pi != __pos.Length)");
    sb.AppendLine($"{ind}{{");
    sb.AppendLine($"{ind}  __ok = false;");
    Fail unexpected = new("UnexpectedArgument", "null", $"\"Error: Unexpected argument for '{pattern}'.\"");
    sb.AppendLine($"{ind}  {unexpected.Assign()}");
    sb.AppendLine($"{ind}}}");

    EmitUnknownKeys(sb, slots, pattern, indent);
    EmitKindGates(sb, slots, pattern, indent);
    EmitRequiredOptions(sb, slots, pattern, indent);

    if (!bind)
    {
      foreach (Slot slot in slots)
      {
        if (slot.IsList)
        {
          sb.AppendLine($"{ind}_ = __al_{slot.Id}.Count;");
        }
        else if (!slot.IsFlag)
        {
          sb.AppendLine($"{ind}_ = __ar_{slot.Id};");
        }
      }
    }
    else
    {
      sb.AppendLine($"{ind}if (__ok)");
      sb.AppendLine($"{ind}{{");
      foreach (Slot slot in slots)
      {
        if (slot.Option is not null && NeedsJsonSuppliedFlag(route, slot.Option))
        {
          sb.AppendLine($"{ind}  __supplied_{slot.Id} = __av_{slot.Id} || __jsonMap.ContainsKey(\"{Escape(slot.Key)}\");");
        }

        if (slot.VariableName is null)
          continue;

        EmitFill(sb, slot, pattern, indent + 2);
      }

      sb.AppendLine($"{ind}}}");
    }

    _ = route;
  }

  private static void EmitLiteral(StringBuilder sb, string word, int indent)
  {
    string ind = new(' ', indent);
    string literal = Escape(word);
    sb.AppendLine($"{ind}if (__pi >= __pos.Length || !string.Equals(__pos[__pi], \"{literal}\", global::System.StringComparison.Ordinal))");
    sb.AppendLine($"{ind}{{");
    sb.AppendLine($"{ind}  __ok = false;");
    sb.AppendLine($"{ind}  __litOk = false;");
    sb.AppendLine($"{ind}}}");
    sb.AppendLine($"{ind}else");
    sb.AppendLine($"{ind}{{");
    sb.AppendLine($"{ind}  __pi++;");
    sb.AppendLine($"{ind}  __lit++;");
    sb.AppendLine($"{ind}}}");
  }

  private static void EmitOptionScan(StringBuilder sb, Slot slot, int indent)
  {
    OptionDefinition option = slot.Option!;
    string ind = new(' ', indent);
    sb.AppendLine($"{ind}for (int __i = 0; __i < __end; __i++)");
    sb.AppendLine($"{ind}{{");
    if (slot.IsFlag)
    {
      sb.AppendLine($"{ind}  if ({FlagCondition(option)})");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __av_{slot.Id} = true;");
      sb.AppendLine($"{ind}    __consumed.Add(__i);");
      sb.AppendLine($"{ind}    break;");
      sb.AppendLine($"{ind}  }}");
    }
    else if (slot.IsList)
    {
      EmitEqualsArm(sb, option, slot, indent + 2, repeated: true);
      sb.AppendLine($"{ind}  if ({FlagCondition(option)})");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __av_{slot.Id} = true;");
      sb.AppendLine($"{ind}    __consumed.Add(__i);");
      sb.AppendLine($"{ind}    if (__i + 1 < __end && !__forms.Contains(routeArgs[__i + 1]))");
      sb.AppendLine($"{ind}    {{");
      sb.AppendLine($"{ind}      __al_{slot.Id}.Add(routeArgs[__i + 1]);");
      sb.AppendLine($"{ind}      __consumed.Add(__i + 1);");
      sb.AppendLine($"{ind}      __i++;");
      sb.AppendLine($"{ind}    }}");
      sb.AppendLine($"{ind}  }}");
    }
    else
    {
      EmitEqualsArm(sb, option, slot, indent + 2, repeated: false);
      sb.AppendLine($"{ind}  if ({FlagCondition(option)})");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __av_{slot.Id} = true;");
      sb.AppendLine($"{ind}    __consumed.Add(__i);");
      sb.AppendLine($"{ind}    if (__i + 1 < __end && !__forms.Contains(routeArgs[__i + 1]))");
      sb.AppendLine($"{ind}    {{");
      sb.AppendLine($"{ind}      __ar_{slot.Id} = routeArgs[__i + 1];");
      sb.AppendLine($"{ind}      __consumed.Add(__i + 1);");
      sb.AppendLine($"{ind}    }}");
      sb.AppendLine($"{ind}    break;");
      sb.AppendLine($"{ind}  }}");
    }

    sb.AppendLine($"{ind}}}");
  }

  private static void EmitEqualsArm(StringBuilder sb, OptionDefinition option, Slot slot, int indent, bool repeated)
  {
    string ind = new(' ', indent);
    if (option.LongForm is not null)
    {
      string prefix = "--" + option.LongForm + "=";
      sb.AppendLine($"{ind}if (routeArgs[__i].StartsWith(\"{Escape(prefix)}\", global::System.StringComparison.Ordinal))");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __av_{slot.Id} = true;");
      sb.AppendLine($"{ind}  __consumed.Add(__i);");
      if (repeated)
      {
        sb.AppendLine($"{ind}  __al_{slot.Id}.Add(routeArgs[__i].Substring({prefix.Length}));");
      }
      else
      {
        sb.AppendLine($"{ind}  __ar_{slot.Id} = routeArgs[__i].Substring({prefix.Length});");
        sb.AppendLine($"{ind}  break;");
      }

      sb.AppendLine($"{ind}}}");
    }

    if (option.ShortForm is not null)
    {
      string prefix = "-" + option.ShortForm + "=";
      sb.AppendLine($"{ind}if (routeArgs[__i].StartsWith(\"{Escape(prefix)}\", global::System.StringComparison.Ordinal))");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __av_{slot.Id} = true;");
      sb.AppendLine($"{ind}  __consumed.Add(__i);");
      if (repeated)
      {
        sb.AppendLine($"{ind}  __al_{slot.Id}.Add(routeArgs[__i].Substring({prefix.Length}));");
      }
      else
      {
        sb.AppendLine($"{ind}  __ar_{slot.Id} = routeArgs[__i].Substring({prefix.Length});");
        sb.AppendLine($"{ind}  break;");
      }

      sb.AppendLine($"{ind}}}");
    }
  }
}
