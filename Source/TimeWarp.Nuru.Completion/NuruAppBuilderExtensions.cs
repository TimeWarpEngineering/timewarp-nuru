namespace TimeWarp.Nuru.Completion;

using System;
using TimeWarp.Nuru;

/// <summary>
/// Extension methods for NuruAppBuilder to enable shell completion support.
/// </summary>
public static class NuruAppBuilderExtensions
{
  /// <summary>
  /// Enables shell completion by automatically registering the `--generate-completion {shell}` route.
  /// </summary>
  /// <param name="builder">The NuruAppBuilder instance.</param>
  /// <param name="appName">
  /// Optional application name to use in generated scripts.
  /// If null, attempts to use the entry assembly name.
  /// </param>
  /// <returns>The builder for fluent chaining.</returns>
  public static NuruAppBuilder EnableShellCompletion(
    this NuruAppBuilder builder,
    string? appName = null)
  {
    ArgumentNullException.ThrowIfNull(builder);

    // Use provided name or fallback to entry assembly name
    string effectiveAppName = appName ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "myapp";

    // Register the --generate-completion route
    builder.AddRoute("--generate-completion {shell}", (string shell) =>
    {
      var generator = new CompletionScriptGenerator();

      string script = shell.ToLowerInvariant() switch
      {
        "bash" => generator.GenerateBash(builder.EndpointCollection, effectiveAppName),
        "zsh" => generator.GenerateZsh(builder.EndpointCollection, effectiveAppName),
        "pwsh" or "powershell" => generator.GeneratePowerShell(builder.EndpointCollection, effectiveAppName),
        "fish" => generator.GenerateFish(builder.EndpointCollection, effectiveAppName),
        _ => throw new ArgumentException(
          $"Unknown shell: {shell}. Supported shells: bash, zsh, pwsh, fish",
          nameof(shell))
      };

      Console.WriteLine(script);
      return 0;
    });

    return builder;
  }
}
