# Task 032: Implement Stream-Based REPL Test Mode

## Status
- [ ] Not Started
- Created: 2024-11-20
- Priority: High
- Category: Testing Infrastructure

## Problem Statement

The current REPL implementation is tightly coupled to `System.Console`, making it extremely difficult to test properly. Current testing approaches require:
- Hacky cancellation token timeouts
- Arbitrary delays to wait for async operations
- Inability to verify output
- Non-deterministic test behavior
- Cannot test interactive features like tab completion, arrow keys, etc.

This makes the REPL essentially untestable in an automated fashion, forcing reliance on manual testing.

## Proposed Solution

Implement an I/O abstraction layer that allows the REPL to work with either the real console (production) or streams (testing), enabling deterministic, fast, and comprehensive testing.

## Technical Design

### 1. Create I/O Abstraction Interface

```csharp
namespace TimeWarp.Nuru.Repl.IO;

public interface IReplIO
{
    // Output methods
    void WriteLine(string? message = null);
    void Write(string message);
    
    // Input methods
    string? ReadLine();
    ConsoleKeyInfo ReadKey(bool intercept);
    
    // Console properties
    int WindowWidth { get; }
    void Clear();
    
    // Testing support
    bool IsTestMode { get; }
}
```

### 2. Console Implementation (Production)

```csharp
internal class ConsoleReplIO : IReplIO
{
    public void WriteLine(string? message = null) => Console.WriteLine(message);
    public void Write(string message) => Console.Write(message);
    public string? ReadLine() => Console.ReadLine();
    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);
    public int WindowWidth => Console.WindowWidth;
    public void Clear() => Console.Clear();
    public bool IsTestMode => false;
}
```

### 3. Stream-Based Test Implementation

```csharp
internal class StreamReplIO : IReplIO
{
    private readonly TextReader Input;
    private readonly TextWriter Output;
    private readonly Queue<ConsoleKeyInfo> KeyQueue;
    
    public StreamReplIO(TextReader input, TextWriter output)
    {
        Input = input;
        Output = output;
        KeyQueue = new Queue<ConsoleKeyInfo>();
        WindowWidth = 80; // Fixed for testing
    }
    
    public void WriteLine(string? message = null) => Output.WriteLine(message);
    public void Write(string message) => Output.Write(message);
    public string? ReadLine() => Input.ReadLine();
    
    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        if (KeyQueue.Count > 0)
            return KeyQueue.Dequeue();
        
        // Convert line input to key presses for testing
        var line = Input.ReadLine();
        if (line == null)
            return new ConsoleKeyInfo('\0', ConsoleKey.D, false, false, true); // Ctrl+D
        
        foreach (char c in line)
            KeyQueue.Enqueue(new ConsoleKeyInfo(c, ConsoleKey.A, false, false, false));
        
        KeyQueue.Enqueue(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        return KeyQueue.Dequeue();
    }
    
    public int WindowWidth { get; }
    public void Clear() { } // No-op for testing
    public bool IsTestMode => true;
    
    // Additional testing helpers
    public void QueueKey(ConsoleKeyInfo key) => KeyQueue.Enqueue(key);
    public void QueueKeys(params ConsoleKeyInfo[] keys) 
    {
        foreach (var key in keys) 
            KeyQueue.Enqueue(key);
    }
}
```

### 4. Update ReplOptions

```csharp
public class ReplOptions
{
    // ... existing properties ...
    
    /// <summary>
    /// I/O provider for REPL operations. Defaults to Console.
    /// Can be overridden for testing with stream-based implementation.
    /// </summary>
    internal IReplIO? IO { get; set; }
}
```

### 5. Update ReplSession

Replace all `Console.*` calls with `IO.*` calls:
- `Console.WriteLine()` → `IO.WriteLine()`
- `Console.Write()` → `IO.Write()`
- `Console.ReadLine()` → `IO.ReadLine()`
- `Console.ReadKey()` → `IO.ReadKey()`
- `Console.Clear()` → `IO.Clear()`
- `Console.WindowWidth` → `IO.WindowWidth`

### 6. Factory Method for Test Mode

```csharp
public static class ReplTestMode
{
    public static ReplOptions CreateTestOptions(string input)
    {
        var inputReader = new StringReader(input);
        var outputWriter = new StringWriter();
        
        return new ReplOptions
        {
            IO = new StreamReplIO(inputReader, outputWriter)
        };
    }
    
    public static (ReplOptions options, StringWriter output) CreateTestOptionsWithOutput(string input)
    {
        var inputReader = new StringReader(input);
        var outputWriter = new StringWriter();
        
        var options = new ReplOptions
        {
            IO = new StreamReplIO(inputReader, outputWriter)
        };
        
        return (options, outputWriter);
    }
}
```

## Implementation Steps

1. **Create abstraction layer** (2 hours)
   - [ ] Create `IReplIO` interface
   - [ ] Implement `ConsoleReplIO` for production
   - [ ] Implement `StreamReplIO` for testing
   - [ ] Add unit tests for `StreamReplIO`

2. **Refactor ReplSession** (3 hours)
   - [ ] Update all Console calls to use IReplIO
   - [ ] Add IO initialization logic
   - [ ] Ensure backward compatibility

3. **Refactor ReplConsoleReader** (2 hours)
   - [ ] Update all Console calls
   - [ ] Handle special keys in test mode
   - [ ] Test arrow key navigation

4. **Update ReplOptions** (1 hour)
   - [ ] Add IO property
   - [ ] Add factory methods for test creation
   - [ ] Update documentation

5. **Create test utilities** (2 hours)
   - [ ] Helper methods for common test scenarios
   - [ ] Key sequence builders
   - [ ] Output assertion helpers

6. **Write comprehensive tests** (4 hours)
   - [ ] Session lifecycle tests
   - [ ] Command execution tests
   - [ ] History management tests
   - [ ] Tab completion tests
   - [ ] Error handling tests

## Testing Benefits

With this implementation, tests become clean and deterministic:

```csharp
public static async Task Should_start_and_exit_session()
{
    // Arrange
    var input = new StringReader("exit\n");
    var output = new StringWriter();
    
    var options = new ReplOptions
    {
        IO = new StreamReplIO(input, output)
    };
    
    var app = new NuruAppBuilder()
        .AddReplSupport()
        .Build();
    
    // Act
    await app.RunReplAsync(options);
    
    // Assert
    var outputText = output.ToString();
    outputText.ShouldContain("Welcome to");
    outputText.ShouldContain("Goodbye!");
}

public static async Task Should_execute_command_with_output()
{
    // Arrange
    var input = new StringReader("status\nexit\n");
    var output = new StringWriter();
    
    var app = new NuruAppBuilder()
        .AddRoute("status", () => { 
            Console.WriteLine("System OK"); 
            return 0; 
        })
        .AddReplSupport()
        .Build();
    
    var options = new ReplOptions
    {
        IO = new StreamReplIO(input, output)
    };
    
    // Act
    await app.RunReplAsync(options);
    
    // Assert
    output.ToString().ShouldContain("System OK");
}
```

## Advantages

1. **Deterministic Testing**: No timing issues, no cancellation token hacks
2. **Full Control**: Can script exact input sequences including special keys
3. **Output Verification**: Can capture and assert on all output
4. **No Side Effects**: Tests don't interact with actual console
5. **Fast Tests**: No delays needed, runs at full speed
6. **Debuggable**: Can step through test execution easily
7. **Platform Independent**: Tests work the same on all platforms

## Acceptance Criteria

- [ ] All Console calls abstracted through IReplIO
- [ ] Tests can run without real console interaction
- [ ] No breaking changes to existing REPL functionality
- [ ] Test coverage for all REPL features
- [ ] Documentation updated with testing examples
- [ ] Performance impact < 1% in production mode

## Dependencies

- Must maintain backward compatibility
- Should work with existing ReplOptions
- Must support all current REPL features

## Notes

- Consider making IReplIO public in future for custom implementations
- Could enable scenarios like web-based REPL, remote REPL, etc.
- Stream implementation could be enhanced for more complex key sequences
- Consider adding record/replay functionality for debugging

## Related Tasks

- Task 027: REPL AddRoute Implementation
- Task 031: Implement REPL Tab Completion
- Tests: repl-01 through repl-15 test files

## Estimated Time

Total: ~14 hours
- Design and abstraction: 2 hours
- Implementation: 8 hours  
- Testing: 4 hours

## Priority Justification

High priority because:
1. Currently REPL is essentially untestable
2. Blocks comprehensive test suite completion
3. Will improve code quality and maintainability
4. Enables CI/CD testing of REPL features
5. Required for confidence in REPL changes