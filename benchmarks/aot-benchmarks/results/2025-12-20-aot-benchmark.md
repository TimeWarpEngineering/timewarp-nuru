# AOT CLI Framework Benchmark Results

**Date:** 2025-12-20 (Updated after optimization pass)
**Platform:** Ubuntu 24.04 (Linux 5.15)
**Runtime:** .NET 10.0.1
**Tool:** hyperfine 1.18.0 (1000 runs, 20 warmup)

## Executive Summary

This benchmark measures **Native AOT cold-start performance** for .NET CLI frameworks. All frameworks were compiled with `PublishAot=true` and measured for real-world startup time.

**Key Findings:**
- **ConsoleAppFramework** is the fastest at ~2.5ms (purpose-built for AOT)
- **System.CommandLine** performs excellently at ~3.3ms (1.31x baseline)
- **TimeWarp.Nuru.Direct** beats CommandLineParser at **1.65x** (vs 1.66x)
- **TimeWarp.Nuru.Full** at 3.23x competes with SpectreConsole while offering far more features
- Several frameworks (CliFx, McMaster, PowerArgs) compile but **crash at runtime** due to reflection

## Cold Start Performance (1000 runs)

| Framework | Relative | Notes |
|:---|---:|:---|
| ConsoleAppFramework | 1.00x (baseline) | AOT-first, source-generated |
| System.CommandLine | 1.31x | Microsoft official |
| **Nuru-Direct** | **1.65x** | **Beats CommandLineParser!** |
| CommandLineParser | 1.66x | |
| CoconaLite | 1.86x | |
| SpectreConsole | 2.62x | Rich console UI |
| **Nuru-Full** | **3.23x** | **DI + Mediator + REPL + Completion** |
| Cocona | 8.42x | Hosting infrastructure overhead |

## Optimization Journey

Today's optimization pass improved performance significantly:

| Builder | Before Opts | After Opts | Improvement |
|:---|---:|---:|:---|
| **Nuru-Direct** | 1.72x | **1.65x** | Now beats CommandLineParser |
| **Nuru-Full** | 3.58x | **3.23x** | -10% improvement |

### Optimizations Applied

1. **Cache OptionMatchers/PositionalMatchers on CompiledRoute** - Lazy cache LINQ results
2. **Cache RepeatedOptions on CompiledRoute** - Avoid per-call filtering
3. **Replace HashSet<int> with Span<bool> stackalloc** - Zero heap allocation for consumed indices
4. **Remove dead code (CheckRequiredOptions)** - 130 lines of unused code
5. **Lazy-create collectedValues list** - Only allocate when repeated options match
6. **Pre-size matches list (capacity: 2)** - Avoid List resizing
7. **Pre-size catchAllArgs list** - Based on remaining args count
8. **Pre-size extractedValues Dictionary (capacity: 8)** - Avoid Dictionary resizing

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

## Feature Comparison

TimeWarp.Nuru offers significantly more features than faster frameworks:

| Feature | ConsoleAppFramework | System.CommandLine | Nuru |
|:---|:---:|:---:|:---:|
| Route pattern matching | ✗ | Limited | ✓ |
| Typed parameters | ✓ | ✓ | ✓ |
| Optional parameters | ✓ | ✓ | ✓ |
| Catch-all parameters | ✗ | ✓ | ✓ |
| Repeated options | ✗ | ✓ | ✓ |
| REPL mode | ✗ | ✗ | ✓ |
| Tab completion | ✗ | ✓ | ✓ |
| Shell completion | ✗ | ✓ | ✓ |
| Dependency Injection | ✗ | ✗ | ✓ (Full) |
| Mediator pattern | ✗ | ✗ | ✓ (Full) |
| OpenTelemetry | ✗ | ✗ | ✓ (Full) |
| Help generation | ✓ | ✓ | ✓ |

## Analysis

### TimeWarp.Nuru Performance

- **Nuru-Direct (1.65x)** now beats CommandLineParser (1.66x) - excellent for a feature-rich framework
- **Nuru-Full (3.23x)** includes DI, Mediator, REPL, Completion, Telemetry - competitive with SpectreConsole (2.62x) while offering far more features

### Framework Comparison

1. **ConsoleAppFramework** wins on performance because it was purpose-built for AOT with source generators that eliminate all runtime overhead.

2. **System.CommandLine** (Microsoft) performs excellently and should be considered the "standard" to compare against.

3. **TimeWarp.Nuru** offers the best balance between features and performance:
   - Direct builder: Faster than CommandLineParser with more features
   - Full builder: Only ~23% slower than SpectreConsole but with DI, Mediator, REPL, Completion

4. **Cocona** is surprisingly slow in AOT (8.42x) despite being from the same author as ConsoleAppFramework.

### Hidden AOT Issues

Three frameworks **compile successfully but crash at runtime**:

- **CliFx**: Uses `Assembly.Location` which returns empty string in AOT
- **McMaster**: Uses `MethodInfo.MakeGenericMethod()` for type parsing
- **PowerArgs**: Uses reflection for attribute-based argument parsing

This highlights that AOT compatibility requires runtime testing, not just successful compilation.

## Methodology

- All benchmarks run with `hyperfine -N --warmup 20 --runs 1000`
- Each framework parses: `--str hello -i 13 -b` (or framework-specific equivalent)
- Measured on Ubuntu 24.04 with .NET 10.0.1
- Native AOT with `PublishAot=true`, `TrimMode=partial`, `InvariantGlobalization=true`

## Recommendations

1. **For maximum AOT performance**: Use ConsoleAppFramework or System.CommandLine
2. **For TimeWarp.Nuru users**: 
   - Use `CreateEmptyBuilder` for CLI tools where startup time is critical
   - Use `CreateBuilder` when you need DI/Mediator/REPL features
3. **Avoid**: CliFx, McMaster, PowerArgs if AOT is required
