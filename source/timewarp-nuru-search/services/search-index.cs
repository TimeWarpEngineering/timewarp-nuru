#region Purpose
// SQLite-backed store for indexed CLI endpoints: persists capability catalogs and answers search queries.
#endregion

namespace TimeWarp.Nuru.Search.Services;

public sealed partial class SearchIndex : IAsyncDisposable
{
  private readonly SqliteConnection Connection;
  private readonly ILogger<SearchIndex> Logger;
  private readonly string DataSource;
  private bool Initialized;

  public SearchIndex(ILogger<SearchIndex> logger) : this(logger, DatabasePath.GetIndexPath())
  {
  }

  /// <summary>
  /// Opens the index at an explicit SQLite data source (e.g. <c>:memory:</c>). Used by tests so
  /// the production path under <c>~/.nuru</c> is never touched.
  /// </summary>
  internal SearchIndex(ILogger<SearchIndex> logger, string dataSource)
  {
    ArgumentNullException.ThrowIfNull(logger);
    ArgumentException.ThrowIfNullOrEmpty(dataSource);

    this.Logger = logger;
    this.DataSource = dataSource;
    SqliteConnectionStringBuilder builder = new() { DataSource = dataSource };
    Connection = new SqliteConnection(builder.ConnectionString);
  }

  public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
  {
    if (Initialized)
    {
      return;
    }

    await Connection.OpenAsync(cancellationToken).ConfigureAwait(false);

    await CreateSchemaAsync(cancellationToken).ConfigureAwait(false);
    Initialized = true;
    LogInitialized(Logger, DataSource);
  }

  [LoggerMessage(LogLevel.Information, "Search index initialized at {Path}")]
  private static partial void LogInitialized(ILogger logger, string path);

  private async Task CreateSchemaAsync(CancellationToken cancellationToken)
  {
    string createClisTable = """
      CREATE TABLE IF NOT EXISTS clis (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL UNIQUE,
        cli_path TEXT,
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

    await using (SqliteCommand cmd = Connection.CreateCommand())
    {
      cmd.CommandText = createClisTable;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (SqliteCommand cmd = Connection.CreateCommand())
    {
      cmd.CommandText = createEndpointsTable;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (SqliteCommand cmd = Connection.CreateCommand())
    {
      cmd.CommandText = createFtsTable;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (SqliteCommand cmd = Connection.CreateCommand())
    {
      cmd.CommandText = createInsertTrigger;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (SqliteCommand cmd = Connection.CreateCommand())
    {
      cmd.CommandText = createDeleteTrigger;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await using (SqliteCommand cmd = Connection.CreateCommand())
    {
      cmd.CommandText = createUpdateTrigger;
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    await EnsureCliPathColumnAsync(cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Adds <c>clis.cli_path</c> to an index created before the column existed (482-011 / S-3).
  /// Rows from that index keep a null path; rebuild falls back to the CLI name for them.
  /// </summary>
  private async Task EnsureCliPathColumnAsync(CancellationToken cancellationToken)
  {
    await using (SqliteCommand cmd = Connection.CreateCommand())
    {
      cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('clis') WHERE name = 'cli_path'";
      long count = (long)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0L);

      if (count > 0)
      {
        return;
      }
    }

    await using SqliteCommand alter = Connection.CreateCommand();
    alter.CommandText = "ALTER TABLE clis ADD COLUMN cli_path TEXT";
    await alter.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Indexes a CLI under its capabilities <paramref name="cliName"/> (the app's WithName). A
  /// <c>--cli</c> filter matches that name or the file name of <paramref name="cliPath"/>; see
  /// <see cref="ResolveCliNamesAsync"/>. <paramref name="cliPath"/> is
  /// the executable that was actually run; <c>index rebuild --all</c> runs it again. The name and
  /// the path differ when the executable file name is not the capabilities name.
  /// </summary>
  public async Task IndexCliAsync(
    string cliName,
    string version,
    string capabilitiesJson,
    IReadOnlyList<EndpointCapability> endpoints,
    string? cliPath = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(cliName);
    ArgumentNullException.ThrowIfNull(version);
    ArgumentNullException.ThrowIfNull(capabilitiesJson);
    ArgumentNullException.ThrowIfNull(endpoints);

    if (!Initialized)
    {
      await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    await using DbTransaction transaction = await Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

    try
    {
      await DeleteCliEndpointsAsync(cliName, transaction, cancellationToken).ConfigureAwait(false);

      await using (SqliteCommand cmd = Connection.CreateCommand())
      {
        cmd.Transaction = (SqliteTransaction)transaction;
        cmd.CommandText = """
          INSERT OR REPLACE INTO clis (name, cli_path, version, indexed_at, capabilities_json)
          VALUES ($name, $cliPath, $version, $indexedAt, $capabilitiesJson)
          """;
        cmd.Parameters.AddWithValue("$name", cliName);
        cmd.Parameters.AddWithValue("$cliPath", cliPath ?? (object)DBNull.Value);
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
      LogIndexedEndpoints(Logger, endpoints.Count, cliName);
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
    await using SqliteCommand cmd = Connection.CreateCommand();
    cmd.Transaction = (SqliteTransaction)transaction;
    cmd.CommandText = "DELETE FROM endpoints WHERE cli_name = $cliName";
    cmd.Parameters.AddWithValue("$cliName", cliName);
    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

    await using SqliteCommand cmd2 = Connection.CreateCommand();
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
    await using SqliteCommand cmd = Connection.CreateCommand();
    cmd.Transaction = (SqliteTransaction)transaction;
    cmd.CommandText = """
      INSERT INTO endpoints (cli_name, pattern, description, group_path, endpoint_json)
      VALUES ($cliName, $pattern, $description, $groupPath, $endpointJson)
      """;
    cmd.Parameters.AddWithValue("$cliName", cliName);
    cmd.Parameters.AddWithValue("$pattern", endpoint.Pattern);
    cmd.Parameters.AddWithValue("$description", endpoint.Description ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("$groupPath", string.Join(" ", endpoint.GroupPath));
    cmd.Parameters.AddWithValue("$endpointJson", JsonSerializer.Serialize(endpoint, SearchIndexJsonContext.Default.EndpointCapability));
    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<CliInfo>> ListClisAsync(CancellationToken cancellationToken = default)
  {
    if (!Initialized)
    {
      await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    List<CliInfo> clis = [];

    await using SqliteCommand cmd = Connection.CreateCommand();
    cmd.CommandText = """
      SELECT name, version, indexed_at,
             (SELECT COUNT(*) FROM endpoints WHERE cli_name = clis.name) as endpoint_count,
             cli_path
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
        EndpointCount = reader.GetInt32(3),
        CliPath = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(4)
      });
    }

    return clis;
  }

  public async Task ClearIndexAsync(CancellationToken cancellationToken = default)
  {
    if (!Initialized)
    {
      await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    await using SqliteCommand cmd = Connection.CreateCommand();
    cmd.CommandText = "DELETE FROM endpoints";
    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

    await using SqliteCommand cmd2 = Connection.CreateCommand();
    cmd2.CommandText = "DELETE FROM clis";
    await cmd2.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

    LogClearedAll(Logger);
  }

  [LoggerMessage(LogLevel.Information, "Cleared all indexed data")]
  private static partial void LogClearedAll(ILogger logger);

  public async Task ClearCliAsync(string cliName, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(cliName);

    if (!Initialized)
    {
      await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    await using DbTransaction transaction = await Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

    try
    {
      await DeleteCliEndpointsAsync(cliName, transaction, cancellationToken).ConfigureAwait(false);
      await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
      LogClearedCli(Logger, cliName);
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
    await Connection.CloseAsync().ConfigureAwait(false);
    await Connection.DisposeAsync().ConfigureAwait(false);
  }
}

public sealed class SearchResult
{
  public required string CliName { get; init; }

  /// <summary>
  /// Version of the CLI that owns this endpoint, as recorded when it was indexed
  /// (<c>clis.version</c>). Null only if the owning <c>clis</c> row is missing.
  /// </summary>
  public string? CliVersion { get; init; }

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

  /// <summary>Executable that was run to index this CLI. Null for rows indexed before the path was stored.</summary>
  public string? CliPath { get; init; }
}
