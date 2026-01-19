# AOT CLI Framework Benchmark Results

**Date:** 2025-12-28 23:37:55
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
| `bench-consoleappframework` | 2.9 ± 0.8 | 2.0 | 8.1 | 1.00 |
| `bench-systemcommandline` | 3.3 ± 0.3 | 2.4 | 4.4 | 1.16 ± 0.35 |
| `bench-clifx` | 68.2 ± 22.7 | 47.8 | 154.3 | 23.90 ± 10.40 |
| `bench-mcmaster` | 78.5 ± 22.7 | 56.6 | 186.1 | 27.51 ± 11.09 |
| `bench-nuru-full` | 6.2 ± 0.6 | 5.1 | 8.0 | 2.17 ± 0.65 |
| `bench-powerargs` | 80.9 ± 25.9 | 52.7 | 223.0 | 28.33 ± 12.07 |
| `bench-commandlineparser` | 4.8 ± 0.4 | 3.8 | 6.1 | 1.67 ± 0.49 |
| `bench-cocona` | 16.0 ± 3.6 | 12.8 | 33.8 | 5.59 ± 2.01 |
| `bench-coconalite` | 5.1 ± 1.0 | 3.8 | 10.2 | 1.78 ± 0.62 |
| `bench-spectreconsole` | 6.2 ± 0.5 | 5.0 | 7.4 | 2.16 ± 0.63 |


## Failed Builds (AOT Not Supported)

### bench-nuru-direct

**Error:** Executable not found

## Summary

- **Total frameworks tested:** 11
- **AOT compatible:** 10
- **AOT incompatible:** 1

