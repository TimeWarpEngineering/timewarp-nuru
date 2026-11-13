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

`--cli` selects one CLI by its app name (the `WithName` value, shown in brackets in results) or by
the file name of its executable. Both select the same CLI: an app built with `WithName("probe")`
from an executable named `core-routing` is found by `--cli probe` and by `--cli core-routing`. When
neither name is indexed yet, `--cli` looks the value up on PATH and indexes that executable first.

### List indexed CLIs

```bash
nuru index list
```

### Clear index

```bash
nuru index clear
nuru index clear --cli my-cli
```

`index clear --cli` takes the same app name or executable name.

## Features

- **SQLite FTS5** - Full-text search with Porter stemmer for better word matching
- **Multi-CLI support** - Index and search across multiple CLIs
- **AOT compatible** - Fast startup, single binary deployment
- **On-demand indexing** - No background services, index when needed

## Database Location

The search index is stored at `~/.nuru/index.db`.
