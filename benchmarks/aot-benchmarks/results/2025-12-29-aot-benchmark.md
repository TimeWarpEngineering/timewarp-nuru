# AOT CLI Framework Benchmark Results

**Date:** 2025-12-29 10:19:19
**Platform:** Unix 5.15.167.4
**Runtime:** 10.0.1

## AOT Compatibility

| Framework | AOT Support | Binary Size | Notes |
|-----------|-------------|-------------|-------|
| bench-consoleappframework | ✓ Yes | 2.5 MB |  |
| bench-systemcommandline | ✓ Yes | 3.3 MB |  |
| bench-clifx | ✓ Yes | 4.1 MB |  |
| bench-mcmaster | ✓ Yes | 5.2 MB |  |
| bench-nuru-direct | ✗ No | N/A | AOT publish failed (non-zero exit code) |
| bench-nuru-full | ✓ Yes | 9.9 MB |  |
| bench-powerargs | ✓ Yes | 5.0 MB |  |
| bench-commandlineparser | ✓ Yes | 4.5 MB |  |
| bench-cocona | ✓ Yes | 6.4 MB |  |
| bench-coconalite | ✓ Yes | 4.7 MB |  |
| bench-spectreconsole | ✓ Yes | 9.9 MB |  |

## Binary Size Ranking

| Rank | Framework | Size |
|------|-----------|------|
| 1 | bench-consoleappframework | 2.5 MB |
| 2 | bench-systemcommandline | 3.3 MB |
| 3 | bench-clifx | 4.1 MB |
| 4 | bench-commandlineparser | 4.5 MB |
| 5 | bench-coconalite | 4.7 MB |
| 6 | bench-powerargs | 5.0 MB |
| 7 | bench-mcmaster | 5.2 MB |
| 8 | bench-cocona | 6.4 MB |
| 9 | bench-nuru-full | 9.9 MB |
| 10 | bench-spectreconsole | 9.9 MB |

## Cold Start Performance (hyperfine)

| Command | Mean [ms] | Min [ms] | Max [ms] | Relative |
|:---|---:|---:|---:|---:|
| `bench-consoleappframework` | 3.1 ± 0.4 | 2.5 | 5.1 | 1.00 |
| `bench-systemcommandline` | 3.8 ± 0.4 | 3.2 | 4.9 | 1.24 ± 0.19 |
| `bench-clifx` | 74.7 ± 28.2 | 49.2 | 213.1 | 24.23 ± 9.64 |
| `bench-mcmaster` | 77.0 ± 27.3 | 54.0 | 203.0 | 24.96 ± 9.38 |
| `bench-nuru-full` | 4.5 ± 0.5 | 3.3 | 6.4 | 1.45 ± 0.24 |
| `bench-powerargs` | 84.6 ± 53.2 | 57.2 | 526.0 | 27.45 ± 17.60 |
| `bench-commandlineparser` | 4.1 ± 0.9 | 3.2 | 12.7 | 1.32 ± 0.35 |
| `bench-cocona` | 16.2 ± 3.9 | 14.0 | 36.3 | 5.27 ± 1.44 |
| `bench-coconalite` | 5.2 ± 0.6 | 4.2 | 7.7 | 1.70 ± 0.29 |
| `bench-spectreconsole` | 8.3 ± 2.8 | 6.3 | 21.0 | 2.70 ± 0.97 |


## Failed Builds (AOT Not Supported)

### bench-nuru-direct

**Error:** AOT publish failed (non-zero exit code)

## Summary

- **Total frameworks tested:** 11
- **AOT compatible:** 10
- **AOT incompatible:** 1

