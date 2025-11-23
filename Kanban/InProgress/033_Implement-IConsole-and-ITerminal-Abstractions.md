# Implement IConsole and ITerminal Abstractions

## Description

Create foundational console/terminal abstractions in the core TimeWarp.Nuru library to enable testability and extensibility. This is a prerequisite for Task 032 (IReplIO Abstraction) and provides a simpler, more focused approach.

## Parent

032_Implement-IReplIO-Abstraction

## Problem Statement

The current `NuruConsole` in `Source/TimeWarp.Nuru/NuruConsole.cs` is:
- A static class with no interface
- Internal (not public)
- Output-only (WriteLine, WriteErrorLine)
- Uses delegate-based substitution pattern

This limits testability across the framework, not just REPL.

## Proposed Solution

Split into two interfaces with clean separation of concerns:

### IConsole (Basic I/O)
```csharp
public interface IConsole
{
    void WriteLine(string? message = null);
    void Write(string message);
    Task WriteLineAsync(string? message = null);
    void WriteErrorLine(string? message = null);
    Task WriteErrorLineAsync(string? message = null);
    string? ReadLine();
}
```

### ITerminal : IConsole (Interactive Terminal)
```csharp
public interface ITerminal : IConsole
{
    ConsoleKeyInfo ReadKey(bool intercept);
    void SetCursorPosition(int left, int top);
    (int Left, int Top) GetCursorPosition();
    int WindowWidth { get; }
    bool IsInteractive { get; }
    bool SupportsColor { get; }
    void Clear();
}
```

### Implementation Classes

| Class | Implements | Purpose |
|-------|------------|---------|
| NuruConsole | IConsole | Production basic I/O (refactored from current static class) |
| NuruTerminal | ITerminal | Production full terminal capabilities |
| TestConsole | IConsole | Testing with StringWriter/StringReader |
| TestTerminal | ITerminal | Testing REPL with key queue support |

## Files to Create

- `Source/TimeWarp.Nuru/IO/IConsole.cs` - Public interface
- `Source/TimeWarp.Nuru/IO/ITerminal.cs` - Public interface extending IConsole
- `Source/TimeWarp.Nuru/IO/NuruConsole.cs` - Production IConsole implementation
- `Source/TimeWarp.Nuru/IO/NuruTerminal.cs` - Production ITerminal implementation
- `Source/TimeWarp.Nuru/IO/TestConsole.cs` - Testing IConsole implementation
- `Source/TimeWarp.Nuru/IO/TestTerminal.cs` - Testing ITerminal implementation

## Files to Modify

- `Source/TimeWarp.Nuru/NuruConsole.cs` - Refactor to implement IConsole (move to IO folder)

## Requirements

- [ ] Create IConsole interface with basic I/O operations
- [ ] Create ITerminal interface extending IConsole with terminal capabilities
- [ ] Refactor existing NuruConsole to implement IConsole
- [ ] Create NuruTerminal implementing ITerminal
- [ ] Create TestConsole for basic output testing
- [ ] Create TestTerminal with key queue for REPL testing
- [ ] Maintain 100% backward compatibility
- [ ] All interfaces and classes are public
- [ ] Full XML documentation on public APIs

## Notes

- The ITerminal interface will be used by REPL components
- Simpler approach than full IReplIO - focused on core abstractions
- Enables testing of both core framework and REPL features
