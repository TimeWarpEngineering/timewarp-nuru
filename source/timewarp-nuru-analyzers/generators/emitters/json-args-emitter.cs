// Emits --json-args peel, parse, candidate selection, and reflection-free binding.
// Calls without --json-args keep the existing matcher. This path runs only when a
// JSON object was loaded. It does not use ComputedSpecificity to break ties.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Generates the <c>--json-args</c> preamble and the JSON route-selection path.
/// </summary>
internal static class JsonArgsEmitter
{
  private enum ValueShape
  {
    Text,
    Bool,
    Number,
    Enum,
    TextList,
    NumberList,
    EnumList
  }

  private sealed class Slot
  {
    public required string Key { get; init; }
    public required string Id { get; init; }
    public bool IsOption { get; init; }
    public bool IsFlag { get; init; }
    public bool IsList { get; init; }
    public bool IsCatchAll { get; init; }
    public bool Required { get; init; }
    public bool RequiredFlag { get; init; }
    public bool IsNullable { get; init; }
    public string? BaseConstraint { get; init; }
    public string? DeclaredType { get; init; }
    public string? VariableName { get; init; }
    public string? EnumTypeName { get; init; }
    public string? NumberClr { get; init; }
    public string? NumberKeyword { get; init; }
    public CustomConverterDefinition? Converter { get; init; }
    public OptionDefinition? Option { get; init; }
    public ParameterDefinition? Parameter { get; init; }
    public ValueShape Shape { get; init; }
    public string ExpectedType { get; init; } = "string";
  }

  /// <summary>
  /// Peels <c>--json-args</c> from the original args, rejects attached forms, filters
  /// config args, and either leaves a built-in for later dispatch or parses the payload.
  /// </summary>
  public static void EmitPrepare(StringBuilder sb, AppModel app)
  {
    sb.AppendLine("    // --json-args is peeled from the original args before IsConfigArg and before user routes.");
    sb.AppendLine("    string? __jsonArgsValue = null;");
    sb.AppendLine("    bool __jsonArgsSeen = false;");
    sb.AppendLine("    global::System.Collections.Generic.List<string> __jsonPeeledArgs = new(args.Length);");
    sb.AppendLine("    for (int __ji = 0; __ji < args.Length; __ji++)");
    sb.AppendLine("    {");
    sb.AppendLine("      string __jarg = args[__ji];");
    sb.AppendLine("      if (__jarg.StartsWith(\"--json-args=\", global::System.StringComparison.Ordinal)");
    sb.AppendLine("        || __jarg.StartsWith(\"--json-args:\", global::System.StringComparison.Ordinal)");
    sb.AppendLine("        || __jarg.StartsWith(\"/json-args=\", global::System.StringComparison.Ordinal)");
    sb.AppendLine("        || __jarg.StartsWith(\"/json-args:\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("      {");
    sb.AppendLine("        await app.Terminal.WriteErrorLineAsync(\"Error: --json-args must be a separate token. Use --json-args -, --json-args @path, or --json-args '{...}'.\").ConfigureAwait(false);");
    sb.AppendLine("        return 1;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      if (string.Equals(__jarg, \"--json-args\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("      {");
    sb.AppendLine("        if (__jsonArgsSeen)");
    sb.AppendLine("        {");
    sb.AppendLine("          await app.Terminal.WriteErrorLineAsync(\"Error: --json-args was specified more than once.\").ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        __jsonArgsSeen = true;");
    sb.AppendLine("        if (__ji + 1 >= args.Length)");
    sb.AppendLine("        {");
    sb.AppendLine("          await app.Terminal.WriteErrorLineAsync(\"Error: --json-args requires a value.\").ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        __jsonArgsValue = args[++__ji];");
    sb.AppendLine("        continue;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      __jsonPeeledArgs.Add(__jarg);");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    static bool IsConfigArg(string arg)");
    sb.AppendLine("    {");
    sb.AppendLine("      if (arg.StartsWith(\"--\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("      {");
    sb.AppendLine("        int eqIdx = arg.IndexOf('=');");
    sb.AppendLine("        int colonIdx = arg.IndexOf(':');");
    sb.AppendLine("        return (eqIdx > 2) || (colonIdx > 2);");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      if (arg.StartsWith(\"/\", global::System.StringComparison.Ordinal) && arg.Length > 1 && char.IsLetter(arg[1]))");
    sb.AppendLine("      {");
    sb.AppendLine("        int eqIdx = arg.IndexOf('=');");
    sb.AppendLine("        int colonIdx = arg.IndexOf(':');");
    sb.AppendLine("        return (eqIdx > 1) || (colonIdx > 1);");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      return false;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    string[] routeArgs = [.. __jsonPeeledArgs.Where(arg => !IsConfigArg(arg))];");
    sb.AppendLine("    global::System.Text.Json.JsonDocument? __jsonDoc = null;");
    sb.AppendLine("    bool __jsonBuiltInOnly = false;");
    sb.AppendLine("    if (__jsonArgsValue is not null)");
    sb.AppendLine("    {");
    sb.AppendLine("      bool __jsonHelpToken = false;");
    sb.AppendLine("      for (int __hi = 0; __hi < routeArgs.Length; __hi++)");
    sb.AppendLine("      {");
    sb.AppendLine("        if (routeArgs[__hi] is \"--help\" or \"-h\")");
    sb.AppendLine("        {");
    sb.AppendLine("          __jsonHelpToken = true;");
    sb.AppendLine("          break;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      __jsonBuiltInOnly = __MatchesNoBodyBuiltIn(routeArgs);");
    sb.AppendLine("      if (!__jsonBuiltInOnly && !__jsonHelpToken)");
    sb.AppendLine("      {");
    EmitParse(sb);
    sb.AppendLine("      }");
    sb.AppendLine("    }");
    sb.AppendLine();
    EmitBuiltInPredicate(sb, app);
  }

  /// <summary>
  /// When a JSON object is loaded, selects a route by literal count and binds it.
  /// Always returns from the generated method on this path.
  /// </summary>
  public static void EmitDispatch(
    StringBuilder sb,
    IReadOnlyList<RouteDefinition> routes,
    AppModel app,
    string methodSuffix,
    string? loggerFactoryFieldName)
  {
    sb.AppendLine("    if (__jsonDoc is not null)");
    sb.AppendLine("    {");
    sb.AppendLine("      using (__jsonDoc)");
    sb.AppendLine("      {");
    sb.AppendLine("        global::System.Collections.Generic.Dictionary<string, global::System.Text.Json.JsonElement> __jsonMap = new(global::System.StringComparer.Ordinal);");
    sb.AppendLine("        foreach (global::System.Text.Json.JsonProperty __prop in __jsonDoc.RootElement.EnumerateObject())");
    sb.AppendLine("        {");
    sb.AppendLine("          __jsonMap[__prop.Name] = __prop.Value;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        int __jsonWinner = -1;");
    sb.AppendLine("        int __jsonBestLiterals = -1;");
    sb.AppendLine("        int __jsonWinnerAttempt = 0;");
    sb.AppendLine("        global::System.Collections.Generic.List<string> __jsonTied = new();");
    sb.AppendLine("        global::System.Collections.Generic.List<string> __jsonFailures = new();");

    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
    {
      RouteDefinition route = routes[routeIndex];
      List<Slot> slots = BuildSlots(route, routeIndex, app.CustomConverters);
      string pattern = EmitterStringUtils.EscapeForStringLiteral(route.FullPattern);
      sb.AppendLine();
      sb.AppendLine($"        bool __TryJson_{routeIndex}(out int literalCount, out int attempt, out bool literalsMatched, out string? failure)");
      sb.AppendLine("        {");
      sb.AppendLine("          literalCount = 0;");
      sb.AppendLine("          attempt = 0;");
      sb.AppendLine("          literalsMatched = false;");
      sb.AppendLine("          failure = null;");
      sb.AppendLine("          bool __ok;");
      sb.AppendLine("          int __lit;");
      sb.AppendLine("          bool __litOk;");
      sb.AppendLine("          string? __fail;");
      sb.AppendLine("          int __best = -1;");
      sb.AppendLine("          int __bestAttempt = 0;");
      sb.AppendLine("          bool __any = false;");
      sb.AppendLine("          bool __anyLit = false;");
      sb.AppendLine("          string? __anyFail = null;");

      EmitCheckCall(sb, route, slots, pattern, routeIndex, attempt: 0, prefix: GroupWords(route), aliasMode: false, indent: 10);
      List<string> aliases = route.Aliases.IsDefaultOrEmpty ? [] : [.. route.Aliases];
      for (int aliasIndex = 0; aliasIndex < aliases.Count; aliasIndex++)
      {
        string[] aliasWords = aliases[aliasIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        EmitCheckCall(sb, route, slots, pattern, routeIndex, attempt: aliasIndex + 1, prefix: aliasWords, aliasMode: true, indent: 10);
      }

      sb.AppendLine("          if (__any)");
      sb.AppendLine("          {");
      sb.AppendLine("            literalCount = __best;");
      sb.AppendLine("            attempt = __bestAttempt;");
      sb.AppendLine("            return true;");
      sb.AppendLine("          }");
      sb.AppendLine();
      sb.AppendLine("          if (__anyLit)");
      sb.AppendLine("          {");
      sb.AppendLine("            literalsMatched = true;");
      sb.AppendLine("            failure = __anyFail;");
      sb.AppendLine("          }");
      sb.AppendLine();
      sb.AppendLine("          return false;");
      sb.AppendLine("        }");
    }

    sb.AppendLine();
    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
    {
      string pattern = EmitterStringUtils.EscapeForStringLiteral(routes[routeIndex].FullPattern);
      sb.AppendLine($"        if (__TryJson_{routeIndex}(out int __jl_{routeIndex}, out int __ja_{routeIndex}, out bool __jm_{routeIndex}, out string? __jf_{routeIndex}))");
      sb.AppendLine("        {");
      sb.AppendLine($"          if (__jl_{routeIndex} > __jsonBestLiterals)");
      sb.AppendLine("          {");
      sb.AppendLine($"            __jsonBestLiterals = __jl_{routeIndex};");
      sb.AppendLine($"            __jsonWinner = {routeIndex};");
      sb.AppendLine($"            __jsonWinnerAttempt = __ja_{routeIndex};");
      sb.AppendLine("            __jsonTied.Clear();");
      sb.AppendLine($"            __jsonTied.Add(\"{pattern}\");");
      sb.AppendLine("          }");
      sb.AppendLine($"          else if (__jl_{routeIndex} == __jsonBestLiterals)");
      sb.AppendLine("          {");
      sb.AppendLine($"            __jsonTied.Add(\"{pattern}\");");
      sb.AppendLine("          }");
      sb.AppendLine("        }");
      sb.AppendLine($"        else if (__jm_{routeIndex} && __jf_{routeIndex} is not null)");
      sb.AppendLine("        {");
      sb.AppendLine($"          __jsonFailures.Add(__jf_{routeIndex});");
      sb.AppendLine("        }");
    }

    sb.AppendLine();
    sb.AppendLine("        if (__jsonTied.Count > 1)");
    sb.AppendLine("        {");
    sb.AppendLine("          __jsonTied.Sort(global::System.StringComparer.Ordinal);");
    sb.AppendLine("          string __tieList = string.Join(\", \", __jsonTied.Select(static __p => \"'\" + __p + \"'\"));");
    sb.AppendLine("          await app.Terminal.WriteErrorLineAsync(\"Error: --json-args matches more than one route: \" + __tieList + \".\").ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        if (__jsonWinner < 0)");
    sb.AppendLine("        {");
    sb.AppendLine("          if (__jsonFailures.Count > 0)");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.Terminal.WriteErrorLineAsync(__jsonFailures[0]!).ConfigureAwait(false);");
    sb.AppendLine("          }");
    sb.AppendLine("          else");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.Terminal.WriteErrorLineAsync(\"Unknown command. Use --help for usage.\").ConfigureAwait(false);");
    sb.AppendLine("          }");
    sb.AppendLine();
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        switch (__jsonWinner)");
    sb.AppendLine("        {");
    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
    {
      EmitBindCase(sb, routes[routeIndex], routeIndex, app, methodSuffix, loggerFactoryFieldName);
    }

    sb.AppendLine("          default:");
    sb.AppendLine("            await app.Terminal.WriteErrorLineAsync(\"Unknown command. Use --help for usage.\").ConfigureAwait(false);");
    sb.AppendLine("            return 1;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
    sb.AppendLine("    }");
    sb.AppendLine();
  }

  private static void EmitCheckCall(
    StringBuilder sb,
    RouteDefinition route,
    List<Slot> slots,
    string pattern,
    int routeIndex,
    int attempt,
    IReadOnlyList<string> prefix,
    bool aliasMode,
    int indent)
  {
    string ind = new(' ', indent);
    sb.AppendLine($"{ind}__ok = true;");
    sb.AppendLine($"{ind}__lit = 0;");
    sb.AppendLine($"{ind}__litOk = true;");
    sb.AppendLine($"{ind}__fail = null;");
    sb.AppendLine($"{ind}{{");
    EmitAttemptBody(sb, route, slots, pattern, prefix, aliasMode, bind: false, indent + 2);
    sb.AppendLine($"{ind}}}");
    sb.AppendLine($"{ind}if (__ok && __litOk)");
    sb.AppendLine($"{ind}{{");
    sb.AppendLine($"{ind}  if (__lit > __best)");
    sb.AppendLine($"{ind}  {{");
    sb.AppendLine($"{ind}    __best = __lit;");
    sb.AppendLine($"{ind}    __bestAttempt = {attempt};");
    sb.AppendLine($"{ind}    __any = true;");
    sb.AppendLine($"{ind}  }}");
    sb.AppendLine($"{ind}}}");
    sb.AppendLine($"{ind}else if (__litOk)");
    sb.AppendLine($"{ind}{{");
    sb.AppendLine($"{ind}  __anyLit = true;");
    sb.AppendLine($"{ind}  __anyFail ??= __fail;");
    sb.AppendLine($"{ind}}}");
    _ = routeIndex;
  }

  private static void EmitBindCase(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    AppModel app,
    string methodSuffix,
    string? loggerFactoryFieldName)
  {
    List<Slot> slots = BuildSlots(route, routeIndex, app.CustomConverters);
    string pattern = EmitterStringUtils.EscapeForStringLiteral(route.FullPattern);
    List<string> aliases = route.Aliases.IsDefaultOrEmpty ? [] : [.. route.Aliases];
    sb.AppendLine($"          case {routeIndex}:");
    sb.AppendLine("          {");
    foreach (Slot slot in slots)
    {
      if (slot.VariableName is null)
        continue;

      string type = slot.DeclaredType ?? "string";
      if (slot.IsList)
      {
        sb.AppendLine($"            {type} {slot.VariableName} = [];");
      }
      else if (slot.IsFlag)
      {
        sb.AppendLine($"            {type} {slot.VariableName} = false;");
      }
      else
      {
        sb.AppendLine($"            {type} {slot.VariableName} = default!;");
      }
    }

    sb.AppendLine("            bool __ok;");
    sb.AppendLine("            int __lit;");
    sb.AppendLine("            bool __litOk;");
    sb.AppendLine("            string? __fail;");
    sb.AppendLine("            if (__jsonWinnerAttempt == 0)");
    sb.AppendLine("            {");
    sb.AppendLine("              __ok = true; __lit = 0; __litOk = true; __fail = null;");
    EmitAttemptBody(sb, route, slots, pattern, GroupWords(route), aliasMode: false, bind: true, indent: 14);
    sb.AppendLine("            }");
    for (int aliasIndex = 0; aliasIndex < aliases.Count; aliasIndex++)
    {
      string[] aliasWords = aliases[aliasIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
      sb.AppendLine($"            else if (__jsonWinnerAttempt == {aliasIndex + 1})");
      sb.AppendLine("            {");
      sb.AppendLine("              __ok = true; __lit = 0; __litOk = true; __fail = null;");
      EmitAttemptBody(sb, route, slots, pattern, aliasWords, aliasMode: true, bind: true, indent: 14);
      sb.AppendLine("            }");
    }

    sb.AppendLine("            else");
    sb.AppendLine("            {");
    sb.AppendLine("              __ok = false; __lit = 0; __litOk = false; __fail = \"Unknown command. Use --help for usage.\";");
    sb.AppendLine("            }");
    sb.AppendLine();
    sb.AppendLine("            if (!__ok)");
    sb.AppendLine("            {");
    sb.AppendLine("              await app.Terminal.WriteErrorLineAsync(__fail ?? \"Unknown command. Use --help for usage.\").ConfigureAwait(false);");
    sb.AppendLine("              return 1;");
    sb.AppendLine("            }");
    sb.AppendLine();
    sb.AppendLine("            _ = __lit;");
    EmitInvoke(sb, route, routeIndex, app, methodSuffix, loggerFactoryFieldName, indent: 12);
    sb.AppendLine("            return global::System.Environment.ExitCode;");
    sb.AppendLine("          }");
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
              string missing = $"\"Error: Missing required value '{Escape(slot.Key)}' for '{pattern}'.\"";
              sb.AppendLine($"{ind}    __ok = false;");
              sb.AppendLine($"{ind}    __fail ??= {missing};");
              if (reserved > 0)
              {
                sb.AppendLine($"{ind}    __litOk = false;");
              }
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
    sb.AppendLine($"{ind}  __fail ??= \"Error: Unexpected argument for '{pattern}'.\";");
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

  private static void EmitUnknownKeys(StringBuilder sb, List<Slot> slots, string pattern, int indent)
  {
    pattern = ForInterpolation(pattern);
    string ind = new(' ', indent);
    string known = slots.Count == 0
      ? "(none)"
      : string.Join(", ", slots.Select(s => s.Key).Distinct(StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal));
    known = ForInterpolation(Escape(known));
    sb.AppendLine($"{ind}if (__ok && __litOk)");
    sb.AppendLine($"{ind}{{");
    if (slots.Count == 0)
    {
      sb.AppendLine($"{ind}  foreach (global::System.Collections.Generic.KeyValuePair<string, global::System.Text.Json.JsonElement> __prop in __jsonMap)");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    __fail ??= $\"Error: Unknown key '{{__prop.Key}}' for '{pattern}'. Known names: {known}.\";");
      sb.AppendLine($"{ind}    break;");
      sb.AppendLine($"{ind}  }}");
    }
    else
    {
      string arms = string.Join(" or ", slots.Select(s => s.Key).Distinct(StringComparer.Ordinal).Select(k => $"\"{Escape(k)}\""));
      sb.AppendLine($"{ind}  foreach (global::System.Collections.Generic.KeyValuePair<string, global::System.Text.Json.JsonElement> __prop in __jsonMap)");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    if (__prop.Key is not ({arms}))");
      sb.AppendLine($"{ind}    {{");
      sb.AppendLine($"{ind}      __ok = false;");
      sb.AppendLine($"{ind}      __fail ??= $\"Error: Unknown key '{{__prop.Key}}' for '{pattern}'. Known names: {known}.\";");
      sb.AppendLine($"{ind}      break;");
      sb.AppendLine($"{ind}    }}");
      sb.AppendLine($"{ind}  }}");
    }

    sb.AppendLine($"{ind}}}");
  }

  private static void EmitKindGates(StringBuilder sb, List<Slot> slots, string pattern, int indent)
  {
    string ind = new(' ', indent);
    sb.AppendLine($"{ind}if (__ok && __litOk)");
    sb.AppendLine($"{ind}{{");
    foreach (Slot slot in slots)
    {
      sb.AppendLine($"{ind}  if (__ok && !__av_{slot.Id} && __jsonMap.TryGetValue(\"{Escape(slot.Key)}\", out global::System.Text.Json.JsonElement __el_{slot.Id}))");
      sb.AppendLine($"{ind}  {{");
      EmitKindGate(sb, slot, pattern, indent + 4);
      sb.AppendLine($"{ind}  }}");
    }

    sb.AppendLine($"{ind}}}");
  }

  private static void EmitKindGate(StringBuilder sb, Slot slot, string pattern, int indent)
  {
    pattern = ForInterpolation(pattern);
    string ind = new(' ', indent);
    string element = $"__el_{slot.Id}";
    string expected = Escape(slot.ExpectedType);
    string fail = $"$\"Error: Key '{Escape(slot.Key)}' for '{pattern}' has the wrong JSON type. Expected {expected}.\"";
    if (slot.IsNullable)
    {
      sb.AppendLine($"{ind}if ({element}.ValueKind == global::System.Text.Json.JsonValueKind.Null)");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      EmitKindGateCore(sb, slot, element, fail, indent + 2);
      sb.AppendLine($"{ind}}}");
    }
    else
    {
      sb.AppendLine($"{ind}if ({element}.ValueKind == global::System.Text.Json.JsonValueKind.Null)");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __ok = false;");
      sb.AppendLine($"{ind}  __fail ??= {fail};");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      EmitKindGateCore(sb, slot, element, fail, indent + 2);
      sb.AppendLine($"{ind}}}");
    }
  }

  private static void EmitKindGateCore(StringBuilder sb, Slot slot, string element, string fail, int indent)
  {
    string ind = new(' ', indent);
    switch (slot.Shape)
    {
      case ValueShape.Bool:
        sb.AppendLine($"{ind}if ({element}.ValueKind is not (global::System.Text.Json.JsonValueKind.True or global::System.Text.Json.JsonValueKind.False))");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  __fail ??= {fail};");
        sb.AppendLine($"{ind}}}");
        break;

      case ValueShape.Number:
        string tryGet = TryGetCall(slot, element, "global::System.Index _unused") ?? "false";
        sb.AppendLine($"{ind}if ({element}.ValueKind != global::System.Text.Json.JsonValueKind.Number || !{TryGetDiscard(slot, element)})");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  __fail ??= {fail};");
        sb.AppendLine($"{ind}}}");
        _ = tryGet;
        break;

      case ValueShape.Text:
      case ValueShape.Enum:
        sb.AppendLine($"{ind}if ({element}.ValueKind != global::System.Text.Json.JsonValueKind.String)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  __fail ??= {fail};");
        sb.AppendLine($"{ind}}}");
        if (slot.Shape == ValueShape.Enum && slot.EnumTypeName is not null)
        {
          sb.AppendLine($"{ind}else");
          sb.AppendLine($"{ind}{{");
          sb.AppendLine($"{ind}  global::TimeWarp.Nuru.EnumTypeConverter<{slot.EnumTypeName}> __enumGate_{slot.Id} = new();");
          sb.AppendLine($"{ind}  if (!__enumGate_{slot.Id}.TryConvert({element}.GetString(), out _))");
          sb.AppendLine($"{ind}  {{");
          sb.AppendLine($"{ind}    __ok = false;");
          sb.AppendLine($"{ind}    __fail ??= {fail} + \" \" + __enumGate_{slot.Id}.GetValidValuesMessage();");
          sb.AppendLine($"{ind}  }}");
          sb.AppendLine($"{ind}}}");
        }

        break;

      case ValueShape.TextList:
      case ValueShape.NumberList:
      case ValueShape.EnumList:
        sb.AppendLine($"{ind}if ({element}.ValueKind != global::System.Text.Json.JsonValueKind.Array)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  __fail ??= {fail};");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine($"{ind}else");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  foreach (global::System.Text.Json.JsonElement __item in {element}.EnumerateArray())");
        sb.AppendLine($"{ind}  {{");
        if (IsBoolName(slot.BaseConstraint))
        {
          sb.AppendLine($"{ind}    if (__item.ValueKind is not (global::System.Text.Json.JsonValueKind.True or global::System.Text.Json.JsonValueKind.False))");
        }
        else if (slot.Shape == ValueShape.TextList || slot.Shape == ValueShape.EnumList)
        {
          sb.AppendLine($"{ind}    if (__item.ValueKind != global::System.Text.Json.JsonValueKind.String)");
        }
        else
        {
          sb.AppendLine($"{ind}    if (__item.ValueKind != global::System.Text.Json.JsonValueKind.Number || !{TryGetDiscard(slot, "__item")})");
        }

        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}      __ok = false;");
        sb.AppendLine($"{ind}      __fail ??= {fail};");
        sb.AppendLine($"{ind}      break;");
        sb.AppendLine($"{ind}    }}");
        if (slot.Shape == ValueShape.EnumList && slot.EnumTypeName is not null)
        {
          sb.AppendLine($"{ind}    else");
          sb.AppendLine($"{ind}    {{");
          sb.AppendLine($"{ind}      global::TimeWarp.Nuru.EnumTypeConverter<{slot.EnumTypeName}> __enumItem_{slot.Id} = new();");
          sb.AppendLine($"{ind}      if (!__enumItem_{slot.Id}.TryConvert(__item.GetString(), out _))");
          sb.AppendLine($"{ind}      {{");
          sb.AppendLine($"{ind}        __ok = false;");
          sb.AppendLine($"{ind}        __fail ??= {fail} + \" \" + __enumItem_{slot.Id}.GetValidValuesMessage();");
          sb.AppendLine($"{ind}        break;");
          sb.AppendLine($"{ind}      }}");
          sb.AppendLine($"{ind}    }}");
        }

        sb.AppendLine($"{ind}  }}");
        sb.AppendLine($"{ind}}}");
        break;
    }
  }

  private static void EmitRequiredOptions(StringBuilder sb, List<Slot> slots, string pattern, int indent)
  {
    string ind = new(' ', indent);
    sb.AppendLine($"{ind}if (__ok && __litOk)");
    sb.AppendLine($"{ind}{{");
    foreach (Slot slot in slots.Where(s => s.IsOption))
    {
      if (slot.RequiredFlag)
      {
        sb.AppendLine($"{ind}  if (!__av_{slot.Id})");
        sb.AppendLine($"{ind}  {{");
        sb.AppendLine($"{ind}    if (!__jsonMap.TryGetValue(\"{Escape(slot.Key)}\", out global::System.Text.Json.JsonElement __flagEl_{slot.Id}) || __flagEl_{slot.Id}.ValueKind != global::System.Text.Json.JsonValueKind.True)");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}      __ok = false;");
        sb.AppendLine($"{ind}      __fail ??= \"Error: Missing required value '{Escape(slot.Key)}' for '{pattern}'.\";");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine($"{ind}  }}");
      }
      else if (slot.Required)
      {
        sb.AppendLine($"{ind}  if (!__av_{slot.Id} && !__jsonMap.ContainsKey(\"{Escape(slot.Key)}\"))");
        sb.AppendLine($"{ind}  {{");
        sb.AppendLine($"{ind}    __ok = false;");
        sb.AppendLine($"{ind}    __fail ??= \"Error: Missing required value '{Escape(slot.Key)}' for '{pattern}'.\";");
        sb.AppendLine($"{ind}  }}");
      }
    }

    sb.AppendLine($"{ind}}}");
  }

  private static void EmitFill(StringBuilder sb, Slot slot, string pattern, int indent)
  {
    if (slot.VariableName is null)
      return;

    pattern = ForInterpolation(pattern);
    string ind = new(' ', indent);
    string expected = Escape(slot.ExpectedType);
    string fail = $"$\"Error: Key '{Escape(slot.Key)}' for '{pattern}' has the wrong JSON type. Expected {expected}.\"";
    sb.AppendLine($"{ind}if (__av_{slot.Id})");
    sb.AppendLine($"{ind}{{");
    if (slot.IsList)
    {
      EmitAssignListFromStrings(sb, slot, $"__al_{slot.Id}", fail, indent + 2);
    }
    else if (slot.IsFlag)
    {
      sb.AppendLine($"{ind}  {slot.VariableName} = true;");
    }
    else
    {
      EmitAssignScalarFromString(sb, slot, $"__ar_{slot.Id}", fail, indent + 2);
    }

    sb.AppendLine($"{ind}}}");
    sb.AppendLine($"{ind}else if (__jsonMap.TryGetValue(\"{Escape(slot.Key)}\", out global::System.Text.Json.JsonElement __fill_{slot.Id}))");
    sb.AppendLine($"{ind}{{");
    EmitAssignFromJson(sb, slot, $"__fill_{slot.Id}", fail, indent + 2);
    sb.AppendLine($"{ind}}}");
  }

  private static void EmitAssignFromJson(StringBuilder sb, Slot slot, string element, string fail, int indent)
  {
    string ind = new(' ', indent);
    if (slot.IsNullable)
    {
      sb.AppendLine($"{ind}if ({element}.ValueKind == global::System.Text.Json.JsonValueKind.Null)");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  {slot.VariableName} = null;");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      EmitAssignFromJsonCore(sb, slot, element, fail, indent + 2);
      sb.AppendLine($"{ind}}}");
    }
    else
    {
      EmitAssignFromJsonCore(sb, slot, element, fail, indent);
    }
  }

  private static void EmitAssignFromJsonCore(StringBuilder sb, Slot slot, string element, string fail, int indent)
  {
    string ind = new(' ', indent);
    switch (slot.Shape)
    {
      case ValueShape.Bool:
        sb.AppendLine($"{ind}{slot.VariableName} = {element}.GetBoolean();");
        break;

      case ValueShape.Number:
        string clr = slot.NumberClr ?? "int";
        string call = TryGetCall(slot, element, $"{clr} __num_{slot.Id}") ?? "false";
        sb.AppendLine($"{ind}if (!{call})");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  __fail ??= {fail};");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine($"{ind}else");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  {slot.VariableName} = __num_{slot.Id};");
        sb.AppendLine($"{ind}}}");
        break;

      case ValueShape.Text:
      case ValueShape.Enum:
        sb.AppendLine($"{ind}string? __raw_{slot.Id} = {element}.GetString();");
        EmitAssignScalarFromString(sb, slot, $"__raw_{slot.Id}", fail, indent);
        break;

      case ValueShape.TextList:
      case ValueShape.NumberList:
      case ValueShape.EnumList:
        sb.AppendLine($"{ind}global::System.Collections.Generic.List<string> __items_{slot.Id} = new();");
        sb.AppendLine($"{ind}foreach (global::System.Text.Json.JsonElement __item in {element}.EnumerateArray())");
        sb.AppendLine($"{ind}{{");
        if (slot.Shape == ValueShape.NumberList)
        {
          string itemClr = slot.NumberClr ?? "int";
          sb.AppendLine($"{ind}  {itemClr} __itemNum;");
          string itemCall = TryGetCall(slot, "__item", "__itemNum") ?? "false";
          sb.AppendLine($"{ind}  if (!{itemCall})");
          sb.AppendLine($"{ind}  {{");
          sb.AppendLine($"{ind}    __ok = false;");
          sb.AppendLine($"{ind}    __fail ??= {fail};");
          sb.AppendLine($"{ind}    break;");
          sb.AppendLine($"{ind}  }}");
          sb.AppendLine($"{ind}  __items_{slot.Id}.Add(__itemNum.ToString(global::System.Globalization.CultureInfo.InvariantCulture));");
        }
        else
        {
          if (IsBoolName(slot.BaseConstraint))
          {
            sb.AppendLine($"{ind}  __items_{slot.Id}.Add(__item.ValueKind == global::System.Text.Json.JsonValueKind.True ? \"true\" : \"false\");");
          }
          else
          {
            sb.AppendLine($"{ind}  __items_{slot.Id}.Add(__item.GetString() ?? \"\");");
          }
        }

        sb.AppendLine($"{ind}}}");
        sb.AppendLine($"{ind}if (__ok)");
        sb.AppendLine($"{ind}{{");
        EmitAssignListFromStrings(sb, slot, $"__items_{slot.Id}", fail, indent + 2);
        sb.AppendLine($"{ind}}}");
        break;
    }
  }

  private static void EmitAssignScalarFromString(StringBuilder sb, Slot slot, string raw, string fail, int indent)
  {
    string ind = new(' ', indent);
    if (slot.Shape == ValueShape.Enum && slot.EnumTypeName is not null)
    {
      sb.AppendLine($"{ind}global::TimeWarp.Nuru.EnumTypeConverter<{slot.EnumTypeName}> __enum_{slot.Id} = new();");
      sb.AppendLine($"{ind}if (!__enum_{slot.Id}.TryConvert({raw}, out object? __enumObj_{slot.Id}))");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __ok = false;");
      sb.AppendLine($"{ind}  __fail ??= {fail} + \" \" + __enum_{slot.Id}.GetValidValuesMessage();");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  {slot.VariableName} = ({slot.EnumTypeName})__enumObj_{slot.Id}!;");
      sb.AppendLine($"{ind}}}");
      return;
    }

    if (slot.Converter is not null)
    {
      sb.AppendLine($"{ind}{slot.Converter.ConverterTypeName} __conv_{slot.Id} = new();");
      sb.AppendLine($"{ind}if ({raw} is null || !__conv_{slot.Id}.TryConvert({raw}, out object? __convObj_{slot.Id}))");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __ok = false;");
      sb.AppendLine($"{ind}  __fail ??= {fail};");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  {slot.VariableName} = ({slot.DeclaredType ?? slot.Converter.TargetTypeName})__convObj_{slot.Id}!;");
      sb.AppendLine($"{ind}}}");
      return;
    }

    if (slot.BaseConstraint is not null && IsSpecialParse(slot.BaseConstraint))
    {
      EmitSpecialParse(sb, slot, raw, fail, indent);
      return;
    }

    (string ClrType, string TryParseCondition)? builtin = slot.BaseConstraint is null
      ? null
      : TypeConversionMap.GetBuiltInTryConversion(slot.BaseConstraint, raw, $"__parsed_{slot.Id}");
    if (builtin is not null && slot.NumberClr is null && !IsPlainString(slot))
    {
      (string clr, string condition) = builtin.Value;
      sb.AppendLine($"{ind}{clr} __parsed_{slot.Id};");
      sb.AppendLine($"{ind}if ({raw} is null || !({condition}))");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __ok = false;");
      sb.AppendLine($"{ind}  __fail ??= {fail};");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  {slot.VariableName} = __parsed_{slot.Id};");
      sb.AppendLine($"{ind}}}");
      return;
    }

    if (slot.Shape == ValueShape.Number && slot.BaseConstraint is not null)
    {
      (string clr, string condition) = TypeConversionMap.GetBuiltInTryConversion(slot.BaseConstraint, raw, $"__parsed_{slot.Id}")!.Value;
      sb.AppendLine($"{ind}{clr} __parsed_{slot.Id};");
      sb.AppendLine($"{ind}if ({raw} is null || !({condition}))");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __ok = false;");
      sb.AppendLine($"{ind}  __fail ??= {fail};");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  {slot.VariableName} = __parsed_{slot.Id};");
      sb.AppendLine($"{ind}}}");
      return;
    }

    sb.AppendLine($"{ind}{slot.VariableName} = {raw}!;");
  }

  private static void EmitAssignListFromStrings(StringBuilder sb, Slot slot, string list, string fail, int indent)
  {
    string ind = new(' ', indent);
    if (slot.Shape == ValueShape.TextList && IsPlainString(slot))
    {
      sb.AppendLine($"{ind}{slot.VariableName} = [.. {list}];");
      return;
    }

    if (slot.Shape == ValueShape.EnumList && slot.EnumTypeName is not null)
    {
      sb.AppendLine($"{ind}global::TimeWarp.Nuru.EnumTypeConverter<{slot.EnumTypeName}> __enumList_{slot.Id} = new();");
      sb.AppendLine($"{ind}{slot.EnumTypeName}[] __enumArr_{slot.Id} = new {slot.EnumTypeName}[{list}.Count];");
      sb.AppendLine($"{ind}for (int __ei = 0; __ei < {list}.Count; __ei++)");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  if (!__enumList_{slot.Id}.TryConvert({list}[__ei], out object? __enumObj))");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    __fail ??= {fail} + \" \" + __enumList_{slot.Id}.GetValidValuesMessage();");
      sb.AppendLine($"{ind}    break;");
      sb.AppendLine($"{ind}  }}");
      sb.AppendLine($"{ind}  __enumArr_{slot.Id}[__ei] = ({slot.EnumTypeName})__enumObj!;");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}if (__ok) {slot.VariableName} = __enumArr_{slot.Id};");
      return;
    }

    if (slot.BaseConstraint is not null)
    {
      (string ClrType, string TryParseCondition)? typed = TypeConversionMap.GetBuiltInTryConversion(
        slot.BaseConstraint,
        $"{list}[__ni]",
        $"__parsed_{slot.Id}");
      if (typed is var (clr, condition))
      {
        sb.AppendLine($"{ind}{clr}[] __typed_{slot.Id} = new {clr}[{list}.Count];");
        sb.AppendLine($"{ind}for (int __ni = 0; __ni < {list}.Count; __ni++)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  {clr} __parsed_{slot.Id};");
        sb.AppendLine($"{ind}  if (!({condition}))");
        sb.AppendLine($"{ind}  {{");
        sb.AppendLine($"{ind}    __ok = false;");
        sb.AppendLine($"{ind}    __fail ??= {fail};");
        sb.AppendLine($"{ind}    break;");
        sb.AppendLine($"{ind}  }}");
        sb.AppendLine($"{ind}  __typed_{slot.Id}[__ni] = __parsed_{slot.Id};");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine($"{ind}if (__ok) {slot.VariableName} = __typed_{slot.Id};");
        return;
      }
    }

    if (slot.Shape == ValueShape.NumberList && slot.BaseConstraint is not null && slot.NumberClr is not null)
    {
      sb.AppendLine($"{ind}{slot.NumberClr}[] __numArr_{slot.Id} = new {slot.NumberClr}[{list}.Count];");
      sb.AppendLine($"{ind}for (int __ni = 0; __ni < {list}.Count; __ni++)");
      sb.AppendLine($"{ind}{{");
      (string clr, string condition) = TypeConversionMap.GetBuiltInTryConversion(slot.BaseConstraint, $"{list}[__ni]", $"__parsed_{slot.Id}")!.Value;
      _ = clr;
      sb.AppendLine($"{ind}  {slot.NumberClr} __parsed_{slot.Id};");
      sb.AppendLine($"{ind}  if (!({condition}))");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    __fail ??= {fail};");
      sb.AppendLine($"{ind}    break;");
      sb.AppendLine($"{ind}  }}");
      sb.AppendLine($"{ind}  __numArr_{slot.Id}[__ni] = __parsed_{slot.Id};");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}if (__ok) {slot.VariableName} = __numArr_{slot.Id};");
      return;
    }

    sb.AppendLine($"{ind}{slot.VariableName} = [.. {list}];");
  }

  private static void EmitSpecialParse(StringBuilder sb, Slot slot, string raw, string fail, int indent)
  {
    string ind = new(' ', indent);
    string constraint = slot.BaseConstraint!.ToLowerInvariant();
    sb.AppendLine($"{ind}if ({raw} is null)");
    sb.AppendLine($"{ind}{{");
    sb.AppendLine($"{ind}  __ok = false;");
    sb.AppendLine($"{ind}  __fail ??= {fail};");
    sb.AppendLine($"{ind}}}");
    sb.AppendLine($"{ind}else");
    sb.AppendLine($"{ind}{{");
    if (constraint == "uri")
    {
      sb.AppendLine($"{ind}  if (!global::System.Uri.TryCreate({raw}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? __uri_{slot.Id}) || __uri_{slot.Id} is null)");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    __fail ??= {fail};");
      sb.AppendLine($"{ind}  }}");
      sb.AppendLine($"{ind}  else");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    {slot.VariableName} = __uri_{slot.Id};");
      sb.AppendLine($"{ind}  }}");
    }
    else if (constraint == "fileinfo")
    {
      sb.AppendLine($"{ind}  try");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    {slot.VariableName} = new global::System.IO.FileInfo({raw});");
      sb.AppendLine($"{ind}  }}");
      sb.AppendLine($"{ind}  catch (global::System.Exception)");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    __fail ??= {fail};");
      sb.AppendLine($"{ind}  }}");
    }
    else
    {
      sb.AppendLine($"{ind}  try");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    {slot.VariableName} = new global::System.IO.DirectoryInfo({raw});");
      sb.AppendLine($"{ind}  }}");
      sb.AppendLine($"{ind}  catch (global::System.Exception)");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    __fail ??= {fail};");
      sb.AppendLine($"{ind}  }}");
    }

    sb.AppendLine($"{ind}}}");
  }

  private static void EmitInvoke(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    AppModel app,
    string methodSuffix,
    string? loggerFactoryFieldName,
    int indent)
  {
    string ind = new(' ', indent);
    ImmutableArray<BehaviorDefinition> behaviors = app.Behaviors.IsDefault ? [] : app.Behaviors;
    ImmutableArray<CustomConverterDefinition> converters = app.CustomConverters.IsDefault ? [] : app.CustomConverters;
    _ = converters;
    if (behaviors.Length > 0)
    {
      bool commandCreatedByBehavior = route.Handler.HandlerKind == HandlerKind.Command;
      BehaviorEmitter.EmitPipelineWrapper(
        sb,
        route,
        routeIndex,
        behaviors,
        app.Services,
        indent,
        () => HandlerInvokerEmitter.Emit(
          sb,
          route,
          routeIndex,
          app.Services,
          indent + 2,
          commandAlreadyCreated: commandCreatedByBehavior,
          loggerFactoryFieldName: loggerFactoryFieldName,
          useRuntimeDI: app.UseMicrosoftDependencyInjection,
          runtimeDISuffix: methodSuffix,
          httpClientConfigurations: app.HttpClientConfigurations));
    }
    else
    {
      HandlerInvokerEmitter.Emit(
        sb,
        route,
        routeIndex,
        app.Services,
        indent,
        loggerFactoryFieldName: loggerFactoryFieldName,
        useRuntimeDI: app.UseMicrosoftDependencyInjection,
        runtimeDISuffix: methodSuffix,
        httpClientConfigurations: app.HttpClientConfigurations);
    }

    _ = ind;
  }

  private static void EmitParse(StringBuilder sb)
  {
    sb.AppendLine("        if (fromRepl && __jsonArgsValue == \"-\")");
    sb.AppendLine("        {");
    sb.AppendLine("          await app.Terminal.WriteErrorLineAsync(\"Error: --json-args - is not available in the REPL. Use @path or a single-line '{\\\"…\\\"}'.\").ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        string __jsonText;");
    sb.AppendLine("        if (__jsonArgsValue == \"-\")");
    sb.AppendLine("        {");
    sb.AppendLine("          if (!global::System.Console.IsInputRedirected)");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.Terminal.WriteErrorLineAsync(\"Error: --json-args - requires redirected stdin.\").ConfigureAwait(false);");
    sb.AppendLine("            return 1;");
    sb.AppendLine("          }");
    sb.AppendLine();
    sb.AppendLine("          using global::System.IO.StreamReader __jsonReader = new(global::System.Console.OpenStandardInput(), global::System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);");
    sb.AppendLine("          __jsonText = await __jsonReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);");
    sb.AppendLine("          if (string.IsNullOrWhiteSpace(__jsonText))");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.Terminal.WriteErrorLineAsync(\"Error: --json-args - got empty stdin.\").ConfigureAwait(false);");
    sb.AppendLine("            return 1;");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("        else if (__jsonArgsValue.StartsWith(\"@\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("        {");
    sb.AppendLine("          string __jsonPath = __jsonArgsValue.Substring(1);");
    sb.AppendLine("          if (__jsonPath.Contains(\"://\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.Terminal.WriteErrorLineAsync(\"Error: --json-args @path does not accept URIs.\").ConfigureAwait(false);");
    sb.AppendLine("            return 1;");
    sb.AppendLine("          }");
    sb.AppendLine();
    sb.AppendLine("          if (__jsonPath == \"~\" || __jsonPath.StartsWith(\"~/\", global::System.StringComparison.Ordinal) || __jsonPath.StartsWith(\"~\\\\\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("          {");
    sb.AppendLine("            string __home = global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.UserProfile);");
    sb.AppendLine("            __jsonPath = __jsonPath == \"~\" ? __home : global::System.IO.Path.Combine(__home, __jsonPath.Substring(2));");
    sb.AppendLine("          }");
    sb.AppendLine();
    sb.AppendLine("          try");
    sb.AppendLine("          {");
    sb.AppendLine("            __jsonText = global::System.IO.File.ReadAllText(__jsonPath, global::System.Text.Encoding.UTF8);");
    sb.AppendLine("          }");
    sb.AppendLine("          catch (global::System.Exception)");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.Terminal.WriteErrorLineAsync(\"Error: --json-args could not read '\" + __jsonPath + \"'.\").ConfigureAwait(false);");
    sb.AppendLine("            return 1;");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("        else if (__jsonArgsValue.StartsWith(\"{\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("        {");
    sb.AppendLine("          __jsonText = __jsonArgsValue;");
    sb.AppendLine("        }");
    sb.AppendLine("        else");
    sb.AppendLine("        {");
    sb.AppendLine("          await app.Terminal.WriteErrorLineAsync(\"Error: --json-args value must be -, @path, or a JSON object.\").ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        try");
    sb.AppendLine("        {");
    sb.AppendLine("          __jsonDoc = global::System.Text.Json.JsonDocument.Parse(__jsonText);");
    sb.AppendLine("        }");
    sb.AppendLine("        catch (global::System.Text.Json.JsonException)");
    sb.AppendLine("        {");
    sb.AppendLine("          await app.Terminal.WriteErrorLineAsync(\"Error: --json-args value is not valid JSON.\").ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        if (__jsonDoc.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)");
    sb.AppendLine("        {");
    sb.AppendLine("          __jsonDoc.Dispose();");
    sb.AppendLine("          __jsonDoc = null;");
    sb.AppendLine("          await app.Terminal.WriteErrorLineAsync(\"Error: --json-args value must be a JSON object.\").ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
  }

  private static void EmitBuiltInPredicate(StringBuilder sb, AppModel app)
  {
    sb.AppendLine("    static bool __MatchesNoBodyBuiltIn(string[] routeArgs)");
    sb.AppendLine("    {");
    if (app.HasHelp)
    {
      sb.AppendLine("      if (routeArgs is [\"--help\" or \"-h\"]) return true;");
    }

    sb.AppendLine("      if (routeArgs is [\"--version\"]) return true;");
    sb.AppendLine("      if (routeArgs.Length == 5 && routeArgs[0] == \"--capabilities\" && (routeArgs[1] == \"--search\" || routeArgs[1] == \"-s\") && (routeArgs[3] == \"--group-filter\" || routeArgs[3] == \"-g\")) return true;");
    sb.AppendLine("      if (routeArgs.Length == 5 && routeArgs[0] == \"--capabilities\" && (routeArgs[1] == \"--group-filter\" || routeArgs[1] == \"-g\") && (routeArgs[3] == \"--search\" || routeArgs[3] == \"-s\")) return true;");
    sb.AppendLine("      if (routeArgs.Length == 3 && routeArgs[0] == \"--capabilities\" && (routeArgs[1] == \"--search\" || routeArgs[1] == \"-s\")) return true;");
    sb.AppendLine("      if (routeArgs.Length == 3 && routeArgs[0] == \"--capabilities\" && (routeArgs[1] == \"--group-filter\" || routeArgs[1] == \"-g\")) return true;");
    sb.AppendLine("      if (routeArgs is [\"--capabilities\"]) return true;");
    if (app.HasCheckUpdatesRoute)
    {
      sb.AppendLine("      if (routeArgs is [\"--check-updates\"]) return true;");
    }

    if (app.HasCompletion)
    {
      sb.AppendLine("      if (routeArgs.Length >= 2 && routeArgs[0] == \"__complete\" && int.TryParse(routeArgs[1], out _)) return true;");
      sb.AppendLine("      if (routeArgs is [\"--generate-completion\", _]) return true;");
      sb.AppendLine("      if (routeArgs is [\"--install-completion\", \"--dry-run\"]) return true;");
      sb.AppendLine("      if (routeArgs is [\"--install-completion\", \"--dry-run\", _]) return true;");
      sb.AppendLine("      if (routeArgs is [\"--install-completion\"]) return true;");
      sb.AppendLine("      if (routeArgs is [\"--install-completion\", _]) return true;");
    }

    sb.AppendLine("      return false;");
    sb.AppendLine("    }");
    sb.AppendLine();
  }

  private static List<Slot> BuildSlots(
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> converters)
  {
    if (converters.IsDefault)
      converters = [];

    List<Slot> slots = [];
    HashSet<string> ids = new(StringComparer.Ordinal);
    foreach (ParameterDefinition param in route.Parameters)
    {
      slots.Add(CreateParameterSlot(route, param, converters, ids));
    }

    foreach (OptionDefinition option in route.Options)
    {
      slots.Add(CreateOptionSlot(route, option, converters, ids));
    }

    AssignVariables(route, routeIndex, slots);
    return slots;
  }

  private static Slot CreateParameterSlot(
    RouteDefinition route,
    ParameterDefinition param,
    ImmutableArray<CustomConverterDefinition> converters,
    HashSet<string> ids)
  {
    _ = route;
    string? constraint = StripQuestion(param.TypeConstraint);
    bool nullable = (param.TypeConstraint?.EndsWith('?') ?? false);
    ValueShape shape = ShapeFor(constraint, isFlag: false, isList: param.IsCatchAll, isEnum: false, converters: converters);
    if (param.IsCatchAll && shape == ValueShape.Text)
      shape = ValueShape.TextList;

    CustomConverterDefinition? converter = FindConverter(constraint, converters);
    return new Slot
    {
      Key = param.Name,
      Id = AllocId(param.Name, ids),
      IsOption = false,
      IsFlag = false,
      IsList = param.IsCatchAll,
      IsCatchAll = param.IsCatchAll,
      Required = !param.IsOptional && !param.IsCatchAll,
      RequiredFlag = false,
      IsNullable = nullable,
      BaseConstraint = constraint,
      NumberClr = NumberClr(constraint),
      NumberKeyword = NumberClr(constraint),
      Converter = converter,
      Shape = shape,
      ExpectedType = Expected(constraint, shape, null),
      Parameter = param
    };
  }

  private static Slot CreateOptionSlot(
    RouteDefinition route,
    OptionDefinition option,
    ImmutableArray<CustomConverterDefinition> converters,
    HashSet<string> ids)
  {
    string key = option.LongForm ?? option.ShortForm ?? "option";
    string? constraint = StripQuestion(option.TypeConstraint);
    bool nullable = (option.TypeConstraint?.EndsWith('?') ?? false) || option.ParameterIsOptional;
    bool requiredFlag = option.IsFlag && !option.IsOptional && !option.IsFlagBound(route);
    bool isList = option.IsRepeated;
    ValueShape shape;
    if (option.IsFlag)
    {
      shape = ValueShape.Bool;
    }
    else
    {
      shape = ShapeFor(constraint, isFlag: false, isList: isList, isEnum: false, converters: converters);
      if (isList && shape == ValueShape.Text)
        shape = ValueShape.TextList;
      if (isList && shape == ValueShape.Number)
        shape = ValueShape.NumberList;
    }

    return new Slot
    {
      Key = key,
      Id = AllocId(key, ids),
      IsOption = true,
      IsFlag = option.IsFlag,
      IsList = isList,
      IsCatchAll = false,
      Required = !option.IsFlag && !option.IsOptional && !option.ParameterIsOptional,
      RequiredFlag = requiredFlag,
      IsNullable = nullable && !option.IsFlag,
      BaseConstraint = option.IsFlag ? "bool" : constraint,
      NumberClr = NumberClr(constraint),
      NumberKeyword = NumberClr(constraint),
      Converter = FindConverter(constraint, converters),
      Option = option,
      Shape = shape,
      ExpectedType = option.IsFlag ? "bool" : Expected(constraint, shape, null)
    };
  }

  private static void AssignVariables(RouteDefinition route, int routeIndex, List<Slot> slots)
  {
    if (route.Handler.Parameters.IsDefaultOrEmpty)
      return;

    List<ParameterDefinition> routeParams = [.. route.Parameters];
    int routeParamIndex = 0;
    foreach (ParameterBinding param in route.Handler.Parameters)
    {
      if (param.Source is BindingSource.Service or BindingSource.CancellationToken)
        continue;

      Slot? slot = null;
      ParameterDefinition? routeParam = null;
      OptionDefinition? option = null;
      if (param.Source is BindingSource.Parameter or BindingSource.CatchAll)
      {
        if (routeParamIndex < routeParams.Count)
        {
          routeParam = routeParams[routeParamIndex];
          slot = slots.FirstOrDefault(s => !s.IsOption && ReferenceEquals(s.Parameter, routeParam));
          routeParamIndex++;
        }
      }
      else if (param.Source is BindingSource.Option or BindingSource.Flag)
      {
        slot = slots.FirstOrDefault(s =>
          s.IsOption &&
          (string.Equals(s.Key, param.SourceName, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(s.Option?.ShortForm, param.SourceName, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(s.Option?.ParameterName, param.ParameterName, StringComparison.OrdinalIgnoreCase)));
        option = slot?.Option;
      }

      if (slot is null)
        continue;

      string variable = CaptureName(route, routeIndex, param, routeParam, option);
      bool isEnum = param.IsEnumType;
      string? enumType = isEnum ? StripQuestion(param.ParameterTypeName) : slot.EnumTypeName;
      if (isEnum && slot.IsList)
      {
        enumType = StripQuestion(UnwrapElement(param.ParameterTypeName ?? string.Empty));
      }

      ValueShape shape = slot.Shape;
      if (isEnum)
      {
        shape = slot.IsList ? ValueShape.EnumList : ValueShape.Enum;
      }

      bool nullable = slot.IsNullable || (param.ParameterTypeName?.EndsWith('?') ?? false);
      string? declared = param.ParameterTypeName ?? slot.DeclaredType ?? "string";
      string? constraint = slot.BaseConstraint;
      if (isEnum)
        constraint ??= enumType;

      slots[slots.IndexOf(slot)] = new Slot
      {
        Key = slot.Key,
        Id = slot.Id,
        IsOption = slot.IsOption,
        IsFlag = slot.IsFlag || param.Source == BindingSource.Flag,
        IsList = slot.IsList || param.IsArray,
        IsCatchAll = slot.IsCatchAll || param.Source == BindingSource.CatchAll,
        Required = slot.Required,
        RequiredFlag = slot.RequiredFlag,
        IsNullable = nullable,
        BaseConstraint = constraint,
        DeclaredType = declared,
        VariableName = variable,
        EnumTypeName = enumType,
        NumberClr = slot.NumberClr ?? NumberClr(StripQuestion(declared)),
        NumberKeyword = slot.NumberKeyword ?? NumberClr(StripQuestion(declared)),
        Converter = slot.Converter ?? FindConverter(StripQuestion(declared), []),
        Option = slot.Option,
        Shape = shape,
        ExpectedType = isEnum ? (enumType ?? "enum") : slot.ExpectedType,
        Parameter = slot.Parameter
      };
    }
  }

  private static string CaptureName(
    RouteDefinition route,
    int routeIndex,
    ParameterBinding param,
    ParameterDefinition? routeParam,
    OptionDefinition? option)
  {
    if (route.Handler.HandlerKind == HandlerKind.Method)
      return CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName);

    if (route.Handler.HandlerKind == HandlerKind.Command)
    {
      return param.Source switch
      {
        BindingSource.CatchAll => $"__{CSharpIdentifierUtils.ToCamelCase(param.ParameterName)}_{routeIndex}",
        BindingSource.Flag => CSharpIdentifierUtils.EscapeIfKeyword(CSharpIdentifierUtils.ToCamelCase(param.SourceName)),
        BindingSource.Option => CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName.ToLowerInvariant()),
        _ => CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName.ToLowerInvariant())
      };
    }

    if (param.Source is BindingSource.Parameter or BindingSource.CatchAll)
    {
      if (routeParam is null)
        return CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName);

      bool isStringTypedCatchAll = routeParam.IsCatchAll
        && routeParam.HasTypeConstraint
        && routeParam.TypeConstraint!.Equals("string", StringComparison.OrdinalIgnoreCase);
      if (routeParam.IsCatchAll && (!routeParam.HasTypeConstraint || isStringTypedCatchAll))
        return $"__{routeParam.CamelCaseName}_{routeIndex}";

      return CSharpIdentifierUtils.EscapeIfKeyword(routeParam.CamelCaseName);
    }

    if (option is not null)
    {
      string optVarName = option.ExpectsValue
        ? CSharpIdentifierUtils.ToCamelCase(option.ParameterName ?? option.LongForm ?? option.ShortForm ?? param.ParameterName)
        : CSharpIdentifierUtils.ToCamelCase(option.LongForm ?? option.ShortForm ?? param.ParameterName);
      return CSharpIdentifierUtils.EscapeIfKeyword(optVarName);
    }

    return CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName);
  }

  private static ValueShape ShapeFor(
    string? constraint,
    bool isFlag,
    bool isList,
    bool isEnum,
    ImmutableArray<CustomConverterDefinition> converters)
  {
    _ = converters;
    if (isFlag || IsBoolName(constraint))
      return isList ? ValueShape.TextList : ValueShape.Bool;

    if (isEnum)
      return isList ? ValueShape.EnumList : ValueShape.Enum;

    if (NumberClr(constraint) is not null)
      return isList ? ValueShape.NumberList : ValueShape.Number;

    return isList ? ValueShape.TextList : ValueShape.Text;
  }

  private static string Expected(string? constraint, ValueShape shape, string? enumType)
  {
    if (shape is ValueShape.Enum or ValueShape.EnumList)
      return enumType ?? constraint ?? "enum";

    if (shape == ValueShape.Bool)
      return "bool";

    if (shape is ValueShape.Number or ValueShape.NumberList)
      return constraint ?? "number";

    if (shape is ValueShape.TextList)
      return constraint is null ? "string array" : constraint + " array";

    return constraint ?? "string";
  }

  private static bool IsPlainString(Slot slot)
  {
    if (slot.Shape is not (ValueShape.Text or ValueShape.TextList))
      return false;

    if (slot.Converter is not null || slot.EnumTypeName is not null)
      return false;

    if (slot.BaseConstraint?.Equals("string", StringComparison.OrdinalIgnoreCase) == false)
      return false;

    return true;
  }

  private static bool IsSpecialParse(string constraint)
  {
    return constraint.ToLowerInvariant() is "uri" or "fileinfo" or "directoryinfo";
  }

  private static bool IsBoolName(string? name)
  {
    if (name is null)
      return false;

    string trimmed = StripQuestion(name) ?? name;
    return trimmed.Equals("bool", StringComparison.OrdinalIgnoreCase)
      || trimmed.Equals("boolean", StringComparison.OrdinalIgnoreCase)
      || trimmed.EndsWith("System.Boolean", StringComparison.Ordinal);
  }

  private static string? NumberClr(string? constraint)
  {
    if (constraint is null)
      return null;

    return constraint.ToLowerInvariant() switch
    {
      "int" or "int32" => "int",
      "long" or "int64" => "long",
      "short" or "int16" => "short",
      "byte" => "byte",
      "sbyte" => "sbyte",
      "ushort" or "uint16" => "ushort",
      "uint" or "uint32" => "uint",
      "ulong" or "uint64" => "ulong",
      "float" or "single" => "float",
      "double" => "double",
      "decimal" => "decimal",
      _ => null
    };
  }

  private static string? TryGetCall(Slot slot, string element, string outDecl)
  {
    string? keyword = slot.NumberKeyword;
    if (keyword is null)
      return null;

    string method = keyword switch
    {
      "int" => "TryGetInt32",
      "long" => "TryGetInt64",
      "short" => "TryGetInt16",
      "byte" => "TryGetByte",
      "sbyte" => "TryGetSByte",
      "ushort" => "TryGetUInt16",
      "uint" => "TryGetUInt32",
      "ulong" => "TryGetUInt64",
      "float" => "TryGetSingle",
      "double" => "TryGetDouble",
      "decimal" => "TryGetDecimal",
      _ => "TryGetDouble"
    };
    return $"{element}.{method}(out {outDecl})";
  }

  private static string TryGetDiscard(Slot slot, string element)
  {
    string? keyword = slot.NumberKeyword ?? "int";
    string method = keyword switch
    {
      "int" => "TryGetInt32",
      "long" => "TryGetInt64",
      "short" => "TryGetInt16",
      "byte" => "TryGetByte",
      "sbyte" => "TryGetSByte",
      "ushort" => "TryGetUInt16",
      "uint" => "TryGetUInt32",
      "ulong" => "TryGetUInt64",
      "float" => "TryGetSingle",
      "double" => "TryGetDouble",
      "decimal" => "TryGetDecimal",
      _ => "TryGetInt32"
    };
    return $"{element}.{method}(out _)";
  }

  private static CustomConverterDefinition? FindConverter(string? constraint, ImmutableArray<CustomConverterDefinition> converters)
  {
    if (constraint is null || converters.IsDefaultOrEmpty)
      return null;

    return converters.FirstOrDefault(c =>
      string.Equals(c.TargetTypeName, constraint, StringComparison.OrdinalIgnoreCase) ||
      string.Equals(SimpleName(c.TargetTypeName), constraint, StringComparison.OrdinalIgnoreCase) ||
      string.Equals(c.ConstraintAlias, constraint, StringComparison.OrdinalIgnoreCase) ||
      string.Equals(SimpleName(c.ConverterTypeName).EndsWith("Converter", StringComparison.Ordinal)
        ? SimpleName(c.ConverterTypeName)[..^"Converter".Length]
        : SimpleName(c.ConverterTypeName), constraint, StringComparison.OrdinalIgnoreCase));
  }

  private static string SimpleName(string typeName)
  {
    const string globalPrefix = "global::";
    if (typeName.StartsWith(globalPrefix, StringComparison.Ordinal))
      typeName = typeName[globalPrefix.Length..];

    int dot = typeName.LastIndexOf('.');
    return dot >= 0 ? typeName[(dot + 1)..] : typeName;
  }

  private static string? StripQuestion(string? typeName)
  {
    if (typeName is null)
      return null;

    return typeName.EndsWith('?') ? typeName[..^1] : typeName;
  }

  private static string UnwrapElement(string typeName)
  {
    string name = StripQuestion(typeName) ?? typeName;
    if (name.EndsWith("[]", StringComparison.Ordinal))
      return name[..^2];

    int start = name.IndexOf('<', StringComparison.Ordinal);
    int end = name.LastIndexOf('>');
    if (start >= 0 && end > start)
      return name[(start + 1)..end];

    return name;
  }

  private static string AllocId(string key, HashSet<string> ids)
  {
    StringBuilder sb = new();
    foreach (char c in key)
    {
      sb.Append(char.IsLetterOrDigit(c) ? c : '_');
    }

    if (sb.Length == 0 || char.IsDigit(sb[0]))
      sb.Insert(0, '_');

    string id = sb.ToString();
    if (ids.Add(id))
      return id;

    int n = 2;
    while (!ids.Add(id + "_" + n))
      n++;

    return id + "_" + n;
  }

  private static string[] GroupWords(RouteDefinition route)
  {
    if (string.IsNullOrEmpty(route.GroupPrefix))
      return [];

    return route.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);
  }

  private static int ReservedAfter(List<SegmentDefinition> segments, int index, bool aliasMode)
  {
    int count = 0;
    for (int i = index + 1; i < segments.Count; i++)
    {
      if (!aliasMode && segments[i] is LiteralDefinition)
        count++;

      if (segments[i] is EndOfOptionsSeparatorDefinition)
        count++;
    }

    return count;
  }

  private static string FlagCondition(OptionDefinition option)
  {
    string? longCheck = option.LongForm is not null
      ? $"routeArgs[__i] == \"--{Escape(option.LongForm)}\""
      : null;
    string? shortCheck = option.ShortForm is not null
      ? $"routeArgs[__i] == \"-{Escape(option.ShortForm)}\""
      : null;
    return (longCheck, shortCheck) switch
    {
      (not null, not null) => $"{longCheck} || {shortCheck}",
      (not null, null) => longCheck,
      (null, not null) => shortCheck!,
      _ => "false"
    };
  }

  private static string Escape(string value) => EmitterStringUtils.EscapeForStringLiteral(value);

  // Pattern text is embedded in generated interpolated strings. Doubling braces keeps `{env}` literal.
  private static string ForInterpolation(string alreadyEscaped) =>
    alreadyEscaped.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);
}
