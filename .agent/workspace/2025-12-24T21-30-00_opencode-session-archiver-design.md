# OpenCode Session Archiver - Design & Implementation Analysis

## Executive Summary

This report analyzes the feasibility of building a C# CLI application using TimeWarp.Nuru that captures OpenCode AI coding sessions and stores them in PostgreSQL with full-text search (using pg_textsearch BM25) and optional semantic search (using pgvector). The analysis covers the OpenCode SDK/Server APIs, storage formats, PostgreSQL extensions, and provides a complete implementation blueprint.

## Scope

- **OpenCode SDK/Server**: REST API for session, message, and part retrieval
- **pg_textsearch**: BM25-ranked full-text search PostgreSQL extension
- **pgvector**: Vector similarity search for semantic capabilities
- **TimeWarp.Nuru**: CLI framework for building the archiver tool

## Methodology

1. Fetched and analyzed OpenCode SDK documentation (https://opencode.ai/docs/sdk/)
2. Fetched and analyzed pg_textsearch repository (https://github.com/timescale/pg_textsearch)
3. Fetched and analyzed pgvector repository (https://github.com/pgvector/pgvector)
4. Examined local OpenCode storage structure (~/.local/share/opencode/storage/)
5. Reviewed TimeWarp.Nuru samples and patterns

---

## Findings

### 1. OpenCode Data Architecture

OpenCode stores sessions in a hierarchical JSON structure:

```
~/.local/share/opencode/storage/
├── session/           # Session metadata (JSON files)
│   └── ses_xxx.json   # {id, title, created, updated, parentID, ...}
├── message/           # Messages organized by session
│   └── ses_xxx/
│       └── msg_xxx.json  # {id, sessionID, role, time, summary, agent, model}
├── part/              # Message parts (content)
│   └── msg_xxx/
│       └── prt_xxx.json  # {id, messageID, type, text, tool, ...}
├── project/           # Project metadata
├── session_diff/      # Git diffs per session
├── session_share/     # Sharing metadata
└── todo/              # Todo items per session
```

**Key Data Types:**

| Type | Description | Key Fields |
|------|-------------|------------|
| Session | Conversation container | id, title, created, updated, parentID, shareID |
| Message | Individual turn | id, sessionID, role (user/assistant), time, agent, model |
| Part | Content unit | id, messageID, type (text/tool/patch/step-start/step-finish), text |

### 2. OpenCode Server API

The OpenCode server exposes a REST API (default: http://localhost:4096):

```
# Session endpoints
GET  /session              # List all sessions
GET  /session/:id          # Get session details
GET  /session/:id/message  # Get messages in session
GET  /session/:id/todo     # Get todos for session

# Message endpoints  
GET  /session/:id/message/:messageID  # Get message with parts

# Real-time events
GET  /event                # SSE stream for live updates
```

### 3. pg_textsearch (BM25 Full-Text Search)

**Status**: v0.1.1-dev (prerelease), PostgreSQL 17/18 supported

**Features**:
- BM25 ranking algorithm (industry standard for relevance)
- Simple syntax: `ORDER BY content <@> 'search terms'`
- Configurable parameters (k1, b)
- Works with Postgres text search configurations (english, french, etc.)
- Supports partitioned tables

**Installation**:
```sql
CREATE EXTENSION pg_textsearch;
CREATE INDEX ON sessions USING bm25(content) WITH (text_config='english');
```

**Query Syntax**:
```sql
SELECT * FROM sessions
ORDER BY content <@> 'database optimization'
LIMIT 10;
```

### 4. pgvector (Semantic Search)

**Status**: v0.8.1 (stable), 18.9k GitHub stars

**Features**:
- Vector similarity search (L2, cosine, inner product)
- HNSW and IVFFlat indexes
- Up to 16,000 dimensions
- Works with any embedding model (OpenAI, Ollama, etc.)

**Installation**:
```sql
CREATE EXTENSION vector;
CREATE TABLE sessions (
    id TEXT PRIMARY KEY,
    content TEXT,
    embedding vector(1536)  -- OpenAI ada-002 dimensions
);
CREATE INDEX ON sessions USING hnsw (embedding vector_cosine_ops);
```

**Query Syntax**:
```sql
SELECT *, embedding <=> '[0.1, 0.2, ...]' AS distance
FROM sessions
ORDER BY distance
LIMIT 10;
```

### 5. Hybrid Search Strategy

Combine BM25 keyword search with semantic similarity using Reciprocal Rank Fusion (RRF):

```sql
WITH keyword_results AS (
    SELECT id, ROW_NUMBER() OVER (ORDER BY content <@> 'query') AS rank
    FROM sessions
    ORDER BY content <@> 'query'
    LIMIT 20
),
semantic_results AS (
    SELECT id, ROW_NUMBER() OVER (ORDER BY embedding <=> '[...]') AS rank
    FROM sessions
    ORDER BY embedding <=> '[...]'
    LIMIT 20
)
SELECT COALESCE(k.id, s.id) AS id,
       COALESCE(1.0 / (60 + k.rank), 0) + COALESCE(1.0 / (60 + s.rank), 0) AS rrf_score
FROM keyword_results k
FULL OUTER JOIN semantic_results s ON k.id = s.id
ORDER BY rrf_score DESC
LIMIT 10;
```

---

## Implementation Blueprint

### Database Schema

```sql
-- Enable extensions
CREATE EXTENSION IF NOT EXISTS pg_textsearch;
CREATE EXTENSION IF NOT EXISTS vector;

-- Sessions table
CREATE TABLE sessions (
    id TEXT PRIMARY KEY,
    project_id TEXT,
    parent_id TEXT REFERENCES sessions(id),
    title TEXT,
    summary TEXT,
    agent TEXT,
    model_provider TEXT,
    model_id TEXT,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    share_id TEXT,
    
    -- Full content for search (aggregated from parts)
    full_content TEXT NOT NULL,
    
    -- Semantic search embedding (optional)
    embedding vector(1536),  -- OpenAI ada-002 or configurable
    
    -- Metadata
    message_count INT DEFAULT 0,
    token_count_input INT DEFAULT 0,
    token_count_output INT DEFAULT 0
);

-- BM25 full-text index
CREATE INDEX sessions_bm25_idx ON sessions 
    USING bm25(full_content) WITH (text_config='english');

-- Semantic search index (optional)
CREATE INDEX sessions_embedding_idx ON sessions 
    USING hnsw (embedding vector_cosine_ops);

-- Messages table
CREATE TABLE messages (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,
    role TEXT NOT NULL,  -- 'user' or 'assistant'
    agent TEXT,
    model_provider TEXT,
    model_id TEXT,
    created_at TIMESTAMPTZ NOT NULL,
    summary_title TEXT,
    
    -- Full content (aggregated from parts)
    full_content TEXT NOT NULL,
    
    -- Semantic search embedding (optional)
    embedding vector(1536)
);

-- BM25 index for message search
CREATE INDEX messages_bm25_idx ON messages 
    USING bm25(full_content) WITH (text_config='english');

CREATE INDEX messages_session_idx ON messages(session_id);

-- Parts table (optional - for granular search)
CREATE TABLE parts (
    id TEXT PRIMARY KEY,
    message_id TEXT NOT NULL REFERENCES messages(id) ON DELETE CASCADE,
    session_id TEXT NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,
    part_type TEXT NOT NULL,  -- 'text', 'tool', 'patch', etc.
    content TEXT,
    tool_name TEXT,
    tool_input JSONB,
    tool_output TEXT,
    created_at TIMESTAMPTZ
);

CREATE INDEX parts_message_idx ON parts(message_id);

-- Sync tracking
CREATE TABLE sync_state (
    id SERIAL PRIMARY KEY,
    last_sync_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    sessions_synced INT DEFAULT 0,
    messages_synced INT DEFAULT 0
);
```

### Nuru CLI Application Structure

```csharp
#!/usr/bin/dotnet --
// opencode-archiver - Archive and search OpenCode sessions
#:project path/to/timewarp-nuru.csproj
#:package Npgsql
#:package Microsoft.Extensions.Http
#:package System.Text.Json

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
    .ConfigureServices(ConfigureServices)
    
    // Sync commands
    .Map("sync")
        .WithHandler(SyncAllSessionsAsync)
        .WithDescription("Sync all OpenCode sessions to PostgreSQL")
        .AsCommand()
        .Done()
    .Map("sync --since {date:DateTime}")
        .WithHandler(SyncSinceAsync)
        .WithDescription("Sync sessions modified since date")
        .AsCommand()
        .Done()
    .Map("sync --session {sessionId}")
        .WithHandler(SyncSessionAsync)
        .WithDescription("Sync a specific session")
        .AsCommand()
        .Done()
    
    // Search commands
    .Map("search {query}")
        .WithHandler(SearchSessionsAsync)
        .WithDescription("Full-text search sessions using BM25")
        .AsQuery()
        .Done()
    .Map("search {query} --limit {limit:int}")
        .WithHandler(SearchSessionsWithLimitAsync)
        .WithDescription("Search with custom limit")
        .AsQuery()
        .Done()
    .Map("search {query} --semantic")
        .WithHandler(SemanticSearchAsync)
        .WithDescription("Semantic search using embeddings")
        .AsQuery()
        .Done()
    .Map("search {query} --hybrid")
        .WithHandler(HybridSearchAsync)
        .WithDescription("Combined keyword + semantic search")
        .AsQuery()
        .Done()
    
    // View commands
    .Map("show {sessionId}")
        .WithHandler(ShowSessionAsync)
        .WithDescription("Display session details")
        .AsQuery()
        .Done()
    .Map("list")
        .WithHandler(ListSessionsAsync)
        .WithDescription("List all archived sessions")
        .AsQuery()
        .Done()
    .Map("list --recent {count:int}")
        .WithHandler(ListRecentAsync)
        .WithDescription("List recent sessions")
        .AsQuery()
        .Done()
    
    // Database management
    .Map("db init")
        .WithHandler(InitDatabaseAsync)
        .WithDescription("Initialize database schema")
        .AsCommand()
        .Done()
    .Map("db status")
        .WithHandler(DatabaseStatusAsync)
        .WithDescription("Show database statistics")
        .AsQuery()
        .Done()
    
    .Build();

return await app.RunAsync(args);
```

### Core Service Implementations

```csharp
// OpenCode client service
public class OpenCodeClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    
    public OpenCodeClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _baseUrl = config["OpenCode:BaseUrl"] ?? "http://localhost:4096";
    }
    
    public async Task<Session[]> GetSessionsAsync()
    {
        return await _http.GetFromJsonAsync<Session[]>($"{_baseUrl}/session") 
            ?? Array.Empty<Session>();
    }
    
    public async Task<SessionMessages> GetSessionMessagesAsync(string sessionId)
    {
        return await _http.GetFromJsonAsync<SessionMessages>(
            $"{_baseUrl}/session/{sessionId}/message") 
            ?? new SessionMessages();
    }
}

// PostgreSQL repository
public class SessionRepository
{
    private readonly string _connectionString;
    
    public async Task UpsertSessionAsync(Session session, string fullContent)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO sessions (id, title, full_content, created_at, updated_at)
            VALUES ($1, $2, $3, $4, $5)
            ON CONFLICT (id) DO UPDATE SET
                title = EXCLUDED.title,
                full_content = EXCLUDED.full_content,
                updated_at = EXCLUDED.updated_at", conn);
        
        cmd.Parameters.AddWithValue(session.Id);
        cmd.Parameters.AddWithValue(session.Title ?? "");
        cmd.Parameters.AddWithValue(fullContent);
        cmd.Parameters.AddWithValue(session.CreatedAt);
        cmd.Parameters.AddWithValue(session.UpdatedAt);
        
        await cmd.ExecuteNonQueryAsync();
    }
    
    public async Task<SearchResult[]> SearchAsync(string query, int limit = 10)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand(@"
            SELECT id, title, 
                   full_content <@> $1 AS score,
                   LEFT(full_content, 500) AS snippet
            FROM sessions
            ORDER BY full_content <@> $1
            LIMIT $2", conn);
        
        cmd.Parameters.AddWithValue(query);
        cmd.Parameters.AddWithValue(limit);
        
        var results = new List<SearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new SearchResult
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                Score = reader.GetDouble(2),
                Snippet = reader.GetString(3)
            });
        }
        return results.ToArray();
    }
}
```

### Configuration File (opencode-archiver.settings.json)

```json
{
  "OpenCode": {
    "BaseUrl": "http://localhost:4096",
    "StoragePath": "~/.local/share/opencode/storage"
  },
  "Database": {
    "ConnectionString": "Host=localhost;Database=opencode_archive;Username=postgres;Password=xxx",
    "EnableSemanticSearch": false
  },
  "Embedding": {
    "Provider": "openai",
    "Model": "text-embedding-ada-002",
    "Dimensions": 1536,
    "ApiKey": "${OPENAI_API_KEY}"
  },
  "Sync": {
    "BatchSize": 100,
    "IncludeToolCalls": true,
    "IncludePatches": false
  }
}
```

---

## Recommendations

### Phase 1: MVP with Full-Text Search

1. **Implement basic sync** from OpenCode file storage (faster than API for initial load)
2. **Use pg_textsearch** for BM25 keyword search
3. **Commands**: `sync`, `search`, `show`, `list`
4. **No embeddings** in MVP (simplify deployment)

### Phase 2: Real-Time Sync

1. **Subscribe to SSE events** via `/event` endpoint
2. **Incremental sync** on session changes
3. **Background service** option using .NET BackgroundService

### Phase 3: Semantic Search (Optional)

1. **Add pgvector** extension
2. **Integrate embedding API** (OpenAI, Ollama, local model)
3. **Implement hybrid search** with RRF scoring
4. **Cache embeddings** to avoid recomputation

### Implementation Checklist

- [ ] Create PostgreSQL database with extensions
- [ ] Implement Nuru CLI skeleton with route patterns
- [ ] Build OpenCode file reader (JSON parsing)
- [ ] Implement session/message sync logic
- [ ] Add BM25 search queries
- [ ] Add result formatting (table output)
- [ ] Add configuration validation
- [ ] Optional: OpenCode API client for live sync
- [ ] Optional: Embedding generation service
- [ ] Optional: Hybrid search implementation

---

## References

1. [OpenCode SDK Documentation](https://opencode.ai/docs/sdk/)
2. [OpenCode Server API](https://opencode.ai/docs/server/)
3. [pg_textsearch GitHub](https://github.com/timescale/pg_textsearch)
4. [pgvector GitHub](https://github.com/pgvector/pgvector)
5. [pgvector-dotnet](https://github.com/pgvector/pgvector-dotnet)
6. [TimeWarp.Nuru Samples](./samples/)

---

## Appendix: OpenCode Data Models

```csharp
public record Session(
    string Id,
    string? Title,
    string? ParentID,
    string? ShareID,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record Message(
    string Id,
    string SessionID,
    string Role,  // "user" | "assistant"
    string? Agent,
    ModelInfo? Model,
    MessageTime Time,
    MessageSummary? Summary
);

public record ModelInfo(string ProviderID, string ModelID);

public record MessageTime(long Created);

public record MessageSummary(string? Title, string[]? Diffs);

public record Part(
    string Id,
    string MessageID,
    string SessionID,
    string Type,  // "text" | "tool" | "patch" | "step-start" | "step-finish"
    string? Text,
    string? Tool,
    object? State
);

public record SessionMessages(
    MessageWithParts[] Messages
);

public record MessageWithParts(
    Message Info,
    Part[] Parts
);
```
