# AOT CLI Framework Benchmark Results

**Date:** 2025-12-20
**Platform:** Ubuntu 24.04 (Linux 5.15)
**Runtime:** .NET 10.0.1
**Tool:** hyperfine 1.18.0 (100 runs, 5 warmup)

## Executive Summary

This benchmark measures **Native AOT cold-start performance** for .NET CLI frameworks. All frameworks were compiled with `PublishAot=true` and measured for real-world startup time.

**Key Findings:**
- **ConsoleAppFramework** is the fastest at 2.6ms (purpose-built for AOT)
- **System.CommandLine** performs excellently at 3.2ms (Microsoft's official library)
- **TimeWarp.Nuru.Direct** is competitive at 4.0ms (1.52x baseline)
- **TimeWarp.Nuru.Full** at 8.3ms shows the cost of DI/configuration
- Several frameworks (CliFx, McMaster, PowerArgs) compile but **crash at runtime** due to reflection

## Cold Start Performance

| Framework | Mean | Min | Max | Relative | Binary Size |
|:---|---:|---:|---:|---:|---:|
| ConsoleAppFramework | 2.6 ms | 2.1 ms | 4.1 ms | 1.00 (baseline) | 2.6 MB |
| System.CommandLine | 3.2 ms | 2.8 ms | 4.0 ms | 1.20x | 3.4 MB |
| **Nuru-Direct** | **4.0 ms** | **3.4 ms** | **5.5 ms** | **1.52x** | **5.7 MB** |
| CommandLineParser | 4.3 ms | 3.6 ms | 5.7 ms | 1.63x | 4.5 MB |
| CoconaLite | 4.4 ms | 3.7 ms | 5.4 ms | 1.65x | 4.7 MB |
| SpectreConsole | 6.6 ms | 5.6 ms | 9.1 ms | 2.51x | 9.9 MB |
| **Nuru-Full** | **8.3 ms** | **7.2 ms** | **10.8 ms** | **3.13x** | **13 MB** |
| Cocona | 21.0 ms | 18.8 ms | 26.3 ms | 7.98x | 6.5 MB |

## AOT Compatibility Matrix

| Framework | Compiles | Runs | Binary Size | Status |
|:---|:---:|:---:|---:|:---|
| ConsoleAppFramework | ✓ | ✓ | 2.6 MB | Full AOT support (source-generated) |
| System.CommandLine | ✓ | ✓ | 3.4 MB | Full AOT support |
| **TimeWarp.Nuru.Direct** | ✓ | ✓ | 5.7 MB | Full AOT support (source-generated invokers) |
| **TimeWarp.Nuru.Full** | ✓ | ✓ | 13 MB | Full AOT support (with Mediator) |
| CommandLineParser | ✓ | ✓ | 4.5 MB | Full AOT support |
| CoconaLite | ✓ | ✓ | 4.7 MB | Full AOT support |
| Cocona | ✓ | ✓ | 6.5 MB | Full AOT support |
| SpectreConsole | ✓ | ✓ | 9.9 MB | Full AOT support |
| CliFx | ✓ | ✗ | 4.2 MB | Runtime crash: `Assembly.Location` returns empty |
| McMaster | ✓ | ✗ | 5.3 MB | Runtime crash: `MakeGenericMethod` not AOT compatible |
| PowerArgs | ✓ | ✗ | 5.1 MB | Runtime crash: Reflection-based attribute parsing |

## Binary Size Ranking

| Rank | Framework | Size | Notes |
|:---:|:---|---:|:---|
| 1 | ConsoleAppFramework | 2.6 MB | Smallest - zero dependencies |
| 2 | System.CommandLine | 3.4 MB | Microsoft official |
| 3 | CliFx | 4.2 MB | ⚠️ Crashes at runtime |
| 4 | CommandLineParser | 4.5 MB | |
| 5 | CoconaLite | 4.7 MB | |
| 6 | PowerArgs | 5.1 MB | ⚠️ Crashes at runtime |
| 7 | McMaster | 5.3 MB | ⚠️ Crashes at runtime |
| 8 | **Nuru-Direct** | **5.7 MB** | |
| 9 | Cocona | 6.5 MB | |
| 10 | SpectreConsole | 9.9 MB | Includes rich console UI |
| 11 | **Nuru-Full** | **13 MB** | Includes DI + Mediator |

## Analysis

### TimeWarp.Nuru Performance

- **Nuru-Direct (4.0ms)** is very competitive, only 1.52x slower than the AOT-optimized ConsoleAppFramework
- The 5.7 MB binary size is reasonable for a full-featured CLI framework
- **Nuru-Full (8.3ms)** shows the cost of DI container and Mediator initialization
- The 13 MB binary size reflects the included DI infrastructure

### Framework Comparison

1. **ConsoleAppFramework** wins on performance because it was purpose-built for AOT with source generators that eliminate all runtime overhead.

2. **System.CommandLine** (Microsoft) performs excellently and should be considered the "standard" to compare against.

3. **TimeWarp.Nuru** offers a good balance between features and performance:
   - Direct builder: 1.52x slower than ConsoleAppFramework, but with more features
   - Full builder: 3.13x slower, but includes full DI/configuration support

4. **Cocona** is surprisingly slow in AOT (7.98x) despite being from the same author as ConsoleAppFramework. The hosting infrastructure adds significant overhead.

### Hidden AOT Issues

Three frameworks **compile successfully but crash at runtime**:

- **CliFx**: Uses `Assembly.Location` which returns empty string in AOT
- **McMaster**: Uses `MethodInfo.MakeGenericMethod()` for type parsing
- **PowerArgs**: Uses reflection for attribute-based argument parsing

This highlights that AOT compatibility requires runtime testing, not just successful compilation.

## Methodology

- All benchmarks run with `hyperfine -N --warmup 5 --runs 100`
- Each framework parses: `--str hello --int 13 --bool`
- Measured on Ubuntu 24.04 with .NET 10.0.1
- Native AOT with `PublishAot=true`, `TrimMode=partial`, `InvariantGlobalization=true`

## Recommendations

1. **For maximum AOT performance**: Use ConsoleAppFramework or System.CommandLine
2. **For TimeWarp.Nuru users**: 
   - Use `CreateEmptyBuilder` for CLI tools where startup time is critical
   - Use `CreateBuilder` when you need DI/configuration features
3. **Avoid**: CliFx, McMaster, PowerArgs if AOT is required
