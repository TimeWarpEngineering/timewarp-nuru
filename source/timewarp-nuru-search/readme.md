# TimeWarp.Nuru.Search

A CLI search tool for indexing and searching Nuru CLI endpoints across multiple CLIs.

## Installation

```bash
dotnet tool install -g TimeWarp.Nuru.Search
```

## Usage

### Index a CLI

```bash
nuru index rebuild --cli /path/to/cli
```

### Search endpoints

```bash
nuru search build
nuru search --cli my-cli deploy
nuru search --query "greet name"
```

### List indexed CLIs

```bash
nuru index list
```

### Clear index

```bash
nuru index clear
nuru index clear --cli my-cli
```

## Features

- **SQLite FTS5** - Full-text search with Porter stemmer for better word matching
- **Multi-CLI support** - Index and search across multiple CLIs
- **AOT compatible** - Fast startup, single binary deployment
- **On-demand indexing** - No background services, index when needed

## Database Location

The search index is stored at `~/.nuru/index.db`.
