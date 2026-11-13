#region Purpose
// Emits the JSON bound classes, bind methods, and TryBind dispatcher.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class JsonArgsEmitter
{
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
    string? loggerFactoryFieldName,
    bool hasGeneratedMediator)
  {
    string bound = BoundClassName(methodSuffix, routeIndex);
    sb.AppendLine($"          case {routeIndex}:");
    sb.AppendLine("          {");
    sb.AppendLine($"            if (!{BindMethodName(methodSuffix, routeIndex)}(routeArgs, __jsonMap, __jsonWinnerAttempt, out {bound}? __bound, out global::TimeWarp.Nuru.NuruBindError? __bindFail) || __bound is null)");
    sb.AppendLine("            {");
    sb.AppendLine("              await (__bindFail is not null ? app.WriteBindErrorAsync(__bindFail) : app.Terminal.WriteErrorLineAsync(\"Unknown command. Use --help for usage.\")).ConfigureAwait(false);");
    sb.AppendLine("              return 1;");
    sb.AppendLine("            }");
    sb.AppendLine();
    EmitUnpackAndInvoke(sb, route, routeIndex, app, methodSuffix, loggerFactoryFieldName, hasGeneratedMediator, "__bound", captureResultInto: null, indent: 12);
    sb.AppendLine("            return global::TimeWarp.Nuru.NuruCallContext.CurrentExitCode;");
    sb.AppendLine("          }");
  }

  /// <summary>
  /// Copies the bound values into the locals the invoker reads, then runs the missing-handler
  /// check, the Nuru behaviors and the handler.
  /// </summary>
  private static void EmitUnpackAndInvoke(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    AppModel app,
    string methodSuffix,
    string? loggerFactoryFieldName,
    bool hasGeneratedMediator,
    string boundVariable,
    string? captureResultInto,
    int indent)
  {
    string ind = new(' ', indent);
    List<Slot> slots = BuildSlots(route, routeIndex, app.CustomConverters);
    foreach (Slot slot in slots)
    {
      if (slot.VariableName is not null)
        sb.AppendLine($"{ind}{slot.DeclaredType ?? "string"} {slot.VariableName} = {boundVariable}.{slot.VariableName};");
    }

    foreach (Slot slot in slots)
    {
      if (slot.Option is not null && NeedsJsonSuppliedFlag(route, slot.Option))
        sb.AppendLine($"{ind}bool __supplied_{slot.Id} = {boundVariable}.__supplied_{slot.Id};");
    }

    InterceptorEmitter.EmitMissingHandlerOnDispatch(
      sb,
      route,
      app.UseMicrosoftDependencyInjection,
      hasGeneratedMediator,
      ind);
    InterceptorEmitter.EmitExclusiveGate(sb, route, ind);
    EmitInvoke(sb, route, routeIndex, app, methodSuffix, loggerFactoryFieldName, hasGeneratedMediator, indent, captureResultInto);
  }

  /// <summary>
  /// Runs a request bound by <c>NuruApp.TryBind</c> (or renders its captured result) inside
  /// ExecuteRouteAsync, after configuration, services and the command scope are set up.
  /// Always returns from the generated method on this path.
  /// </summary>
  public static void EmitInvocationDispatch(
    StringBuilder sb,
    IReadOnlyList<RouteDefinition> routes,
    AppModel app,
    string methodSuffix,
    string? loggerFactoryFieldName,
    bool hasGeneratedMediator)
  {
    sb.AppendLine("    if (__invocation is not null)");
    sb.AppendLine("    {");
    sb.AppendLine("      switch (__invocation.Request)");
    sb.AppendLine("      {");
    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
    {
      RouteDefinition route = routes[routeIndex];
      sb.AppendLine($"        case {BoundClassName(methodSuffix, routeIndex)} __bound:");
      sb.AppendLine("        {");
      sb.AppendLine("          if (__invocation.RenderTerminal is not null)");
      sb.AppendLine("          {");
      HandlerInvokerEmitter.EmitRender(
        sb,
        route,
        "__invocation.Result",
        "__invocation.RenderTerminal",
        app.Services,
        app.HttpClientConfigurations,
        loggerFactoryFieldName,
        app.UseMicrosoftDependencyInjection,
        methodSuffix,
        "            ",
        app.VerbatimRendererOutput);
      sb.AppendLine("            return global::TimeWarp.Nuru.NuruCallContext.CurrentExitCode;");
      sb.AppendLine("          }");
      sb.AppendLine();
      sb.AppendLine("          {");
      EmitUnpackAndInvoke(sb, route, routeIndex, app, methodSuffix, loggerFactoryFieldName, hasGeneratedMediator, "__bound", captureResultInto: "__invocation.Result", indent: 12);
      sb.AppendLine("            __invocation.Dispatched = true;");
      sb.AppendLine("            return global::TimeWarp.Nuru.NuruCallContext.CurrentExitCode;");
      sb.AppendLine("          }");
      sb.AppendLine("        }");
    }

    sb.AppendLine("        default:");
    sb.AppendLine("          throw new global::System.ArgumentException(\"The request was not bound by this NuruApp.\", \"request\");");
    sb.AppendLine("      }");
    sb.AppendLine("    }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits, at class level, one bound-request class and one bind method per route, and the
  /// <c>TryBind</c> entry point. <c>--json-args</c> and <c>NuruApp.TryBind</c> both call the bind
  /// method, so they share one binder.
  /// </summary>
  public static void EmitBindMethods(StringBuilder sb, IReadOnlyList<RouteDefinition> routes, AppModel app, string methodSuffix)
  {
    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
    {
      EmitBoundClass(sb, routes[routeIndex], routeIndex, app, methodSuffix);
      EmitBindMethod(sb, routes[routeIndex], routeIndex, app, methodSuffix);
    }

    EmitTryBind(sb, routes, methodSuffix);
  }

  private static string BoundClassName(string methodSuffix, int routeIndex) => $"__JsonBound{methodSuffix}_{routeIndex}";

  private static string BindMethodName(string methodSuffix, int routeIndex) => $"__JsonBind{methodSuffix}_{routeIndex}";

  /// <summary>Name of the generated <c>TryBind</c> entry point that <c>NuruApp.JsonBinder</c> points to.</summary>
  internal static string TryBindMethodName(string methodSuffix) => $"__TryBindJson{methodSuffix}";

  private static void EmitBoundClass(StringBuilder sb, RouteDefinition route, int routeIndex, AppModel app, string methodSuffix)
  {
    List<Slot> slots = BuildSlots(route, routeIndex, app.CustomConverters);
    string name = BoundClassName(methodSuffix, routeIndex);
    string pattern = Escape(route.FullPattern);
    sb.AppendLine($"  private sealed class {name} : global::TimeWarp.Nuru.NuruBoundRequest");
    sb.AppendLine("  {");
    sb.AppendLine($"    public {name}() : base(\"{pattern}\") {{ }}");
    foreach (Slot slot in slots)
    {
      if (slot.VariableName is not null)
        sb.AppendLine($"    public {slot.DeclaredType ?? "string"} {slot.VariableName} = default!;");
    }

    foreach (Slot slot in slots)
    {
      if (slot.Option is not null && NeedsJsonSuppliedFlag(route, slot.Option))
        sb.AppendLine($"    public bool __supplied_{slot.Id};");
    }

    sb.AppendLine("  }");
    sb.AppendLine();
  }

  private static void EmitBindMethod(StringBuilder sb, RouteDefinition route, int routeIndex, AppModel app, string methodSuffix)
  {
    List<Slot> slots = BuildSlots(route, routeIndex, app.CustomConverters);
    string pattern = Escape(route.FullPattern);
    string bound = BoundClassName(methodSuffix, routeIndex);
    List<string> aliases = route.Aliases.IsDefaultOrEmpty ? [] : [.. route.Aliases];
    sb.AppendLine($"  private static bool {BindMethodName(methodSuffix, routeIndex)}(");
    sb.AppendLine("    string[] routeArgs,");
    sb.AppendLine("    global::System.Collections.Generic.Dictionary<string, global::System.Text.Json.JsonElement> __jsonMap,");
    sb.AppendLine("    int __jsonWinnerAttempt,");
    sb.AppendLine($"    out {bound}? __boundOut,");
    sb.AppendLine("    out global::TimeWarp.Nuru.NuruBindError? __failOut)");
    sb.AppendLine("  {");
    foreach (Slot slot in slots)
    {
      if (slot.VariableName is null)
        continue;

      string type = slot.DeclaredType ?? "string";
      if (slot.IsList)
      {
        sb.AppendLine($"    {type} {slot.VariableName} = [];");
      }
      else if (slot.IsFlag)
      {
        sb.AppendLine($"    {type} {slot.VariableName} = false;");
      }
      else
      {
        sb.AppendLine($"    {type} {slot.VariableName} = {DefaultInitializer(slot)};");
      }
    }

    foreach (Slot slot in slots)
    {
      if (slot.Option is not null && NeedsJsonSuppliedFlag(route, slot.Option))
        sb.AppendLine($"    bool __supplied_{slot.Id} = false;");
    }

    sb.AppendLine("    bool __ok;");
    sb.AppendLine("    int __lit;");
    sb.AppendLine("    bool __litOk;");
    sb.AppendLine("    global::TimeWarp.Nuru.NuruBindError? __fail;");
    sb.AppendLine("    if (__jsonWinnerAttempt == 0)");
    sb.AppendLine("    {");
    sb.AppendLine("      __ok = true; __lit = 0; __litOk = true; __fail = null;");
    EmitAttemptBody(sb, route, slots, pattern, GroupWords(route), aliasMode: false, bind: true, indent: 6);
    sb.AppendLine("    }");
    for (int aliasIndex = 0; aliasIndex < aliases.Count; aliasIndex++)
    {
      string[] aliasWords = aliases[aliasIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
      sb.AppendLine($"    else if (__jsonWinnerAttempt == {aliasIndex + 1})");
      sb.AppendLine("    {");
      sb.AppendLine("      __ok = true; __lit = 0; __litOk = true; __fail = null;");
      EmitAttemptBody(sb, route, slots, pattern, aliasWords, aliasMode: true, bind: true, indent: 6);
      sb.AppendLine("    }");
    }

    sb.AppendLine("    else");
    sb.AppendLine("    {");
    sb.AppendLine("      __ok = false; __lit = 0; __litOk = false; __fail = null;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    _ = __lit;");
    sb.AppendLine("    _ = __litOk;");
    sb.AppendLine("    if (!__ok)");
    sb.AppendLine("    {");
    sb.AppendLine("      __boundOut = null;");
    sb.AppendLine("      __failOut = __fail ?? new global::TimeWarp.Nuru.NuruBindError(global::TimeWarp.Nuru.NuruBindErrorKind.UnknownRoute, null, \"Unknown command. Use --help for usage.\");");
    sb.AppendLine("      return false;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine($"    __boundOut = new {bound}");
    sb.AppendLine("    {");
    foreach (Slot slot in slots)
    {
      if (slot.VariableName is not null)
        sb.AppendLine($"      {slot.VariableName} = {slot.VariableName},");
    }

    foreach (Slot slot in slots)
    {
      if (slot.Option is not null && NeedsJsonSuppliedFlag(route, slot.Option))
        sb.AppendLine($"      __supplied_{slot.Id} = __supplied_{slot.Id},");
    }

    sb.AppendLine("    };");
    sb.AppendLine("    __failOut = null;");
    sb.AppendLine("    return true;");
    sb.AppendLine("  }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the static <c>TryBind</c> entry point. The route is named by its pattern, so it binds
  /// through the same per-route method as <c>--json-args</c> with argv holding only the route's
  /// literal words: every parameter and option then comes from the JSON object.
  /// </summary>
  private static void EmitTryBind(StringBuilder sb, IReadOnlyList<RouteDefinition> routes, string methodSuffix)
  {
    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
    {
      string words = string.Join(", ", LiteralWords(routes[routeIndex]).Select(w => $"\"{Escape(w)}\""));
      sb.AppendLine($"  private static readonly string[] __JsonRouteWords{methodSuffix}_{routeIndex} = [{words}];");
    }

    sb.AppendLine();
    sb.AppendLine($"  private static bool {TryBindMethodName(methodSuffix)}(");
    sb.AppendLine("    string route,");
    sb.AppendLine("    global::System.Text.Json.JsonElement arguments,");
    sb.AppendLine("    out global::TimeWarp.Nuru.NuruBoundRequest? request,");
    sb.AppendLine("    out global::TimeWarp.Nuru.NuruBindError? error)");
    sb.AppendLine("  {");
    sb.AppendLine("    request = null;");
    sb.AppendLine("    global::System.Collections.Generic.Dictionary<string, global::System.Text.Json.JsonElement> __jsonMap = new(global::System.StringComparer.Ordinal);");
    sb.AppendLine("    if (arguments.ValueKind == global::System.Text.Json.JsonValueKind.Object)");
    sb.AppendLine("    {");
    sb.AppendLine("      foreach (global::System.Text.Json.JsonProperty __prop in arguments.EnumerateObject())");
    sb.AppendLine("      {");
    sb.AppendLine("        __jsonMap[__prop.Name] = __prop.Value;");
    sb.AppendLine("      }");
    sb.AppendLine("    }");
    sb.AppendLine("    else if (arguments.ValueKind != global::System.Text.Json.JsonValueKind.Undefined)");
    sb.AppendLine("    {");
    sb.AppendLine("      error = new global::TimeWarp.Nuru.NuruBindError(global::TimeWarp.Nuru.NuruBindErrorKind.NotAnObject, null, \"Error: arguments must be a JSON object.\");");
    sb.AppendLine("      return false;");
    sb.AppendLine("    }");
    sb.AppendLine();
    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
    {
      string pattern = Escape(routes[routeIndex].FullPattern);
      sb.AppendLine($"    if (string.Equals(route, \"{pattern}\", global::System.StringComparison.Ordinal))");
      sb.AppendLine("    {");
      sb.AppendLine($"      bool __bound = {BindMethodName(methodSuffix, routeIndex)}(__JsonRouteWords{methodSuffix}_{routeIndex}, __jsonMap, 0, out {BoundClassName(methodSuffix, routeIndex)}? __request, out error);");
      sb.AppendLine("      request = __request;");
      sb.AppendLine("      return __bound;");
      sb.AppendLine("    }");
      sb.AppendLine();
    }

    sb.AppendLine("    error = new global::TimeWarp.Nuru.NuruBindError(global::TimeWarp.Nuru.NuruBindErrorKind.UnknownRoute, null, \"Error: Unknown route '\" + route + \"'.\");");
    sb.AppendLine("    return false;");
    sb.AppendLine("  }");
    sb.AppendLine();
  }
}
