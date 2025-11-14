namespace TimeWarp.Nuru.Completion;

using System.Reflection;

/// <summary>
/// Generates dynamic completion scripts for various shells.
/// Dynamic scripts call back to the application via the __complete route.
/// </summary>
internal static class DynamicCompletionScriptGenerator
{
  private const string BashTemplateName = "TimeWarp.Nuru.Completion.Completion.Templates.bash-completion-dynamic.sh";
  private const string ZshTemplateName = "TimeWarp.Nuru.Completion.Completion.Templates.zsh-completion-dynamic.zsh";
  private const string PwshTemplateName = "TimeWarp.Nuru.Completion.Completion.Templates.pwsh-completion-dynamic.ps1";
  private const string FishTemplateName = "TimeWarp.Nuru.Completion.Completion.Templates.fish-completion-dynamic.fish";

  /// <summary>
  /// Generates a Bash completion script.
  /// </summary>
  public static string GenerateBash(string appName)
  {
    string template = LoadEmbeddedResource(BashTemplateName);
    return template.Replace("{{APP_NAME}}", appName, StringComparison.Ordinal);
  }

  /// <summary>
  /// Generates a Zsh completion script.
  /// </summary>
  public static string GenerateZsh(string appName)
  {
    string template = LoadEmbeddedResource(ZshTemplateName);
    return template.Replace("{{APP_NAME}}", appName, StringComparison.Ordinal);
  }

  /// <summary>
  /// Generates a PowerShell completion script.
  /// </summary>
  public static string GeneratePowerShell(string appName)
  {
    string template = LoadEmbeddedResource(PwshTemplateName);
    return template.Replace("{{APP_NAME}}", appName, StringComparison.Ordinal);
  }

  /// <summary>
  /// Generates a Fish completion script.
  /// </summary>
  public static string GenerateFish(string appName)
  {
    string template = LoadEmbeddedResource(FishTemplateName);
    return template.Replace("{{APP_NAME}}", appName, StringComparison.Ordinal);
  }

  /// <summary>
  /// Loads an embedded resource template.
  /// </summary>
  private static string LoadEmbeddedResource(string resourceName)
  {
    Assembly assembly = typeof(DynamicCompletionScriptGenerator).Assembly;
    using Stream? stream = assembly.GetManifestResourceStream(resourceName);

    if (stream is null)
    {
      throw new InvalidOperationException(
        $"Embedded resource '{resourceName}' not found. " +
        $"Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}"
      );
    }

    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }
}
