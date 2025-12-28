# AOT CLI Framework Benchmark Results

**Date:** 2025-12-28 20:56:54
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
| `bench-consoleappframework` | 4.0 ± 2.0 | 2.3 | 12.3 | 1.16 ± 0.61 |
| `bench-systemcommandline` | 3.4 ± 0.4 | 2.5 | 5.4 | 1.00 |
| `bench-clifx` | 80.1 ± 33.3 | 53.8 | 229.5 | 23.46 ± 10.21 |
| `bench-mcmaster` | 86.0 ± 38.0 | 58.0 | 312.4 | 25.20 ± 11.59 |
| `bench-nuru-full` | 8.0 ± 2.1 | 5.9 | 18.3 | 2.33 ± 0.69 |
| `bench-powerargs` | 84.3 ± 30.5 | 60.5 | 277.6 | 24.69 ± 9.48 |
| `bench-commandlineparser` | 4.7 ± 0.6 | 3.6 | 7.8 | 1.38 ± 0.26 |
| `bench-cocona` | 16.9 ± 6.8 | 13.3 | 57.6 | 4.95 ± 2.10 |
| `bench-coconalite` | 4.7 ± 0.5 | 4.0 | 7.3 | 1.36 ± 0.22 |
| `bench-spectreconsole` | 7.8 ± 2.5 | 5.7 | 21.4 | 2.28 ± 0.79 |


## Failed Builds (AOT Not Supported)

### bench-nuru-direct

**Error:** Executable not found

## Summary

- **Total frameworks tested:** 11
- **AOT compatible:** 10
- **AOT incompatible:** 1

