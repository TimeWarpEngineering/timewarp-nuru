#region Purpose
// Query side of SearchIndex: SearchAsync, the `--cli` key resolver, FTS query sanitizing and group filter
// helpers.
#endregion

#region Design
// The `--cli` key. Rows are stored under the capabilities name (the app's WithName, or its assembly
// name), and clis.cli_path holds the executable that was run. A user types either: the app name they
// see in results, or the executable name they run (search auto-index resolves that name on PATH). So
// ResolveCliNamesAsync accepts both. An exact capabilities name wins. Otherwise every row whose
// executable file name matches is used, with or without its extension, case-insensitive on Windows
// only (task 534).
#endregion

namespace TimeWarp.Nuru.Search.Services;

public sealed partial class SearchIndex
{
  public async Task<IReadOnlyList<SearchResult>> SearchAsync(
    string query,
    string? cliName = null,
    string? groupPath = null,
    int limit = 50,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(query);

    if (!Initialized)
    {
      await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    string sanitizedQuery = SanitizeFtsQuery(query);
    List<SearchResult> results = [];

    if (string.IsNullOrEmpty(sanitizedQuery))
    {
      return results;
    }

    await using SqliteCommand cmd = Connection.CreateCommand();

    StringBuilder sqlBuilder = new();
    sqlBuilder.AppendLine("""
      SELECT e.cli_name, e.pattern, e.description, e.group_path, e.endpoint_json, c.version, endpoints_fts.rank
      FROM endpoints_fts
      JOIN endpoints e ON endpoints_fts.rowid = e.id
      LEFT JOIN clis c ON c.name = e.cli_name
      WHERE endpoints_fts MATCH $query
      """);

    if (!string.IsNullOrEmpty(cliName))
    {
      IReadOnlyList<string> cliNames = await ResolveCliNamesAsync(cliName, cancellationToken).ConfigureAwait(false);

      if (cliNames.Count == 0)
      {
        return results;
      }

      List<string> parameterNames = [];
      for (int i = 0; i < cliNames.Count; i++)
      {
        string parameterName = $"$cliName{i}";
        parameterNames.Add(parameterName);
        cmd.Parameters.AddWithValue(parameterName, cliNames[i]);
      }

      sqlBuilder.Append("  AND e.cli_name IN (").AppendJoin(", ", parameterNames).AppendLine(")");
    }

    string normalizedGroupPath = NormalizeGroupFilter(groupPath);
    if (normalizedGroupPath.Length > 0)
    {
      // Match the group itself or any child group, on whole segment boundaries only.
      sqlBuilder.AppendLine("  AND (e.group_path LIKE $groupPath ESCAPE '\\' OR e.group_path LIKE $groupPath || ' %' ESCAPE '\\')");
      cmd.Parameters.AddWithValue("$groupPath", EscapeLikePattern(normalizedGroupPath));
    }

    sqlBuilder.AppendLine("  ORDER BY endpoints_fts.rank");
    sqlBuilder.AppendLine("  LIMIT $limit");

#pragma warning disable CA2100 // SQL is built from our own code, user input goes through parameters
    cmd.CommandText = sqlBuilder.ToString();
#pragma warning restore CA2100

    cmd.Parameters.AddWithValue("$query", sanitizedQuery);
    cmd.Parameters.AddWithValue("$limit", limit);

    try
    {
      await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
      while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
      {
        string endpointJson = reader.GetString(4);
        EndpointCapability? endpoint = JsonSerializer.Deserialize(endpointJson, SearchIndexJsonContext.Default.EndpointCapability);

        if (endpoint is not null)
        {
          results.Add(new SearchResult
          {
            CliName = reader.GetString(0),
            Pattern = reader.GetString(1),
            Description = await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(2),
            GroupPath = reader.GetString(3),
            CliVersion = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(5),
            Endpoint = endpoint
          });
        }
      }
    }
    catch (SqliteException ex)
    {
      // Defense in depth: SanitizeFtsQuery should make every MATCH expression valid, but a
      // malformed query must degrade to "no results", never crash the CLI.
      LogSearchFailed(Logger, ex, query);
      results.Clear();
    }

    return results;
  }

  /// <summary>
  /// Resolves a <c>--cli</c> value to the capabilities names it selects: the value itself when a
  /// CLI is indexed under that name, otherwise every indexed CLI whose executable file name is the
  /// value. Empty when nothing matches. See the Design region.
  /// </summary>
  public async Task<IReadOnlyList<string>> ResolveCliNamesAsync(string cli, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(cli);

    IReadOnlyList<CliInfo> clis = await ListClisAsync(cancellationToken).ConfigureAwait(false);

    if (clis.Any(c => string.Equals(c.Name, cli, StringComparison.Ordinal)))
    {
      return [cli];
    }

    return
    [
      .. clis
        .Where(c => IsExecutableName(c.CliPath, cli))
        .Select(c => c.Name)
    ];
  }

  /// <summary>
  /// True when <paramref name="cli"/> names the file at <paramref name="cliPath"/>, with or without
  /// its extension (<c>tool</c> matches <c>tool.exe</c>). File names compare case-insensitively on
  /// Windows only.
  /// </summary>
  internal static bool IsExecutableName(string? cliPath, string cli)
  {
    if (string.IsNullOrEmpty(cliPath) || string.IsNullOrEmpty(cli))
    {
      return false;
    }

    StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    return string.Equals(Path.GetFileName(cliPath), cli, comparison)
      || string.Equals(Path.GetFileNameWithoutExtension(cliPath), cli, comparison);
  }

  [LoggerMessage(LogLevel.Warning, "Search query could not be executed: {Query}")]
  private static partial void LogSearchFailed(ILogger logger, Exception exception, string query);

  /// <summary>
  /// Turns free-form user input into a valid FTS5 MATCH expression: every whitespace-separated
  /// token becomes a quoted prefix term. Control characters (NUL, other C0/C1) are treated as
  /// separators — FTS5 reports <c>unterminated string</c> for an embedded U+0000, and none of
  /// them carry search meaning.
  /// </summary>
  internal static string SanitizeFtsQuery(string query)
  {
    ArgumentNullException.ThrowIfNull(query);

    string[] tokens = StripControlCharacters(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
    List<string> sanitizedTokens = [];

    foreach (string token in tokens)
    {
      // FTS5: double-quoted string is a literal token. Double internal quotes.
      // Append * outside the closing quote for prefix matching.
      string escaped = token.Replace("\"", "\"\"", StringComparison.Ordinal);
      sanitizedTokens.Add($"\"{escaped}\"*");
    }

    return string.Join(" ", sanitizedTokens);
  }

  private static string StripControlCharacters(string query)
  {
    char[] chars = query.ToCharArray();
    bool changed = false;

    for (int i = 0; i < chars.Length; i++)
    {
      if (char.IsControl(chars[i]))
      {
        chars[i] = ' ';
        changed = true;
      }
    }

    return changed ? new string(chars) : query;
  }

  /// <summary>
  /// Normalizes a <c>--group</c> filter to the space-separated form stored in <c>group_path</c>.
  /// Accepts the dotted form documented by the capabilities filter (<c>docker.remote</c>) as well as
  /// a space-separated form (<c>docker remote</c>).
  /// </summary>
  internal static string NormalizeGroupFilter(string? groupFilter)
  {
    if (string.IsNullOrWhiteSpace(groupFilter))
    {
      return string.Empty;
    }

    string[] segments = groupFilter.Split(['.', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return string.Join(" ", segments);
  }

  internal static string EscapeLikePattern(string input)
  {
    return input
      .Replace("\\", "\\\\", StringComparison.Ordinal)
      .Replace("%", "\\%", StringComparison.Ordinal)
      .Replace("_", "\\_", StringComparison.Ordinal);
  }
}
