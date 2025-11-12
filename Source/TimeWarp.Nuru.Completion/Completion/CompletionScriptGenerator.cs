namespace TimeWarp.Nuru.Completion;

using System;
using System.Collections.Generic;
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
  public string GenerateBash(EndpointCollection endpoints, string appName)
  {
    string template = LoadTemplate("bash-completion.sh");

    var commands = ExtractCommands(endpoints);
    var options = ExtractOptions(endpoints);

    // Replace placeholders
    string script = template
      .Replace("{{APP_NAME}}", appName)
      .Replace("{{COMMANDS}}", string.Join(" ", commands))
      .Replace("{{OPTIONS}}", string.Join(" ", options));

    return script;
  }

  /// <summary>
  /// Generate zsh completion script.
  /// </summary>
  public string GenerateZsh(EndpointCollection endpoints, string appName)
  {
    string template = LoadTemplate("zsh-completion.zsh");

    var commands = ExtractCommands(endpoints);
    var options = ExtractOptions(endpoints);

    // Build _arguments specification
    var argsSpec = new StringBuilder();
    foreach (string option in options)
    {
      argsSpec.AppendLine($"  '{option}[{option}]' \\");
    }

    string script = template
      .Replace("{{APP_NAME}}", appName)
      .Replace("{{COMMANDS}}", string.Join(" ", commands))
      .Replace("{{ARGUMENTS_SPEC}}", argsSpec.ToString());

    return script;
  }

  /// <summary>
  /// Generate PowerShell completion script.
  /// </summary>
  public string GeneratePowerShell(EndpointCollection endpoints, string appName)
  {
    string template = LoadTemplate("pwsh-completion.ps1");

    var commands = ExtractCommands(endpoints);
    var options = ExtractOptions(endpoints);

    // Build PowerShell completion results
    var completions = new StringBuilder();
    foreach (string cmd in commands)
    {
      completions.AppendLine($"    [CompletionResult]::new('{cmd}', '{cmd}', [CompletionResultType]::ParameterValue, '{cmd}')");
    }

    string script = template
      .Replace("{{APP_NAME}}", appName)
      .Replace("{{COMPLETIONS}}", completions.ToString());

    return script;
  }

  /// <summary>
  /// Generate fish completion script.
  /// </summary>
  public string GenerateFish(EndpointCollection endpoints, string appName)
  {
    string template = LoadTemplate("fish-completion.fish");

    var commands = ExtractCommands(endpoints);
    var options = ExtractOptions(endpoints);

    // Build fish complete commands
    var completes = new StringBuilder();
    foreach (string cmd in commands)
    {
      completes.AppendLine($"complete -c {appName} -a '{cmd}' -d 'Command: {cmd}'");
    }

    foreach (string option in options)
    {
      completes.AppendLine($"complete -c {appName} -l {option.TrimStart('-')} -d 'Option: {option}'");
    }

    string script = template
      .Replace("{{APP_NAME}}", appName)
      .Replace("{{COMPLETE_COMMANDS}}", completes.ToString());

    return script;
  }

  /// <summary>
  /// Extract all unique command literals from routes.
  /// </summary>
  private HashSet<string> ExtractCommands(EndpointCollection endpoints)
  {
    var commands = new HashSet<string>();

    foreach (Endpoint endpoint in endpoints.Endpoints)
    {
      CompiledRoute route = endpoint.CompiledRoute;

      // Get first literal segment as command
      RouteMatcher? firstSegment = route.Segments.FirstOrDefault();
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
  private HashSet<string> ExtractOptions(EndpointCollection endpoints)
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
  private string LoadTemplate(string templateName)
  {
    Assembly assembly = typeof(CompletionScriptGenerator).Assembly;
    string resourceName = $"TimeWarp.Nuru.Completion.Completion.Templates.{templateName}";

    using Stream? stream = assembly.GetManifestResourceStream(resourceName);
    if (stream == null)
    {
      throw new InvalidOperationException($"Template not found: {templateName}");
    }

    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }
}
