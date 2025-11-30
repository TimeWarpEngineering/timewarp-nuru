# TimeWarp.Nuru CLI Framework Benchmarks

This benchmark compares TimeWarp.Nuru with other popular .NET CLI frameworks.

## Frameworks Included

- **TimeWarp.Nuru** - Your framework
- **ConsoleAppFramework v5** - High-performance CLI framework (baseline)
- **Cocona & Cocona.Lite** - Easy-to-use CLI frameworks
- **CliFx** - Declarative CLI framework
- **System.CommandLine** - Microsoft's modern CLI framework
- **Spectre.Console.Cli** - Feature-rich CLI framework
- **McMaster.Extensions.CommandLineUtils** - Popular CLI framework
- **CommandLineParser** - Classic command line parser
- **PowerArgs** - Attribute-based CLI framework

## Running the Benchmarks

### Quick Run
```bash
cd Benchmarks/TimeWarp.Nuru.Benchmarks
dotnet run -c Release
```

### Detailed Run with HTML Report
```bash
cd Benchmarks/TimeWarp.Nuru.Benchmarks
dotnet run -c Release -- --exporters html
```

### Run Specific Benchmarks
```bash
cd Benchmarks/TimeWarp.Nuru.Benchmarks
dotnet run -c Release -- --filter "*Nuru*"
```

## Benchmark Scenario

All frameworks parse the same command line arguments:
```
--str "hello world" -i 13 -b
```

This tests:
- String option parsing (`--str` / `-s`)
- Integer option parsing (`--int` / `-i`)
- Boolean flag parsing (`--bool` / `-b`)

## Understanding Results

The benchmarks measure:
- **Execution Time** - How long it takes to parse arguments and execute
- **Memory Allocation** - How much memory is allocated during execution

ConsoleAppFramework v5 is set as the baseline for comparison.

### Metrics Explained

#### Mean (Execution Time)
The average time in milliseconds (ms) it takes for the framework to:
1. Initialize itself
2. Parse the command-line arguments
3. Execute a minimal command handler
4. Complete and return

**Interpretation:**
- **< 2 ms**: Exceptional performance (imperceptible to users)
- **< 10 ms**: Excellent performance
- **< 50 ms**: Good performance (still feels instant)
- **< 100 ms**: Acceptable for most CLI tools
- **> 100 ms**: Users may notice startup delay

#### Ratio
Relative performance compared to the baseline (ConsoleAppFramework v5):
- **1.00**: Same speed as baseline
- **10.00**: 10x slower than baseline
- **20.00**: 20x slower than baseline

#### Allocated (Memory)
Total heap memory allocated in bytes during the benchmark:
- **0 B**: No heap allocations (optimal)
- **< 1 KB**: Minimal allocations
- **< 10 KB**: Very good
- **< 100 KB**: Acceptable
- **> 100 KB**: May impact performance in high-frequency scenarios

#### Why These Metrics Matter

1. **Startup Performance**: CLI tools are often used in scripts and automation where they may be invoked thousands of times. Every millisecond counts.

2. **Memory Efficiency**: Lower allocations mean:
   - Less garbage collection pressure
   - Better performance in memory-constrained environments
   - More predictable performance characteristics

3. **User Experience**: 
   - Users expect CLI tools to feel instant
   - Anything over 100ms feels sluggish
   - Fast tools encourage more interactive use

## Latest Results

See the `Results/` folder for timestamped benchmark results and detailed analysis.