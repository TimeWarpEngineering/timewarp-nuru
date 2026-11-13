#region Purpose
// Emits JSON invoke, parse preamble, and built-in predicate code.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class JsonArgsEmitter
{
  private static void EmitInvoke(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    AppModel app,
    string methodSuffix,
    string? loggerFactoryFieldName,
    bool hasGeneratedMediator,
    int indent,
    string? captureResultInto)
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
          httpClientConfigurations: app.HttpClientConfigurations,
          useDirectInvoke: app.UseDirectInvoke,
          hasGeneratedMediator: hasGeneratedMediator,
          jsonArgsPath: true,
          captureResultInto: captureResultInto,
          verbatimRendererOutput: app.VerbatimRendererOutput),
        methodSuffix: methodSuffix,
        jsonArgsPath: true);
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
        httpClientConfigurations: app.HttpClientConfigurations,
        useDirectInvoke: app.UseDirectInvoke,
        hasGeneratedMediator: hasGeneratedMediator,
        jsonArgsPath: true,
        captureResultInto: captureResultInto,
        verbatimRendererOutput: app.VerbatimRendererOutput);
    }

    _ = ind;
  }

  private static void EmitParse(StringBuilder sb)
  {
    sb.AppendLine("        if (fromRepl && __jsonArgsValue == \"-\")");
    sb.AppendLine("        {");
    sb.AppendLine("          await app.WriteBindErrorAsync(\"Error: --json-args - is not available in the REPL. Use @path or a single-line '{\\\"…\\\"}'.\", null, null).ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        string __jsonText;");
    sb.AppendLine("        if (__jsonArgsValue == \"-\")");
    sb.AppendLine("        {");
    sb.AppendLine("          if (!global::System.Console.IsInputRedirected)");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.WriteBindErrorAsync(\"Error: --json-args - requires redirected stdin.\", null, null).ConfigureAwait(false);");
    sb.AppendLine("            return 1;");
    sb.AppendLine("          }");
    sb.AppendLine();
    sb.AppendLine("          using global::System.IO.StreamReader __jsonReader = new(global::System.Console.OpenStandardInput(), global::System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);");
    sb.AppendLine("          __jsonText = await __jsonReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);");
    sb.AppendLine("          if (string.IsNullOrWhiteSpace(__jsonText))");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.WriteBindErrorAsync(\"Error: --json-args - got empty stdin.\", null, null).ConfigureAwait(false);");
    sb.AppendLine("            return 1;");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("        else if (__jsonArgsValue.StartsWith(\"@\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("        {");
    sb.AppendLine("          string __jsonPath = __jsonArgsValue.Substring(1);");
    sb.AppendLine("          if (__jsonPath.Contains(\"://\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.WriteBindErrorAsync(\"Error: --json-args @path does not accept URIs.\", null, null).ConfigureAwait(false);");
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
    sb.AppendLine("            __jsonText = global::System.IO.File.ReadAllText(global::TimeWarp.Nuru.NuruCallContext.GetFullPath(__jsonPath), global::System.Text.Encoding.UTF8);");
    sb.AppendLine("          }");
    sb.AppendLine("          catch (global::System.Exception)");
    sb.AppendLine("          {");
    sb.AppendLine("            await app.WriteBindErrorAsync(\"Error: --json-args could not read '\" + __jsonPath + \"'.\", null, null).ConfigureAwait(false);");
    sb.AppendLine("            return 1;");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("        else if (__jsonArgsValue.StartsWith(\"{\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("        {");
    sb.AppendLine("          __jsonText = __jsonArgsValue;");
    sb.AppendLine("        }");
    sb.AppendLine("        else");
    sb.AppendLine("        {");
    sb.AppendLine("          await app.WriteBindErrorAsync(\"Error: --json-args value must be -, @path, or a JSON object.\", null, null).ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        try");
    sb.AppendLine("        {");
    sb.AppendLine("          __jsonDoc = global::System.Text.Json.JsonDocument.Parse(__jsonText);");
    sb.AppendLine("        }");
    sb.AppendLine("        catch (global::System.Text.Json.JsonException)");
    sb.AppendLine("        {");
    sb.AppendLine("          await app.WriteBindErrorAsync(\"Error: --json-args value is not valid JSON.\", null, null).ConfigureAwait(false);");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        if (__jsonDoc.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)");
    sb.AppendLine("        {");
    sb.AppendLine("          __jsonDoc.Dispose();");
    sb.AppendLine("          __jsonDoc = null;");
    sb.AppendLine("          await app.WriteBindErrorAsync(\"Error: --json-args value must be a JSON object.\", global::TimeWarp.Nuru.NuruBindErrorKind.NotAnObject, null).ConfigureAwait(false);");
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

    if (app.McpRoute is not null)
    {
      sb.AppendLine($"      if (routeArgs is [\"{BuiltInFlags.McpRouteForm}\"]) return true;");
    }

    if (app.ServeRoute is not null)
    {
      sb.AppendLine($"      if (routeArgs is [\"{BuiltInFlags.ServeRouteForm}\"] or [\"{BuiltInFlags.ServeRouteForm}\", \"--status\" or \"--stop\"]) return true;");
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
}
