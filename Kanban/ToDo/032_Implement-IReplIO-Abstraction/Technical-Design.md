# Technical Design: IReplIO Abstraction

## Architecture Overview

The IReplIO abstraction creates a clean separation between REPL logic and I/O operations, enabling the REPL to work in any environment.

```
┌─────────────────────┐
│   NuruApp / User    │
└──────────┬──────────┘
           │
    ┌──────▼──────┐
    │ ReplSession │
    └──────┬──────┘
           │
    ┌──────▼──────────┐
    │ ReplOptions     │
    │  - IO: IReplIO  │
    └──────┬──────────┘
           │
    ┌──────▼──────┐
    │   IReplIO   │ (Interface)
    └──────┬──────┘
           │
    ┌──────┴───────────────┬─────────────┬──────────────┐
    │                       │             │              │
┌───▼────────┐  ┌──────────▼──────┐  ┌───▼──────┐  ┌───▼──────┐
│ConsoleReplIO│  │ StreamReplIO    │  │WebReplIO │  │ GuiReplIO│
│  (Default)  │  │   (Testing)     │  │ (User)   │  │  (User)  │
└─────────────┘  └─────────────────┘  └──────────┘  └──────────┘
```

## Interface Definition

```csharp
namespace TimeWarp.Nuru.Repl.IO;

/// <summary>
/// Abstraction for REPL input/output operations.
/// Implement this interface to create custom REPL environments.
/// </summary>
public interface IReplIO
{
    // ===== Core Output Operations =====
    
    /// <summary>
    /// Writes a line of text to the output.
    /// </summary>
    void WriteLine(string? message = null);
    
    /// <summary>
    /// Writes text without a newline to the output.
    /// </summary>
    void Write(string message);
    
    /// <summary>
    /// Clears the output display.
    /// </summary>
    void Clear();
    
    // ===== Core Input Operations =====
    
    /// <summary>
    /// Reads a line of text from input.
    /// Returns null on EOF.
    /// </summary>
    string? ReadLine();
    
    /// <summary>
    /// Reads a single key press.
    /// </summary>
    /// <param name="intercept">If true, key is not displayed.</param>
    ConsoleKeyInfo ReadKey(bool intercept);
    
    // ===== Terminal Properties =====
    
    /// <summary>
    /// Gets the width of the terminal window.
    /// Used for formatting output and completions.
    /// </summary>
    int WindowWidth { get; }
    
    /// <summary>
    /// Indicates if this is an interactive terminal.
    /// False for testing/scripted scenarios.
    /// </summary>
    bool IsInteractive { get; }
    
    // ===== Optional Enhanced Features =====
    
    /// <summary>
    /// Indicates if the output supports ANSI color codes.
    /// </summary>
    bool SupportsColor { get; }
    
    /// <summary>
    /// Sets the cursor position for advanced formatting.
    /// May throw NotSupportedException if not available.
    /// </summary>
    void SetCursorPosition(int left, int top);
    
    /// <summary>
    /// Gets the current cursor position.
    /// May throw NotSupportedException if not available.
    /// </summary>
    (int Left, int Top) GetCursorPosition();
}
```

## Core Implementations

### ConsoleReplIO (Production)

```csharp
namespace TimeWarp.Nuru.Repl.IO;

/// <summary>
/// Standard console implementation of IReplIO.
/// This is the default for normal CLI usage.
/// </summary>
public sealed class ConsoleReplIO : IReplIO
{
    public void WriteLine(string? message = null) 
        => Console.WriteLine(message ?? string.Empty);
    
    public void Write(string message) 
        => Console.Write(message);
    
    public void Clear() 
        => Console.Clear();
    
    public string? ReadLine() 
        => Console.ReadLine();
    
    public ConsoleKeyInfo ReadKey(bool intercept) 
        => Console.ReadKey(intercept);
    
    public int WindowWidth
    {
        get
        {
            try
            {
                return Console.WindowWidth;
            }
            catch
            {
                return 80; // Default fallback
            }
        }
    }
    
    public bool IsInteractive => !Console.IsInputRedirected;
    
    public bool SupportsColor => !Console.IsOutputRedirected;
    
    public void SetCursorPosition(int left, int top)
    {
        try
        {
            Console.SetCursorPosition(left, top);
        }
        catch
        {
            // Silently fail if not supported
        }
    }
    
    public (int Left, int Top) GetCursorPosition()
    {
        try
        {
            return (Console.CursorLeft, Console.CursorTop);
        }
        catch
        {
            return (0, 0); // Default if not supported
        }
    }
}
```

### StreamReplIO (Testing)

```csharp
namespace TimeWarp.Nuru.Repl.IO;

/// <summary>
/// Stream-based implementation for testing.
/// Enables deterministic, scriptable testing of REPL features.
/// </summary>
public sealed class StreamReplIO : IReplIO
{
    private readonly TextReader input;
    private readonly TextWriter output;
    private readonly Queue<ConsoleKeyInfo> keyQueue;
    private int cursorLeft;
    private int cursorTop;
    
    public StreamReplIO(TextReader input, TextWriter output)
    {
        this.input = input ?? throw new ArgumentNullException(nameof(input));
        this.output = output ?? throw new ArgumentNullException(nameof(output));
        this.keyQueue = new Queue<ConsoleKeyInfo>();
        this.WindowWidth = 80;
    }
    
    public void WriteLine(string? message = null)
    {
        output.WriteLine(message ?? string.Empty);
        output.Flush(); // Ensure immediate write for testing
    }
    
    public void Write(string message)
    {
        output.Write(message);
        output.Flush();
    }
    
    public void Clear()
    {
        // For testing, just add a marker
        output.WriteLine("[CLEAR]");
    }
    
    public string? ReadLine() => input.ReadLine();
    
    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        // First check queued keys
        if (keyQueue.Count > 0)
            return keyQueue.Dequeue();
        
        // Read a line and convert to keys
        var line = input.ReadLine();
        if (line == null)
        {
            // EOF - simulate Ctrl+D
            return new ConsoleKeyInfo('\u0004', ConsoleKey.D, false, false, true);
        }
        
        // Convert line to key sequence
        foreach (char c in line)
        {
            var key = CharToConsoleKey(c);
            keyQueue.Enqueue(new ConsoleKeyInfo(c, key, false, false, false));
        }
        
        // Add Enter at end of line
        keyQueue.Enqueue(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        
        return keyQueue.Count > 0 ? keyQueue.Dequeue() 
            : new ConsoleKeyInfo('\0', ConsoleKey.NoName, false, false, false);
    }
    
    public int WindowWidth { get; set; }
    
    public bool IsInteractive => false; // Always false for testing
    
    public bool SupportsColor { get; set; } = true; // Configurable for tests
    
    public void SetCursorPosition(int left, int top)
    {
        cursorLeft = left;
        cursorTop = top;
        output.WriteLine($"[CURSOR:{left},{top}]");
    }
    
    public (int Left, int Top) GetCursorPosition() => (cursorLeft, cursorTop);
    
    // === Testing Helper Methods ===
    
    /// <summary>
    /// Queue a specific key press for testing special keys.
    /// </summary>
    public void QueueKey(ConsoleKeyInfo key) => keyQueue.Enqueue(key);
    
    /// <summary>
    /// Queue multiple keys at once.
    /// </summary>
    public void QueueKeys(params ConsoleKeyInfo[] keys)
    {
        foreach (var key in keys)
            keyQueue.Enqueue(key);
    }
    
    /// <summary>
    /// Queue special keys like arrows, tab, etc.
    /// </summary>
    public void QueueSpecialKey(ConsoleKey key, bool shift = false, bool alt = false, bool ctrl = false)
    {
        char keyChar = key switch
        {
            ConsoleKey.Tab => '\t',
            ConsoleKey.Enter => '\r',
            ConsoleKey.Escape => '\u001b',
            ConsoleKey.Backspace => '\b',
            _ => '\0'
        };
        
        keyQueue.Enqueue(new ConsoleKeyInfo(keyChar, key, shift, alt, ctrl));
    }
    
    /// <summary>
    /// Simulate arrow key navigation.
    /// </summary>
    public void QueueArrowKeys(params ConsoleKey[] arrows)
    {
        foreach (var arrow in arrows)
        {
            if (arrow is ConsoleKey.UpArrow or ConsoleKey.DownArrow 
                or ConsoleKey.LeftArrow or ConsoleKey.RightArrow)
            {
                QueueSpecialKey(arrow);
            }
        }
    }
    
    private static ConsoleKey CharToConsoleKey(char c) => c switch
    {
        >= 'a' and <= 'z' => ConsoleKey.A + (c - 'a'),
        >= 'A' and <= 'Z' => ConsoleKey.A + (c - 'A'),
        >= '0' and <= '9' => ConsoleKey.D0 + (c - '0'),
        ' ' => ConsoleKey.Spacebar,
        '\t' => ConsoleKey.Tab,
        '\r' or '\n' => ConsoleKey.Enter,
        '\b' => ConsoleKey.Backspace,
        _ => ConsoleKey.NoName
    };
}
```

## Integration with ReplSession

### Current Code (Before)
```csharp
public class ReplSession
{
    private async Task<int> RunInstanceAsync(CancellationToken cancellationToken)
    {
        if (ReplOptions.EnableColors)
        {
            Console.WriteLine(AnsiColors.Green + ReplOptions.WelcomeMessage + AnsiColors.Reset);
        }
        else
        {
            Console.WriteLine(ReplOptions.WelcomeMessage);
        }
        
        // ... more Console usage
    }
}
```

### Updated Code (After)
```csharp
public class ReplSession
{
    private readonly IReplIO io;
    
    internal ReplSession(NuruApp app, ReplOptions options, ILoggerFactory loggerFactory)
    {
        // ... existing initialization ...
        
        // Initialize I/O (use provided or default to console)
        this.io = options.IO ?? new ConsoleReplIO();
    }
    
    private async Task<int> RunInstanceAsync(CancellationToken cancellationToken)
    {
        if (ReplOptions.EnableColors && io.SupportsColor)
        {
            io.WriteLine(AnsiColors.Green + ReplOptions.WelcomeMessage + AnsiColors.Reset);
        }
        else
        {
            io.WriteLine(ReplOptions.WelcomeMessage);
        }
        
        // ... all Console calls replaced with io calls
    }
}
```

## ReplOptions Enhancement

```csharp
public class ReplOptions
{
    // ... existing properties ...
    
    /// <summary>
    /// Gets or sets the I/O provider for REPL operations.
    /// If null, defaults to ConsoleReplIO for standard console interaction.
    /// Can be overridden for testing, web terminals, GUI integration, etc.
    /// </summary>
    /// <example>
    /// <code>
    /// // For testing
    /// var options = new ReplOptions
    /// {
    ///     IO = new StreamReplIO(inputReader, outputWriter)
    /// };
    /// 
    /// // For web terminal
    /// var options = new ReplOptions
    /// {
    ///     IO = new SignalRReplIO(hubContext, connectionId)
    /// };
    /// </code>
    /// </example>
    public IReplIO? IO { get; set; }
}
```

## Migration Strategy

### Phase 1: Add Abstraction (No Breaking Changes)
1. Add IReplIO interface as public API
2. Add ConsoleReplIO implementation
3. Add StreamReplIO for testing
4. Update ReplOptions with IO property (nullable, defaults to ConsoleReplIO)

### Phase 2: Refactor Internals
1. Update ReplSession to use IReplIO
2. Update ReplConsoleReader to use IReplIO
3. Replace all Console.* calls systematically
4. Ensure backward compatibility

### Phase 3: Enable Testing
1. Write comprehensive tests using StreamReplIO
2. Test all REPL features
3. Add test utilities and helpers

### Phase 4: Documentation & Samples
1. Create web terminal sample
2. Document extension points
3. Provide implementation guide

## Performance Considerations

### Benchmarks
```csharp
[Benchmark]
public void ConsoleWrite_Direct() => Console.WriteLine("test");

[Benchmark]
public void ConsoleWrite_ViaInterface() => consoleIO.WriteLine("test");

// Expected: < 1% overhead due to virtual dispatch
```

### Optimization Strategies
1. Cache IReplIO instance (done via field)
2. Minimize interface calls in hot paths
3. Use readonly fields where possible
4. Consider inlining for simple methods

## Testing Strategy

### Unit Tests
```csharp
[Test]
public async Task Should_capture_output_correctly()
{
    // Arrange
    var input = new StringReader("test command\nexit\n");
    var output = new StringWriter();
    var io = new StreamReplIO(input, output);
    var options = new ReplOptions { IO = io };
    
    // Act
    await app.RunReplAsync(options);
    
    // Assert
    var result = output.ToString();
    result.ShouldContain("test command");
    result.ShouldContain("Goodbye");
}
```

### Integration Tests
```csharp
[Test]
public async Task Should_handle_tab_completion()
{
    // Arrange
    var io = new StreamReplIO(new StringReader(""), new StringWriter());
    io.QueueKeys(
        new ConsoleKeyInfo('s', ConsoleKey.S, false, false, false),
        new ConsoleKeyInfo('t', ConsoleKey.T, false, false, false),
        new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false)
    );
    io.QueueSpecialKey(ConsoleKey.Enter);
    
    // ... test tab completion behavior
}
```

## Security Considerations

1. **Input Validation**: All IReplIO implementations must validate input
2. **Output Sanitization**: Web implementations must escape HTML/JavaScript
3. **Resource Limits**: Prevent memory exhaustion from large inputs
4. **Session Isolation**: Multi-user scenarios must isolate sessions

## Future Enhancements

1. **Async I/O**: Consider async versions of methods for better scalability
2. **Rich Output**: Support for tables, progress bars, etc.
3. **Multi-channel**: Separate channels for stdout/stderr
4. **Bidirectional Events**: Push notifications from server to client
5. **Terminal Capabilities**: Query for specific terminal features