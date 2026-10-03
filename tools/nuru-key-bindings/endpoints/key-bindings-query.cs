#region Purpose
// CLI command that prints built-in REPL key bindings.
#endregion

#region Design
// The command is a thin query over KeyBindingCatalog. Filtering and the table
// versus detailed layout live in the catalog so the REPL command prints the
// same text. An unknown profile is a failed invocation (exit code 1) and still
// returns the message as the query result so the terminal shows it.
#endregion

namespace TimeWarp.Nuru.KeyBindings;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// Lists built-in REPL key bindings.
/// </summary>
[NuruRoute("key-bindings", Description = "List REPL key bindings")]
[NuruRouteExample("key-bindings", Description = "List the Default profile")]
[NuruRouteExample("key-bindings Emacs", Description = "List the Emacs profile by positional name")]
[NuruRouteExample("key-bindings --profile Emacs --function Backward", Description = "Emacs bindings whose function name contains Backward")]
[NuruRouteExample("key-bindings --key Ctrl+a --detailed", Description = "Show one chord with its description")]
public sealed class KeyBindingsQuery : IQuery<string>
{
  /// <summary>
  /// Positional built-in profile name, used when <see cref="Profile"/> is not set.
  /// </summary>
  [Parameter(Description = "Built-in profile name, same as --profile")]
  public string? ProfileName { get; set; }

  /// <summary>
  /// Built-in profile name. Defaults to Default.
  /// </summary>
  [Option("profile", "p", Description = "Built-in profile: Default, Emacs, Vi, or VSCode")]
  public string? Profile { get; set; }

  /// <summary>
  /// Chord filter such as Ctrl+a or LeftArrow.
  /// </summary>
  [Option("key", "k", Description = "Filter by key chord, for example Ctrl+a or LeftArrow")]
  public string? Key { get; set; }

  /// <summary>
  /// Function-name filter.
  /// </summary>
  [Option("function", "f", Description = "Filter by function name")]
  public string? Function { get; set; }

  /// <summary>
  /// Prints one block per binding instead of the table.
  /// </summary>
  [Option("detailed", "d", Description = "Show one binding per block instead of a table")]
  public bool Detailed { get; set; }

  /// <summary>
  /// Handles <see cref="KeyBindingsQuery"/>.
  /// </summary>
  public sealed class Handler : IQueryHandler<KeyBindingsQuery, string>
  {
    /// <summary>
    /// Renders the listing.
    /// </summary>
    /// <param name="query">The invocation.</param>
    /// <param name="cancellationToken">Unused. The listing is synchronous.</param>
    /// <returns>The listing text, or the error message when the profile is unknown.</returns>
    public Task<string> Handle(KeyBindingsQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      string? requested = string.IsNullOrWhiteSpace(query.Profile) ? query.ProfileName : query.Profile;
      string profile = string.IsNullOrWhiteSpace(requested) ? "Default" : requested;
      try
      {
        return Task.FromResult
        (
          KeyBindingCatalog.Render(profile, query.Key, query.Function, query.Detailed)
        );
      }
      catch (ArgumentException exception)
      {
        Environment.ExitCode = 1;
        return Task.FromResult(exception.Message);
      }
    }
  }
}
