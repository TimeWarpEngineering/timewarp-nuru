# AOT CLI Framework Benchmark Results

**Date:** 2025-12-28 22:22:40
**Platform:** Unix 5.15.167.4
**Runtime:** 10.0.1

## AOT Compatibility

| Framework | AOT Support | Binary Size | Notes |
|-----------|-------------|-------------|-------|
| bench-consoleappframework | ✓ Yes | 2.5 MB |  |
| bench-systemcommandline | ✓ Yes | 3.3 MB |  |
| bench-clifx | ✓ Yes | 4.1 MB |  |
| bench-mcmaster | ✓ Yes | 5.2 MB |  |
| bench-nuru-direct | ✗ No | N/A | Executable not found |
| bench-nuru-full | ✓ Yes | 11.3 MB |  |
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
| 9 | bench-spectreconsole | 9.9 MB |
| 10 | bench-nuru-full | 11.3 MB |

## Cold Start Performance (hyperfine)

| Command | Mean [ms] | Min [ms] | Max [ms] | Relative |
|:---|---:|---:|---:|---:|
| `bench-consoleappframework` | 2.8 ± 0.6 | 2.0 | 4.7 | 1.00 |
| `bench-systemcommandline` | 3.1 ± 0.4 | 2.5 | 4.8 | 1.10 ± 0.28 |
| `bench-clifx` | 71.2 ± 21.5 | 51.4 | 162.9 | 25.04 ± 9.40 |
| `bench-mcmaster` | 76.6 ± 23.2 | 54.0 | 191.2 | 26.92 ± 10.14 |
| `bench-nuru-full` | 5.8 ± 0.3 | 5.2 | 6.7 | 2.04 ± 0.47 |
| `bench-powerargs` | 83.4 ± 26.0 | 55.7 | 166.1 | 29.31 ± 11.26 |
| `bench-commandlineparser` | 4.7 ± 1.5 | 3.4 | 12.2 | 1.67 ± 0.65 |
| `bench-cocona` | 14.5 ± 3.2 | 12.9 | 30.2 | 5.11 ± 1.61 |
| `bench-coconalite` | 4.6 ± 0.6 | 3.8 | 7.1 | 1.60 ± 0.41 |
| `bench-spectreconsole` | 6.0 ± 0.4 | 4.9 | 7.0 | 2.11 ± 0.49 |


## Failed Builds (AOT Not Supported)

### bench-nuru-direct

**Error:** Executable not found

## Summary

- **Total frameworks tested:** 11
- **AOT compatible:** 10
- **AOT incompatible:** 1

