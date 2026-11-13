#region Purpose
// Emits --json-args peel, parse, candidate selection, and reflection-free binding. The path runs only
// when a JSON object was loaded and never uses ComputedSpecificity to break ties.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>Generates the <c>--json-args</c> preamble and the JSON route-selection path.</summary>
internal static partial class JsonArgsEmitter
{
  private enum ValueShape
  {
    Text,
    Bool,
    Number,
    Enum,
    TextList,
    NumberList,
    EnumList,
    Json
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
  /// One bind failure site: the <c>NuruBindErrorKind</c> member, the key expression, and the message expression.
  /// </summary>
  private readonly record struct Fail(string Kind, string KeyExpression, string Message)
  {
    /// <summary>The statement that records this failure unless an earlier one is recorded.</summary>
    /// <param name="suffix">Expression appended to the message, or null.</param>
    public string Assign(string? suffix = null)
    {
      string message = suffix is null ? Message : $"{Message} + {suffix}";
      return $"__fail ??= new global::TimeWarp.Nuru.NuruBindError(global::TimeWarp.Nuru.NuruBindErrorKind.{Kind}, {KeyExpression}, {message});";
    }
  }

  private static string KeyLiteral(string key) => $"\"{Escape(key)}\"";

  private static Fail MissingValue(Slot slot, string pattern) =>
    new("MissingValue", KeyLiteral(slot.Key), $"\"Error: Missing required value '{Escape(slot.Key)}' for '{pattern}'.\"");

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
    sb.AppendLine("        await app.WriteBindErrorAsync(\"Error: --json-args must be a separate token. Use --json-args -, --json-args @path, or --json-args '{...}'.\", null, null).ConfigureAwait(false);");
    sb.AppendLine("        return 1;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      if (string.Equals(__jarg, \"--json-args\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("      {");
    sb.AppendLine("        if (__jsonArgsSeen)");
    sb.AppendLine("        {");
    sb.AppendLine("          await app.WriteBindErrorAsync(\"Error: --json-args was specified more than once.\", null, null).ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        __jsonArgsSeen = true;");
    sb.AppendLine("        if (__ji + 1 >= args.Length)");
    sb.AppendLine("        {");
    sb.AppendLine("          await app.WriteBindErrorAsync(\"Error: --json-args requires a value.\", null, null).ConfigureAwait(false);");
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
    string? loggerFactoryFieldName,
    bool hasGeneratedMediator,
    bool forwardRoutes = false)
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
    sb.AppendLine("        global::System.Collections.Generic.List<global::TimeWarp.Nuru.NuruBindError> __jsonFailures = new();");

    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
    {
      RouteDefinition route = routes[routeIndex];
      List<Slot> slots = BuildSlots(route, routeIndex, app.CustomConverters);
      string pattern = EmitterStringUtils.EscapeForStringLiteral(route.FullPattern);
      sb.AppendLine();
      sb.AppendLine($"        bool __TryJson_{routeIndex}(out int literalCount, out int attempt, out bool literalsMatched, out global::TimeWarp.Nuru.NuruBindError? failure)");
      sb.AppendLine("        {");
      sb.AppendLine("          literalCount = 0;");
      sb.AppendLine("          attempt = 0;");
      sb.AppendLine("          literalsMatched = false;");
      sb.AppendLine("          failure = null;");
      sb.AppendLine("          bool __ok;");
      sb.AppendLine("          int __lit;");
      sb.AppendLine("          bool __litOk;");
      sb.AppendLine("          global::TimeWarp.Nuru.NuruBindError? __fail;");
      sb.AppendLine("          int __best = -1;");
      sb.AppendLine("          int __bestAttempt = 0;");
      sb.AppendLine("          bool __any = false;");
      sb.AppendLine("          bool __anyLit = false;");
      sb.AppendLine("          global::TimeWarp.Nuru.NuruBindError? __anyFail = null;");

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
      sb.AppendLine($"        if (__TryJson_{routeIndex}(out int __jl_{routeIndex}, out int __ja_{routeIndex}, out bool __jm_{routeIndex}, out global::TimeWarp.Nuru.NuruBindError? __jf_{routeIndex}))");
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
    sb.AppendLine("          await app.WriteBindErrorAsync(\"Error: --json-args matches more than one route: \" + __tieList + \".\", null, null).ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        if (__jsonWinner < 0)");
    sb.AppendLine("        {");
    sb.AppendLine("          if (__jsonFailures.Count > 0)");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.WriteBindErrorAsync(__jsonFailures[0]).ConfigureAwait(false);");
    sb.AppendLine("          }");
    sb.AppendLine("          else");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.Terminal.WriteErrorLineAsync(\"Unknown command. Use --help for usage.\").ConfigureAwait(false);");
    sb.AppendLine("          }");
    sb.AppendLine();
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    if (forwardRoutes)
    {
      // AddServeRoute(): offer the selected route to the forwarder with the resolved JSON object, so
      // the server never reads this process's stdin or resolves @path from its own directory.
      sb.AppendLine("        string?[] __forwardRoutes =");
      sb.AppendLine("        [");
      foreach (RouteDefinition route in routes)
      {
        sb.AppendLine(route.IsLocalOnly
          ? "          null,"
          : $"          \"{EmitterStringUtils.EscapeForStringLiteral(route.FullPattern)}\",");
      }

      sb.AppendLine("        ];");
      sb.AppendLine("        if (!fromRepl && __forwardRoutes[__jsonWinner] is string __forwardRoute");
      sb.AppendLine("          && await global::TimeWarp.Nuru.NuruForwarding.TryForwardAsync(app, __forwardRoute, args, __jsonDoc.RootElement.GetRawText(), __forwardMode, cancellationToken).ConfigureAwait(false) is int __jsonForwardedExit)");
      sb.AppendLine("        {");
      sb.AppendLine("          return __jsonForwardedExit;");
      sb.AppendLine("        }");
      sb.AppendLine();
    }

    sb.AppendLine("        switch (__jsonWinner)");
    sb.AppendLine("        {");
    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
    {
      EmitBindCase(sb, routes[routeIndex], routeIndex, app, methodSuffix, loggerFactoryFieldName, hasGeneratedMediator);
    }

    sb.AppendLine("          default:");
    sb.AppendLine("            await app.Terminal.WriteErrorLineAsync(\"Unknown command. Use --help for usage.\").ConfigureAwait(false);");
    sb.AppendLine("            return 1;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
    sb.AppendLine("    }");
    sb.AppendLine();
  }
}
