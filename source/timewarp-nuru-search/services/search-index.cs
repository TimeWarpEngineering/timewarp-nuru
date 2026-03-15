namespace TimeWarp.Nuru.Search.Services;

public sealed partial class SearchIndex : IAsyncDisposable
{
  private readonly SqliteConnection connection;
  private readonly ILogger<SearchIndex> logger;
  private bool initialized;

  public SearchIndex(ILogger<SearchIndex> logger)
  {
    this.logger = logger;
    string dbPath = DatabasePath.GetIndexPath();
    string connectionString = $"Data Source={dbPath}";
    connection = new SqliteConnection(connectionString);
  }

  public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
  {
    if (initialized)
    {
      return;
    }

    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

    await CreateSchemaAsync(cancellationToken).ConfigureAwait(false);
    initialized = true;
    LogInitialized(logger, DatabasePath.GetIndexPath());
  }

  [LoggerMessage(LogLevel.Information, "Search index initialized at {Path}")]
  private static partial void LogInitialized(ILogger logger, string path);

  private async Task CreateSchemaAsync(CancellationToken cancellationToken)
  {
    string createClisTable = """
      CREATE TABLE IF NOT EXISTS clis (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL UNIQUE,
        version TEXT NOT NULL,
        indexed_at TEXT NOT NULL,
        capabilities_json TEXT NOT NULL
      )
      """;

    string createEndpointsTable = """
      CREATE TABLE IF NOT EXISTS endpoints (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        cli_name TEXT NOT NULL,
        pattern TEXT NOT NULL,
        description TEXT,
        group_path TEXT NOT NULL,
        endpoint_json TEXT NOT NULL,
        FOREIGN KEY (cli_name) REFERENCES clis(name) ON DELETE CASCADE
      )
      """;

    string createFtsTable = """
      CREATE VIRTUAL TABLE IF NOT EXISTS endpoints_fts USING fts5(
        pattern,
        description,
        cli_name,
        content='endpoints',
        content_rowid='id',
        tokenize='porter unicode61'
      )
      """;

    string createInsertTrigger = """
      CREATE TRIGGER IF NOT EXISTS endpoints_ai AFTER INSERT ON endpoints BEGIN
        INSERT INTO endpoints_fts(rowid, pattern, description, cli_name)
        VALUES (new.id, new.pattern, COALESCE(new.description, ''), new.cli_name);
      END
      """;

    string createDeleteTrigger = """
      CREATE TRIGGER IF NOT EXISTS endpoints_ad AFTER DELETE ON endpoints BEGIN
        INSERT INTO endpoints_fts(endpoints_fts, rowid, pattern, description, cli_name)
        VALUES ('delete', old.id, old.pattern, COALESCE(old.description, ''), old.cli_name);
      END
      """;

    string createUpdateTrigger = """
      CREATE TRIGGER IF NOT EXISTS endpoints_au AFTER UPDATE ON endpoints BEGIN
        INSERT INTO endpoints_fts(endpoints_fts, rowid, pattern, description, cli_name)
        VALUES ('delete', old.id, old.pattern, COALESCE(old.description, ''), old.cli_name);
        INSERT INTO endpoints_fts(rowid, pattern, description, cli_name)
        VALUES (new.id, new.pattern, COALESCE(new.description, ''), new.cli_name);
      END
      """;

    await using (SqliteCommand cmd = connection.CreateCommand())
    {
      cmd.CommandText = createClisTable;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (SqliteCommand cmd = connection.CreateCommand())
    {
      cmd.CommandText = createEndpointsTable;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (SqliteCommand cmd = connection.CreateCommand())
    {
      cmd.CommandText = createFtsTable;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (SqliteCommand cmd = connection.CreateCommand())
    {
      cmd.CommandText = createInsertTrigger;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (SqliteCommand cmd = connection.CreateCommand())
    {
      cmd.CommandText = createDeleteTrigger;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (SqliteCommand cmd = connection.CreateCommand())
    {
      cmd.CommandText = createUpdateTrigger;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
  }

  public async Task IndexCliAsync(
    string cliName,
    string version,
    string capabilitiesJson,
    IReadOnlyList<EndpointCapability> endpoints,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(cliName);
    ArgumentNullException.ThrowIfNull(version);
    ArgumentNullException.ThrowIfNull(capabilitiesJson);
    ArgumentNullException.ThrowIfNull(endpoints);

    if (!initialized)
    {
      await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

    try
    {
      await DeleteCliEndpointsAsync(cliName, transaction, cancellationToken).ConfigureAwait(false);

      await using (SqliteCommand cmd = connection.CreateCommand())
      {
        cmd.Transaction = (SqliteTransaction)transaction;
        cmd.CommandText = """
          INSERT OR REPLACE INTO clis (name, version, indexed_at, capabilities_json)
          VALUES ($name, $version, $indexedAt, $capabilitiesJson)
          """;
        cmd.Parameters.AddWithValue("$name", cliName);
        cmd.Parameters.AddWithValue("$version", version);
        cmd.Parameters.AddWithValue("$indexedAt", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$capabilitiesJson", capabilitiesJson);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
      }

      foreach (EndpointCapability endpoint in endpoints)
      {
        await InsertEndpointAsync(cliName, endpoint, transaction, cancellationToken).ConfigureAwait(false);
      }

      await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
      LogIndexedEndpoints(logger, endpoints.Count, cliName);
    }
    catch
    {
      await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
      throw;
    }
  }

  [LoggerMessage(LogLevel.Information, "Indexed {Count} endpoints for CLI {CliName}")]
  private static partial void LogIndexedEndpoints(ILogger logger, int count, string cliName);

  private async Task DeleteCliEndpointsAsync(string cliName, DbTransaction transaction, CancellationToken cancellationToken)
  {
    await using SqliteCommand cmd = connection.CreateCommand();
    cmd.Transaction = (SqliteTransaction)transaction;
    cmd.CommandText = "DELETE FROM endpoints WHERE cli_name = $cliName";
    cmd.Parameters.AddWithValue("$cliName", cliName);
    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

    await using SqliteCommand cmd2 = connection.CreateCommand();
    cmd2.Transaction = (SqliteTransaction)transaction;
    cmd2.CommandText = "DELETE FROM clis WHERE name = $cliName";
    cmd2.Parameters.AddWithValue("$cliName", cliName);
    await cmd2.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
  }

  private async Task InsertEndpointAsync(
    string cliName,
    EndpointCapability endpoint,
    DbTransaction transaction,
    CancellationToken cancellationToken)
  {
    await using SqliteCommand cmd = connection.CreateCommand();
    cmd.Transaction = (SqliteTransaction)transaction;
    cmd.CommandText = """
      INSERT INTO endpoints (cli_name, pattern, description, group_path, endpoint_json)
      VALUES ($cliName, $pattern, $description, $groupPath, $endpointJson)
      """;
    cmd.Parameters.AddWithValue("$cliName", cliName);
    cmd.Parameters.AddWithValue("$pattern", endpoint.Pattern);
    cmd.Parameters.AddWithValue("$description", endpoint.Description ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("$groupPath", string.Join(" ", endpoint.GroupPath));
    cmd.Parameters.AddWithValue("$endpointJson", JsonSerializer.Serialize(endpoint));
    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<SearchResult>> SearchAsync(
    string query,
    string? cliName = null,
    string? groupPath = null,
    int limit = 50,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(query);

    if (!initialized)
    {
      await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    string sanitizedQuery = SanitizeFtsQuery(query);
    List<SearchResult> results = [];

    await using SqliteCommand cmd = connection.CreateCommand();

    StringBuilder sqlBuilder = new();
    sqlBuilder.AppendLine("""
      SELECT e.cli_name, e.pattern, e.description, e.group_path, e.endpoint_json, endpoints_fts.rank
      FROM endpoints_fts
      JOIN endpoints e ON endpoints_fts.rowid = e.id
      WHERE endpoints_fts MATCH $query
      """);

    if (!string.IsNullOrEmpty(cliName))
    {
      sqlBuilder.AppendLine("  AND e.cli_name = $cliName");
      cmd.Parameters.AddWithValue("$cliName", cliName);
    }

    if (!string.IsNullOrEmpty(groupPath))
    {
      sqlBuilder.AppendLine("  AND e.group_path LIKE $groupPath || '%'");
      cmd.Parameters.AddWithValue("$groupPath", groupPath);
    }

    sqlBuilder.AppendLine("  ORDER BY endpoints_fts.rank");
    sqlBuilder.AppendLine("  LIMIT $limit");

#pragma warning disable CA2100 // SQL is built from our own code, user input goes through parameters
    cmd.CommandText = sqlBuilder.ToString();
#pragma warning restore CA2100

    cmd.Parameters.AddWithValue("$query", sanitizedQuery);
    cmd.Parameters.AddWithValue("$limit", limit);

    await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
    {
      string endpointJson = reader.GetString(4);
      EndpointCapability? endpoint = JsonSerializer.Deserialize<EndpointCapability>(endpointJson);

      if (endpoint is not null)
      {
        results.Add(new SearchResult
        {
          CliName = reader.GetString(0),
          Pattern = reader.GetString(1),
          Description = await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(2),
          GroupPath = reader.GetString(3),
          Endpoint = endpoint
        });
      }
    }

    return results;
  }

  private static string SanitizeFtsQuery(string query)
  {
    string[] tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    List<string> sanitizedTokens = [];

    foreach (string token in tokens)
    {
      string escaped = token
        .Replace("'", "''", StringComparison.Ordinal)
        .Replace("\"", "\"\"", StringComparison.Ordinal)
        .Replace("[", "[[", StringComparison.Ordinal)
        .Replace("]", "]]", StringComparison.Ordinal)
        .Replace("(", "((", StringComparison.Ordinal)
        .Replace(")", "))", StringComparison.Ordinal)
        .Replace("*", "", StringComparison.Ordinal)
        .Replace("^", "", StringComparison.Ordinal);

      if (!string.IsNullOrWhiteSpace(escaped))
      {
        sanitizedTokens.Add($"{escaped}*");
      }
    }

    return string.Join(" ", sanitizedTokens);
  }

  public async Task<IReadOnlyList<CliInfo>> ListClisAsync(CancellationToken cancellationToken = default)
  {
    if (!initialized)
    {
      await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    List<CliInfo> clis = [];

    await using SqliteCommand cmd = connection.CreateCommand();
    cmd.CommandText = """
      SELECT name, version, indexed_at, 
             (SELECT COUNT(*) FROM endpoints WHERE cli_name = clis.name) as endpoint_count
      FROM clis
      ORDER BY name
      """;

    await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
    {
      clis.Add(new CliInfo
      {
        Name = reader.GetString(0),
        Version = reader.GetString(1),
        IndexedAt = DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
        EndpointCount = reader.GetInt32(3)
      });
    }

    return clis;
  }

  public async Task ClearIndexAsync(CancellationToken cancellationToken = default)
  {
    if (!initialized)
    {
      await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    await using SqliteCommand cmd = connection.CreateCommand();
    cmd.CommandText = "DELETE FROM endpoints";
    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

    await using SqliteCommand cmd2 = connection.CreateCommand();
    cmd2.CommandText = "DELETE FROM clis";
    await cmd2.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

    LogClearedAll(logger);
  }

  [LoggerMessage(LogLevel.Information, "Cleared all indexed data")]
  private static partial void LogClearedAll(ILogger logger);

  public async Task ClearCliAsync(string cliName, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(cliName);

    if (!initialized)
    {
      await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

    try
    {
      await DeleteCliEndpointsAsync(cliName, transaction, cancellationToken).ConfigureAwait(false);
      await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
      LogClearedCli(logger, cliName);
    }
    catch
    {
      await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
      throw;
    }
  }

  [LoggerMessage(LogLevel.Information, "Cleared index for CLI {CliName}")]
  private static partial void LogClearedCli(ILogger logger, string cliName);

  public async ValueTask DisposeAsync()
  {
    await connection.CloseAsync().ConfigureAwait(false);
    await connection.DisposeAsync().ConfigureAwait(false);
  }
}

public sealed class SearchResult
{
  public required string CliName { get; init; }
  public required string Pattern { get; init; }
  public string? Description { get; init; }
  public required string GroupPath { get; init; }
  public required EndpointCapability Endpoint { get; init; }
}

public sealed class CliInfo
{
  public required string Name { get; init; }
  public required string Version { get; init; }
  public required DateTime IndexedAt { get; init; }
  public required int EndpointCount { get; init; }
}
