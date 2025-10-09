# Calculator Samples

This directory contains three calculator implementations demonstrating different TimeWarp.Nuru approaches. These are .NET 10 single-file executables that can be run directly.

## Running the Examples

**Requires .NET 10 Preview 6 or later**

Simply run any calculator directly:

### calc-delegate.cs - Maximum Performance
```bash
./calc-delegate.cs add 3 5
# Output: 3 + 5 = 8

./calc-delegate.cs multiply 5 6
# Output: 5 × 6 = 30

./calc-delegate.cs round 3.7 --mode up
# Output: Round(3.7, up) = 4

./calc-delegate.cs --help
# Shows all available commands
```

### calc-mediator.cs - Enterprise Patterns
```bash
./calc-mediator.cs add 10 20
# Output: 10 + 20 = 30

./calc-mediator.cs divide 10 0
# Output: Error: Division by zero

./calc-mediator.cs round 3.5 --mode banker
# Output: Round(3.5, banker) = 4
```

### calc-mixed.cs - Best of Both Worlds
```bash
# Simple operations use Direct (fast)
./calc-mixed.cs add 100 200
# Output: 100 + 200 = 300

# Complex operations use Mediator (testable, DI)
./calc-mixed.cs factorial 5
# Output: 5! = 120

./calc-mixed.cs isprime 17
# Output: 17 is prime

./calc-mixed.cs fibonacci 10
# Output: Fibonacci(10) = 55
```

## Architecture Comparison

| Approach     | When to Use                      | Benefits                                                                            |
| ------------ | -------------------------------- | ----------------------------------------------------------------------------------- |
| **Direct**   | Simple CLIs, scripts, tools      | • Minimal memory (4KB)<br>• Fastest execution<br>• No dependencies                  |
| **Mediator** | Enterprise apps, complex domains | • Testable handlers<br>• Dependency injection<br>• Separation of concerns           |
| **Mixed**    | Apps with varying complexity     | • Performance where needed<br>• Structure where valuable<br>• Flexible architecture |

## Implementation Details

These samples use .NET 10's new single-file executable feature:
- Shebang (`#!/usr/bin/dotnet --`) makes them directly executable
- `#:project` directive references the TimeWarp.Nuru project
- No separate project files needed
- Can be edited and run immediately

For more examples, see the [TimeWarp.Nuru documentation](https://github.com/TimeWarpEngineering/timewarp-nuru).