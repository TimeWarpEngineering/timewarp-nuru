namespace TimeWarp.Nuru.Completion;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TimeWarp.Nuru; // Endpoint, EndpointCollection
using TimeWarp.Nuru.Parsing; // CompiledRoute, RouteMatcher, LiteralMatcher, OptionMatcher

/// <summary>
/// Generates shell completion scripts for various shells.
/// </summary>
public class CompletionScriptGenerator
{
  /// <summary>
  /// Generate bash completion script.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Public API should remain instance-based for future extensibility")]
  public string GenerateBash(EndpointCollection endpoints, string appName)
  {
    ArgumentNullException.ThrowIfNull(endpoints);

    string template = LoadTemplate("bash-completion.sh");

    HashSet<string> commands = ExtractCommands(endpoints);
    HashSet<string> options = ExtractOptions(endpoints);

    // Replace placeholders
    string script = template
      .Replace("{{APP_NAME}}", appName, StringComparison.Ordinal)
      .Replace("{{COMMANDS}}", string.Join(" ", commands), StringComparison.Ordinal)
      .Replace("{{OPTIONS}}", string.Join(" ", options), StringComparison.Ordinal);

    return script;
  }

  /// <summary>
  /// Generate zsh completion script.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Public API should remain instance-based for future extensibility")]
  public string GenerateZsh(EndpointCollection endpoints, string appName)
  {
    ArgumentNullException.ThrowIfNull(endpoints);

    string template = LoadTemplate("zsh-completion.zsh");

    HashSet<string> commands = ExtractCommands(endpoints);
    HashSet<string> options = ExtractOptions(endpoints);

    // Build _arguments specification
    var argsSpec = new StringBuilder();
    foreach (string option in options)
    {
      argsSpec.AppendLine(CultureInfo.InvariantCulture, $"  '{option}[{option}]' \\");
    }

    string script = template
      .Replace("{{APP_NAME}}", appName, StringComparison.Ordinal)
      .Replace("{{COMMANDS}}", string.Join(" ", commands), StringComparison.Ordinal)
      .Replace("{{ARGUMENTS_SPEC}}", argsSpec.ToString(), StringComparison.Ordinal);

    return script;
  }

  /// <summary>
  /// Generate PowerShell completion script.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Public API should remain instance-based for future extensibility")]
  public string GeneratePowerShell(EndpointCollection endpoints, string appName)
  {
    ArgumentNullException.ThrowIfNull(endpoints);

    string template = LoadTemplate("pwsh-completion.ps1");

    HashSet<string> commands = ExtractCommands(endpoints);
    HashSet<string> options = ExtractOptions(endpoints);

    // Build PowerShell completion results
    var completions = new StringBuilder();
    foreach (string cmd in commands)
    {
      completions.AppendLine(CultureInfo.InvariantCulture, $"    [System.Management.Automation.CompletionResult]::new('{cmd}', '{cmd}', [System.Management.Automation.CompletionResultType]::ParameterValue, '{cmd}')");
    }

    string script = template
      .Replace("{{APP_NAME}}", appName, StringComparison.Ordinal)
      .Replace("{{COMPLETIONS}}", completions.ToString(), StringComparison.Ordinal);

    return script;
  }

  /// <summary>
  /// Generate fish completion script.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Public API should remain instance-based for future extensibility")]
  public string GenerateFish(EndpointCollection endpoints, string appName)
  {
    ArgumentNullException.ThrowIfNull(endpoints);

    string template = LoadTemplate("fish-completion.fish");

    HashSet<string> commands = ExtractCommands(endpoints);
    HashSet<string> options = ExtractOptions(endpoints);

    // Build fish complete commands
    var completes = new StringBuilder();
    foreach (string cmd in commands)
    {
      completes.AppendLine(CultureInfo.InvariantCulture, $"complete -c {appName} -a '{cmd}' -d 'Command: {cmd}'");
    }

    foreach (string option in options)
    {
      completes.AppendLine(CultureInfo.InvariantCulture, $"complete -c {appName} -l {option.TrimStart('-')} -d 'Option: {option}'");
    }

    string script = template
      .Replace("{{APP_NAME}}", appName, StringComparison.Ordinal)
      .Replace("{{COMPLETE_COMMANDS}}", completes.ToString(), StringComparison.Ordinal);

    return script;
  }

  /// <summary>
  /// Extract all unique command literals from routes.
  /// </summary>
  private static HashSet<string> ExtractCommands(EndpointCollection endpoints)
  {
    var commands = new HashSet<string>();

    foreach (Endpoint endpoint in endpoints.Endpoints)
    {
      CompiledRoute route = endpoint.CompiledRoute;

      // Get first literal segment as command
      RouteMatcher? firstSegment = route.Segments.Count > 0 ? route.Segments[0] : null;
      if (firstSegment is LiteralMatcher literal)
      {
        commands.Add(literal.Value);
      }
    }

    return commands;
  }

  /// <summary>
  /// Extract all unique options from routes.
  /// </summary>
  private static HashSet<string> ExtractOptions(EndpointCollection endpoints)
  {
    var options = new HashSet<string>();

    foreach (Endpoint endpoint in endpoints.Endpoints)
    {
      CompiledRoute route = endpoint.CompiledRoute;

      foreach (OptionMatcher option in route.OptionMatchers)
      {
        options.Add(option.MatchPattern);

        if (!string.IsNullOrEmpty(option.AlternateForm))
        {
          options.Add(option.AlternateForm);
        }
      }
    }

    return options;
  }

  /// <summary>
  /// Load embedded template resource.
  /// </summary>
  private static string LoadTemplate(string templateName)
  {
    Assembly assembly = typeof(CompletionScriptGenerator).Assembly;
    string resourceName = $"TimeWarp.Nuru.Completion.Completion.Templates.{templateName}";

    using Stream? stream = assembly.GetManifestResourceStream(resourceName);
    if (stream is null)
    {
      throw new InvalidOperationException($"Template not found: {templateName}");
    }

    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }
}
