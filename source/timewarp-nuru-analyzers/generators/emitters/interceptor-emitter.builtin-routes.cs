#region Purpose
// InterceptorEmitter partial: built-in flag, MCP, serve, completion, interactive and no-match emission.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class InterceptorEmitter
{
  /// <summary>
  /// Emits handling for built-in flags (--help, --version, --capabilities).
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="app">The application model.</param>
  /// <param name="methodSuffix">Suffix for per-app helper methods (e.g., "_0" for multi-app assemblies).</param>
  private static void EmitBuiltInFlags(StringBuilder sb, AppModel app, string methodSuffix)
  {
    // --help flag
    if (app.HasHelp)
    {
      sb.AppendLine("    // Built-in: --help");
      sb.AppendLine($"    if (routeArgs is [{string.Join(" or ", BuiltInFlags.HelpForms.Select(f => $"\"{f}\""))}])");
      sb.AppendLine("    {");
      sb.AppendLine($"      PrintHelp{methodSuffix}(app.Terminal);");
      sb.AppendLine("      return 0;");
      sb.AppendLine("    }");
      sb.AppendLine();
    }

    // --version flag (always available) - shared across apps (assembly-level)
    sb.AppendLine("    // Built-in: --version");
    sb.AppendLine($"    if (routeArgs is [{string.Join(" or ", BuiltInFlags.VersionForms.Select(f => $"\"{f}\""))}])");
    sb.AppendLine("    {");
    sb.AppendLine("      PrintVersion(app.Terminal);");
    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --capabilities --search <query> --group-filter <group> (most specific, check first)
    sb.AppendLine("    // Built-in: --capabilities --search <query> --group-filter <group>");
    sb.AppendLine("    if (routeArgs.Length == 5 && routeArgs[0] == \"--capabilities\" && (routeArgs[1] == \"--search\" || routeArgs[1] == \"-s\") && (routeArgs[3] == \"--group-filter\" || routeArgs[3] == \"-g\"))");
    sb.AppendLine("    {");
    sb.AppendLine($"      return await SearchCapabilitiesAsync{methodSuffix}(app.Terminal, query: routeArgs[2], groupFilter: routeArgs[4]).ConfigureAwait(false);");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --capabilities --group-filter <group> --search <query> (alternate order)
    sb.AppendLine("    // Built-in: --capabilities --group-filter <group> --search <query>");
    sb.AppendLine("    if (routeArgs.Length == 5 && routeArgs[0] == \"--capabilities\" && (routeArgs[1] == \"--group-filter\" || routeArgs[1] == \"-g\") && (routeArgs[3] == \"--search\" || routeArgs[3] == \"-s\"))");
    sb.AppendLine("    {");
    sb.AppendLine($"      return await SearchCapabilitiesAsync{methodSuffix}(app.Terminal, query: routeArgs[4], groupFilter: routeArgs[2]).ConfigureAwait(false);");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --capabilities --search <query> (search without group filter)
    sb.AppendLine("    // Built-in: --capabilities --search <query>");
    sb.AppendLine("    if (routeArgs.Length == 3 && routeArgs[0] == \"--capabilities\" && (routeArgs[1] == \"--search\" || routeArgs[1] == \"-s\"))");
    sb.AppendLine("    {");
    sb.AppendLine($"      return await SearchCapabilitiesAsync{methodSuffix}(app.Terminal, query: routeArgs[2]).ConfigureAwait(false);");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --capabilities flag with --group-filter (always available for AI tools)
    sb.AppendLine("    // Built-in: --capabilities with --group-filter (for AI tools)");
    sb.AppendLine("    if (routeArgs.Length == 3 && routeArgs[0] == \"--capabilities\" && (routeArgs[1] == \"--group-filter\" || routeArgs[1] == \"-g\"))");
    sb.AppendLine("    {");
    sb.AppendLine($"      PrintCapabilities{methodSuffix}(app.Terminal, groupFilter: routeArgs[2]);");
    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --capabilities flag (always available for AI tools)
    sb.AppendLine("    // Built-in: --capabilities (for AI tools)");
    sb.AppendLine($"    if (routeArgs is [{string.Join(" or ", BuiltInFlags.CapabilitiesForms.Select(f => $"\"{f}\""))}])");
    sb.AppendLine("    {");
    sb.AppendLine($"      PrintCapabilities{methodSuffix}(app.Terminal);");
    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --check-updates flag (opt-in via AddCheckUpdatesRoute())
    if (app.HasCheckUpdatesRoute)
    {
      sb.AppendLine("    // Built-in: --check-updates (opt-in via AddCheckUpdatesRoute())");
      sb.AppendLine("    if (routeArgs is [\"--check-updates\"])");
      sb.AppendLine("    {");
      sb.AppendLine("      await CheckForUpdatesAsync(app.Terminal).ConfigureAwait(false);");
      sb.AppendLine("      return 0;");
      sb.AppendLine("    }");
      sb.AppendLine();
    }

    // mcp route (opt-in via AddMcpRoute() from TimeWarp.Nuru.McpHost)
    if (app.McpRoute is McpRouteModel mcpRoute)
    {
      EmitMcpRoute(sb, mcpRoute);
    }

    // serve route (opt-in via AddServeRoute() from TimeWarp.Nuru.Daemon)
    if (app.ServeRoute is not null)
    {
      EmitServeRoute(sb, methodSuffix);
    }

    // Shell completion routes (opt-in via EnableCompletion())
    if (app.HasCompletion)
    {
      EmitCompletionRoutes(sb, methodSuffix);
    }
  }

  /// <summary>
  /// Emits the built-in mcp route. It runs the TimeWarp.Nuru.McpHost stdio server over this app,
  /// with the options from the AddMcpRoute() arguments.
  /// </summary>
  private static void EmitMcpRoute(StringBuilder sb, McpRouteModel mcpRoute)
  {
    string instructions = mcpRoute.Instructions is null
      ? "null"
      : $"\"{EmitterStringUtils.EscapeForStringLiteral(mcpRoute.Instructions)}\"";
    string groupFilter = mcpRoute.GroupFilter is null
      ? "null"
      : $"\"{EmitterStringUtils.EscapeForStringLiteral(mcpRoute.GroupFilter)}\"";

    sb.AppendLine("    // Built-in: mcp (opt-in via AddMcpRoute())");
    sb.AppendLine($"    if (routeArgs is [\"{BuiltInFlags.McpRouteForm}\"])");
    sb.AppendLine("    {");
    sb.AppendLine("      global::TimeWarp.Nuru.McpHost.NuruMcpHostOptions __mcpOptions = new()");
    sb.AppendLine("      {");
    sb.AppendLine($"        Instructions = {instructions},");
    sb.AppendLine($"        GroupFilter = {groupFilter},");
    sb.AppendLine($"        ContentFormat = (global::TimeWarp.Nuru.McpHost.NuruMcpContentFormat){mcpRoute.ContentFormat.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    sb.AppendLine("      };");
    sb.AppendLine("      return await global::TimeWarp.Nuru.McpHost.NuruMcpServer.RunStdioAsync(app, __mcpOptions, cancellationToken).ConfigureAwait(false);");
    sb.AppendLine("    }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the built-in serve routes. They run the TimeWarp.Nuru.Daemon server over this app, or report
  /// and stop a running one, with the options from the AddServeRoute() arguments.
  /// </summary>
  private static void EmitServeRoute(StringBuilder sb, string methodSuffix)
  {
    string form = BuiltInFlags.ServeRouteForm;
    sb.AppendLine("    // Built-in: serve, serve --status, serve --stop (opt-in via AddServeRoute())");
    sb.AppendLine($"    if (routeArgs is [\"{form}\"])");
    sb.AppendLine($"      return await global::TimeWarp.Nuru.Daemon.NuruDaemonServer.RunAsync(app, __DaemonOptions{methodSuffix}(), cancellationToken).ConfigureAwait(false);");
    sb.AppendLine($"    if (routeArgs is [\"{form}\", \"--status\"])");
    sb.AppendLine($"      return await global::TimeWarp.Nuru.Daemon.NuruDaemonServer.StatusAsync(app, __DaemonOptions{methodSuffix}(), cancellationToken).ConfigureAwait(false);");
    sb.AppendLine($"    if (routeArgs is [\"{form}\", \"--stop\"])");
    sb.AppendLine($"      return await global::TimeWarp.Nuru.Daemon.NuruDaemonServer.StopAsync(app, __DaemonOptions{methodSuffix}(), cancellationToken).ConfigureAwait(false);");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits shell completion route handlers.
  /// </summary>
  private static void EmitCompletionRoutes(StringBuilder sb, string methodSuffix)
  {
    // __complete {index} {*words} - callback route for shell scripts
    sb.AppendLine("    // Built-in: __complete (shell completion callback)");
    sb.AppendLine("    if (routeArgs.Length >= 2 && routeArgs[0] == \"__complete\" && int.TryParse(routeArgs[1], out int completionIndex))");
    sb.AppendLine("    {");
    sb.AppendLine("      string[] completionWords = routeArgs.Length > 2 ? routeArgs[2..] : [];");
    sb.AppendLine("      var completionContext = new global::TimeWarp.Nuru.CompletionContext(completionWords, completionIndex);");
    sb.AppendLine("      return global::TimeWarp.Nuru.DynamicCompletionHandler.HandleCompletion(completionContext, app.CompletionSourceRegistry, app.ShellCompletionProvider ?? global::TimeWarp.Nuru.EmptyShellCompletionProvider.Instance, app.Terminal);");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --generate-completion {shell} - generates shell completion script
    sb.AppendLine("    // Built-in: --generate-completion (generates shell script)");
    sb.AppendLine("    if (routeArgs is [\"--generate-completion\", var genShell])");
    sb.AppendLine("    {");
    sb.AppendLine("      string appName = global::System.IO.Path.GetFileNameWithoutExtension(global::System.Environment.ProcessPath ?? \"app\");");
    sb.AppendLine("      string script = genShell.ToLowerInvariant() switch");
    sb.AppendLine("      {");
    sb.AppendLine("        \"bash\" => global::TimeWarp.Nuru.DynamicCompletionScriptGenerator.GenerateBash(appName),");
    sb.AppendLine("        \"zsh\" => global::TimeWarp.Nuru.DynamicCompletionScriptGenerator.GenerateZsh(appName),");
    sb.AppendLine("        \"fish\" => global::TimeWarp.Nuru.DynamicCompletionScriptGenerator.GenerateFish(appName),");
    sb.AppendLine("        \"pwsh\" or \"powershell\" => global::TimeWarp.Nuru.DynamicCompletionScriptGenerator.GeneratePowerShell(appName),");
    sb.AppendLine("        _ => throw new global::System.ArgumentException($\"Unknown shell: {genShell}. Supported: bash, zsh, fish, pwsh\")");
    sb.AppendLine("      };");
    sb.AppendLine("      await app.Terminal.WriteLineAsync(script).ConfigureAwait(false);");
    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --install-completion --dry-run {shell?} - preview installation (more specific, check first)
    sb.AppendLine("    // Built-in: --install-completion --dry-run (preview installation)");
    sb.AppendLine("    if (routeArgs is [\"--install-completion\", \"--dry-run\"])");
    sb.AppendLine("    {");
    sb.AppendLine("      string appName = global::System.IO.Path.GetFileNameWithoutExtension(global::System.Environment.ProcessPath ?? \"app\");");
    sb.AppendLine("      return global::TimeWarp.Nuru.InstallCompletionHandler.Install(app.Terminal, appName, null, dryRun: true);");
    sb.AppendLine("    }");
    sb.AppendLine();

    sb.AppendLine("    if (routeArgs is [\"--install-completion\", \"--dry-run\", var dryRunShell])");
    sb.AppendLine("    {");
    sb.AppendLine("      string appName = global::System.IO.Path.GetFileNameWithoutExtension(global::System.Environment.ProcessPath ?? \"app\");");
    sb.AppendLine("      return global::TimeWarp.Nuru.InstallCompletionHandler.Install(app.Terminal, appName, dryRunShell, dryRun: true);");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --install-completion {shell?} - installs completion to shell config (less specific, check after --dry-run)
    sb.AppendLine("    // Built-in: --install-completion (installs shell script)");
    sb.AppendLine("    if (routeArgs is [\"--install-completion\"])");
    sb.AppendLine("    {");
    sb.AppendLine("      string appName = global::System.IO.Path.GetFileNameWithoutExtension(global::System.Environment.ProcessPath ?? \"app\");");
    sb.AppendLine("      return global::TimeWarp.Nuru.InstallCompletionHandler.Install(app.Terminal, appName, null, dryRun: false);");
    sb.AppendLine("    }");
    sb.AppendLine();

    sb.AppendLine("    if (routeArgs is [\"--install-completion\", var installShell])");
    sb.AppendLine("    {");
    sb.AppendLine("      string appName = global::System.IO.Path.GetFileNameWithoutExtension(global::System.Environment.ProcessPath ?? \"app\");");
    sb.AppendLine("      return global::TimeWarp.Nuru.InstallCompletionHandler.Install(app.Terminal, appName, installShell, dryRun: false);");
    sb.AppendLine("    }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits --interactive / -i flag check. Called BEFORE user routes so catch-all routes don't intercept it.
  /// Also handles AutoStartWhenEmpty if enabled.
  /// </summary>
  private static void EmitInteractiveFlag(StringBuilder sb, AppModel app, string methodSuffix)
  {
    if (app.HasRepl)
    {
      bool autoStartWhenEmpty = app.ReplOptions?.AutoStartWhenEmpty ?? false;

      sb.AppendLine("    // Built-in: --interactive / -i (REPL mode, opt-in via AddRepl())");
      sb.AppendLine("    // Emitted BEFORE user routes so catch-all routes don't intercept it");
      sb.AppendLine("    if (routeArgs is [\"--interactive\"] or [\"-i\"])");
      sb.AppendLine("    {");
      sb.AppendLine($"      return await RunReplAsync{methodSuffix}(app, cancellationToken).ConfigureAwait(false);");
      sb.AppendLine("    }");
      sb.AppendLine();

      // Emit AutoStartWhenEmpty check if enabled
      if (autoStartWhenEmpty)
      {
        sb.AppendLine("    // Auto-start REPL when no arguments provided (AutoStartWhenEmpty = true)");
        sb.AppendLine("    if (routeArgs.Length == 0)");
        sb.AppendLine("    {");
        sb.AppendLine($"      return await RunReplAsync{methodSuffix}(app, cancellationToken).ConfigureAwait(false);");
        sb.AppendLine("    }");
        sb.AppendLine();
      }
    }
  }

  /// <summary>
  /// Emits the no-match fallback code.
  /// </summary>
  private static void EmitNoMatch(StringBuilder sb)
  {
    sb.AppendLine("    // No route matched. A command that matched apart from an undeclared option names that option.");
    sb.AppendLine("    if (__unknownOption is not null)");
    sb.AppendLine("    {");
    sb.AppendLine("      await app.Terminal.WriteErrorLineAsync(global::TimeWarp.Nuru.NuruUnknownOption.FormatMessage(__unknownOption, __unknownOptionForms)).ConfigureAwait(false);");
    sb.AppendLine("      return 1;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    await app.Terminal.WriteErrorLineAsync(\"Unknown command. Use --help for usage.\").ConfigureAwait(false);");
    sb.AppendLine("    return 1;");
  }
}
