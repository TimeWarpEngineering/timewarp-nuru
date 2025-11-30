# Implement IDisposable for ReplSession

## Description

Add IDisposable pattern to ReplSession to ensure proper cleanup of resources (event handlers, history persistence) even on abnormal termination paths.

## Parent

Code review finding from `.agent/workspace/replsession-code-review-2025-11-25-v2.md` - Issue #8

## Requirements

- Implement IDisposable interface on ReplSession
- Ensure event handler cleanup (Console.CancelKeyPress)
- Guarantee history persistence when enabled
- Avoid duplicate cleanup (idempotent Dispose)
- Update RunAsync to use Dispose in finally block
- Maintain existing goodbye message behavior

## Checklist

### Implementation
- [x] Add IDisposable interface to ReplSession class
- [x] Implement Dispose method with duplicate-call protection
- [x] Move critical cleanup to Dispose (event handler, history save)
- [x] Update CleanupRepl to call Dispose
- [x] Update RunAsync to dispose in finally block
- [x] Verify Functionality

### Testing
- [ ] Verify cleanup happens on normal exit
- [ ] Verify cleanup happens on Ctrl+C
- [ ] Verify cleanup happens on exception
- [ ] Test multiple Dispose calls (should be safe)
- [ ] Verify history saves on abnormal exit

### Documentation
- [x] Update XML comments for ReplSession class
- [x] Document disposal pattern in comments

## Notes

ReplSession manages resources (event handlers, file I/O) but doesn't implement IDisposable. While cleanup happens via `CleanupRepl()` in the normal path, abnormal termination could skip cleanup.

**File to modify:**
- `Source/TimeWarp.Nuru.Repl/Repl/ReplSession.cs`

**Implementation pattern:**

```csharp
internal sealed class ReplSession : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        
        // Critical cleanup that must happen
        Console.CancelKeyPress -= OnCancelKeyPress;
        
        if (ReplOptions.PersistHistory)
            History.Save();
            
        _disposed = true;
    }

    private void CleanupRepl()
    {
        // Dispose handles critical cleanup
        Dispose();
        
        // Display goodbye message (non-critical, cosmetic)
        if (!string.IsNullOrEmpty(ReplOptions.GoodbyeMessage))
            Terminal.WriteLine(ReplOptions.GoodbyeMessage);
    }

    public static async Task<int> RunAsync(
        NuruApp nuruApp,
        ReplOptions replOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        CurrentSession = new ReplSession(nuruApp, replOptions, loggerFactory);

        try
        {
            return await CurrentSession.RunInstanceAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Guaranteed cleanup even on exceptions
            CurrentSession?.Dispose();
            CurrentSession = null;
        }
    }
}
```

**Rationale:**
- Event handlers can cause memory leaks if not unregistered
- History save should happen even on exceptions
- IDisposable provides standard .NET resource management pattern
- Finally block guarantees cleanup in all exit paths

**Estimated effort:** ~20 minutes
