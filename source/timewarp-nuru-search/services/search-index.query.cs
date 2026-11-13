#region Purpose
// Query side of SearchIndex: SearchAsync, FTS query sanitizing and group filter helpers.
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
      sqlBuilder.AppendLine("  AND e.cli_name = $cliName");
      cmd.Parameters.AddWithValue("$cliName", cliName);
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
