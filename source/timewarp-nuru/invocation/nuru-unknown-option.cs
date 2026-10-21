#region Purpose
// Argv checks for option-shaped tokens a route does not declare. Generated route matchers call these so an
// unknown --flag fails the call instead of landing in the positional list (task 515).
#endregion

#region Design
// A token is option-shaped when it starts with '-', is longer than one character, is not the "--" separator,
// and does not parse as a number. "-" alone (stdin by convention) and negative numbers (-5, -1.5e10) stay
// positionals. The matcher only asks about tokens before "--" that no declared option consumed.
// The suggestion is the closest declared form by edit distance, offered only when the distance is small
// next to the typed name, so an unrelated option is never suggested.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Detects and reports argv tokens that look like options but are not declared by the route.
/// </summary>
public static class NuruUnknownOption
{
  /// <summary>
  /// Returns true when <paramref name="token"/> looks like an option: it starts with <c>-</c>, is not
  /// <c>-</c> or <c>--</c>, and is not a number such as <c>-5</c> or <c>-1.5e10</c>.
  /// </summary>
  /// <param name="token">One argv token.</param>
  /// <returns>True when the token is option-shaped.</returns>
  public static bool IsOptionLike(string token)
  {
    ArgumentNullException.ThrowIfNull(token);
    if (token.Length < 2 || token[0] != '-' || token == "--")
      return false;

    return !double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
  }

  /// <summary>
  /// Builds the error line for an unknown option, with a suggestion when a declared form is close.
  /// </summary>
  /// <param name="token">The unknown token as typed (it may carry <c>=value</c>).</param>
  /// <param name="declaredForms">Option forms (<c>--long</c>, <c>-s</c>) of the routes whose command matched.</param>
  /// <returns>For example <c>Unknown option '--repo'. Did you mean '--root'?</c></returns>
  public static string FormatMessage(string token, IEnumerable<string> declaredForms)
  {
    ArgumentNullException.ThrowIfNull(token);
    ArgumentNullException.ThrowIfNull(declaredForms);

    int equalsIndex = token.IndexOf('=', StringComparison.Ordinal);
    string name = equalsIndex > 0 ? token[..equalsIndex] : token;
    List<string> forms = [.. declaredForms.Distinct(StringComparer.Ordinal)];

    // A declared form with an unsupported "=value" (a flag) is not unknown by name; show the whole token.
    if (forms.Contains(name, StringComparer.Ordinal))
      return $"Unknown option '{token}'. Use --help for usage.";

    string? suggestion = Suggest(name, forms);
    return suggestion is null
      ? $"Unknown option '{name}'. Use --help for usage."
      : $"Unknown option '{name}'. Did you mean '{suggestion}'?";
  }

  /// <summary>
  /// Returns true when the next argv token is likely the value of <paramref name="token"/>: the token
  /// has no inline <c>=value</c> and its closest declared form is one of <paramref name="valueForms"/>.
  /// A typo of a flag, or of nothing declared, takes no value.
  /// </summary>
  /// <param name="token">The unknown token as typed.</param>
  /// <param name="declaredForms">All option forms of the route.</param>
  /// <param name="valueForms">The forms of the route's value-taking options.</param>
  /// <returns>True when the following token should count as the option's value.</returns>
  public static bool TakesValue(string token, IEnumerable<string> declaredForms, ICollection<string> valueForms)
  {
    ArgumentNullException.ThrowIfNull(token);
    ArgumentNullException.ThrowIfNull(declaredForms);
    ArgumentNullException.ThrowIfNull(valueForms);

    if (valueForms.Count == 0 || token.Contains('=', StringComparison.Ordinal))
      return false;

    string? suggestion = Suggest(token, declaredForms);
    return suggestion is not null && valueForms.Contains(suggestion);
  }

  /// <summary>
  /// Returns the declared form closest to <paramref name="name"/>, or null when none is close.
  /// </summary>
  /// <param name="name">The unknown option name, without any <c>=value</c>.</param>
  /// <param name="declaredForms">Candidate option forms.</param>
  /// <returns>The closest form, or null.</returns>
  public static string? Suggest(string name, IEnumerable<string> declaredForms)
  {
    ArgumentNullException.ThrowIfNull(name);
    ArgumentNullException.ThrowIfNull(declaredForms);

    string bare = name.TrimStart('-');
    int threshold = Math.Max(2, bare.Length / 3);
    string? best = null;
    int bestDistance = int.MaxValue;
    foreach (string form in declaredForms)
    {
      int distance = Distance(bare, form.TrimStart('-'));
      if (distance < bestDistance)
      {
        bestDistance = distance;
        best = form;
      }
    }

    // bestDistance < bare.Length: a one-letter short option never suggests a different letter
    return best is not null && bestDistance <= threshold && bestDistance < bare.Length ? best : null;
  }

  /// <summary>Levenshtein distance between two strings (ordinal, case-sensitive).</summary>
  private static int Distance(string source, string target)
  {
    int[] previous = new int[target.Length + 1];
    int[] current = new int[target.Length + 1];
    for (int j = 0; j <= target.Length; j++)
      previous[j] = j;

    for (int i = 1; i <= source.Length; i++)
    {
      current[0] = i;
      for (int j = 1; j <= target.Length; j++)
      {
        int cost = source[i - 1] == target[j - 1] ? 0 : 1;
        current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
      }

      (previous, current) = (current, previous);
    }

    return previous[target.Length];
  }
}
