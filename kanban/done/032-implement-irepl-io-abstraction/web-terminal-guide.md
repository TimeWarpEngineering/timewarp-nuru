# Web Terminal Implementation Guide

## Overview

This guide demonstrates how to create a web-based terminal for any Nuru CLI application using Blazor and SignalR. Users can access your CLI through a web browser, enabling cloud deployment, remote access, and cross-platform availability without local installation.

## Architecture

```
┌─────────────────┐         WebSocket          ┌─────────────────┐
│  Browser Client │◄────────────────────────────►│  ASP.NET Server │
│                 │                              │                 │
│  Blazor Terminal│         SignalR             │  NuruApp REPL   │
│   Component     │◄────────────────────────────►│  with           │
│                 │                              │  SignalRReplIO  │
└─────────────────┘                              └─────────────────┘
```

## Server Implementation

### 1. SignalRReplIO Class

```csharp
using TimeWarp.Nuru.Repl.IO;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace YourApp.Web.Terminal;

public class SignalRReplIO : IReplIO
{
    private readonly IHubContext<TerminalHub> hubContext;
    private readonly string connectionId;
    private readonly BlockingCollection<string> inputQueue;
    private readonly BlockingCollection<ConsoleKeyInfo> keyQueue;
    private readonly CancellationToken cancellationToken;
    
    public SignalRReplIO(
        IHubContext<TerminalHub> hubContext, 
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        this.hubContext = hubContext;
        this.connectionId = connectionId;
        this.cancellationToken = cancellationToken;
        this.inputQueue = new BlockingCollection<string>();
        this.keyQueue = new BlockingCollection<ConsoleKeyInfo>();
        this.WindowWidth = 120; // Default, can be updated from client
    }
    
    // ===== Output Methods =====
    
    public void WriteLine(string? message = null)
    {
        var line = message ?? string.Empty;
        _ = hubContext.Clients.Client(connectionId)
            .SendAsync("ReceiveLine", line, cancellationToken);
    }
    
    public void Write(string message)
    {
        _ = hubContext.Clients.Client(connectionId)
            .SendAsync("ReceiveOutput", message, cancellationToken);
    }
    
    public void Clear()
    {
        _ = hubContext.Clients.Client(connectionId)
            .SendAsync("ClearTerminal", cancellationToken);
    }
    
    // ===== Input Methods =====
    
    public string? ReadLine()
    {
        try
        {
            return inputQueue.Take(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null; // EOF on cancellation
        }
    }
    
    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        try
        {
            // First check if we have queued keys
            if (keyQueue.TryTake(out var key, 0))
                return key;
            
            // Otherwise wait for a line and convert to keys
            var line = inputQueue.Take(cancellationToken);
            
            // Convert line to key sequence
            foreach (char c in line)
            {
                keyQueue.Add(new ConsoleKeyInfo(c, CharToKey(c), false, false, false));
            }
            
            // Add Enter key
            keyQueue.Add(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
            
            return keyQueue.Take();
        }
        catch (OperationCanceledException)
        {
            // Return Ctrl+C on cancellation
            return new ConsoleKeyInfo('\u0003', ConsoleKey.C, false, false, true);
        }
    }
    
    // ===== Properties =====
    
    public int WindowWidth { get; set; }
    public bool IsInteractive => true;
    public bool SupportsColor => true;
    
    public void SetCursorPosition(int left, int top)
    {
        _ = hubContext.Clients.Client(connectionId)
            .SendAsync("SetCursor", left, top, cancellationToken);
    }
    
    public (int Left, int Top) GetCursorPosition() => (0, 0); // Not tracked server-side
    
    // ===== Helper Methods =====
    
    public void QueueInput(string input)
    {
        inputQueue.Add(input);
    }
    
    public void QueueKey(ConsoleKeyInfo key)
    {
        keyQueue.Add(key);
    }
    
    public void UpdateWindowWidth(int width)
    {
        WindowWidth = width;
    }
    
    private static ConsoleKey CharToKey(char c) => c switch
    {
        >= 'a' and <= 'z' => ConsoleKey.A + (c - 'a'),
        >= 'A' and <= 'Z' => ConsoleKey.A + (c - 'A'),
        >= '0' and <= '9' => ConsoleKey.D0 + (c - '0'),
        ' ' => ConsoleKey.Spacebar,
        '\t' => ConsoleKey.Tab,
        _ => ConsoleKey.NoName
    };
    
    public void Dispose()
    {
        inputQueue?.Dispose();
        keyQueue?.Dispose();
    }
}
```

### 2. SignalR Hub

```csharp
using Microsoft.AspNetCore.SignalR;
using TimeWarp.Nuru;

namespace YourApp.Web.Terminal;

public class TerminalHub : Hub
{
    private readonly NuruApp app;
    private readonly ILogger<TerminalHub> logger;
    private static readonly ConcurrentDictionary<string, TerminalSession> sessions = new();
    
    public TerminalHub(NuruApp app, ILogger<TerminalHub> logger)
    {
        this.app = app;
        this.logger = logger;
    }
    
    public async Task StartSession()
    {
        var connectionId = Context.ConnectionId;
        
        // Clean up any existing session
        if (sessions.TryRemove(connectionId, out var oldSession))
        {
            oldSession.Dispose();
        }
        
        // Create new session
        var cts = new CancellationTokenSource();
        var io = new SignalRReplIO(Context.GetHttpContext()!.RequestServices
            .GetRequiredService<IHubContext<TerminalHub>>(), connectionId, cts.Token);
        
        var session = new TerminalSession(io, cts);
        sessions[connectionId] = session;
        
        // Start REPL in background
        session.Task = Task.Run(async () =>
        {
            try
            {
                var options = new ReplOptions
                {
                    IO = io,
                    Prompt = "web> ",
                    WelcomeMessage = "Welcome to Web Terminal!",
                    EnableColors = true,
                    ContinueOnError = true
                };
                
                await app.RunReplAsync(options, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal termination
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "REPL session error for {ConnectionId}", connectionId);
                await Clients.Client(connectionId).SendAsync("Error", ex.Message);
            }
            finally
            {
                sessions.TryRemove(connectionId, out _);
            }
        });
        
        await Clients.Caller.SendAsync("SessionStarted");
    }
    
    public async Task SendInput(string input)
    {
        if (sessions.TryGetValue(Context.ConnectionId, out var session))
        {
            session.IO.QueueInput(input);
        }
        else
        {
            await Clients.Caller.SendAsync("Error", "No active session");
        }
    }
    
    public async Task SendKey(string key, bool shift, bool ctrl, bool alt)
    {
        if (sessions.TryGetValue(Context.ConnectionId, out var session))
        {
            var consoleKey = ParseKey(key);
            var keyInfo = new ConsoleKeyInfo(GetKeyChar(consoleKey), consoleKey, shift, alt, ctrl);
            session.IO.QueueKey(keyInfo);
        }
    }
    
    public async Task UpdateTerminalWidth(int width)
    {
        if (sessions.TryGetValue(Context.ConnectionId, out var session))
        {
            session.IO.UpdateWindowWidth(width);
        }
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (sessions.TryRemove(Context.ConnectionId, out var session))
        {
            session.Dispose();
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    private static ConsoleKey ParseKey(string key) => key switch
    {
        "ArrowUp" => ConsoleKey.UpArrow,
        "ArrowDown" => ConsoleKey.DownArrow,
        "ArrowLeft" => ConsoleKey.LeftArrow,
        "ArrowRight" => ConsoleKey.RightArrow,
        "Tab" => ConsoleKey.Tab,
        "Enter" => ConsoleKey.Enter,
        "Escape" => ConsoleKey.Escape,
        "Backspace" => ConsoleKey.Backspace,
        "Delete" => ConsoleKey.Delete,
        "Home" => ConsoleKey.Home,
        "End" => ConsoleKey.End,
        _ => ConsoleKey.NoName
    };
    
    private static char GetKeyChar(ConsoleKey key) => key switch
    {
        ConsoleKey.Tab => '\t',
        ConsoleKey.Enter => '\r',
        ConsoleKey.Escape => '\x1b',
        ConsoleKey.Backspace => '\b',
        _ => '\0'
    };
    
    private class TerminalSession : IDisposable
    {
        public SignalRReplIO IO { get; }
        public CancellationTokenSource CancellationTokenSource { get; }
        public Task? Task { get; set; }
        
        public TerminalSession(SignalRReplIO io, CancellationTokenSource cts)
        {
            IO = io;
            CancellationTokenSource = cts;
        }
        
        public void Dispose()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
            IO.Dispose();
            Task?.Wait(TimeSpan.FromSeconds(5));
        }
    }
}
```

### 3. ASP.NET Core Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Register your NuruApp
builder.Services.AddSingleton<NuruApp>(sp =>
{
    return new NuruAppBuilder()
        .AddRoute("status", () => 
        {
            Console.WriteLine("System is operational");
            return 0;
        })
        .AddRoute("echo {message}", (string message) =>
        {
            Console.WriteLine(message);
            return 0;
        })
        // Add your other routes
        .Build();
});

var app = builder.Build();

// Configure pipeline
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapHub<TerminalHub>("/terminal");

app.Run();
```

## Client Implementation (Blazor)

### 1. Terminal Component

```razor
@page "/terminal"
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable

<PageTitle>Web Terminal</PageTitle>

<div class="terminal-container" @onclick="FocusInput">
    <div class="terminal-output" @ref="outputElement">
        @foreach (var line in outputLines)
        {
            <div class="terminal-line">
                <span>@((MarkupString)ConvertAnsiToHtml(line))</span>
            </div>
        }
        <div class="terminal-input-line">
            <span class="prompt">@currentPrompt</span>
            <span class="input-text">@currentInput</span>
            <span class="cursor">@(showCursor ? "█" : " ")</span>
        </div>
    </div>
    <input @ref="hiddenInput"
           type="text"
           class="hidden-input"
           @onkeydown="@HandleKeyDown"
           @onkeydown:preventDefault="true"
           @oninput="@HandleInput"
           value="@currentInput" />
</div>

@code {
    private HubConnection? hubConnection;
    private ElementReference outputElement;
    private ElementReference hiddenInput;
    
    private List<string> outputLines = new();
    private List<string> commandHistory = new();
    private int historyIndex = -1;
    
    private string currentInput = "";
    private string currentPrompt = "web> ";
    private bool showCursor = true;
    private System.Timers.Timer? cursorTimer;
    
    protected override async Task OnInitializedAsync()
    {
        // Setup cursor blink
        cursorTimer = new System.Timers.Timer(500);
        cursorTimer.Elapsed += (s, e) =>
        {
            showCursor = !showCursor;
            InvokeAsync(StateHasChanged);
        };
        cursorTimer.Start();
        
        // Setup SignalR connection
        hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/terminal"))
            .WithAutomaticReconnect()
            .Build();
        
        // Register handlers
        hubConnection.On<string>("ReceiveLine", (line) =>
        {
            outputLines.Add(line);
            InvokeAsync(StateHasChanged);
            InvokeAsync(ScrollToBottom);
        });
        
        hubConnection.On<string>("ReceiveOutput", (output) =>
        {
            if (outputLines.Count == 0)
                outputLines.Add("");
            
            outputLines[^1] += output;
            InvokeAsync(StateHasChanged);
        });
        
        hubConnection.On("ClearTerminal", () =>
        {
            outputLines.Clear();
            InvokeAsync(StateHasChanged);
        });
        
        hubConnection.On("SessionStarted", () =>
        {
            InvokeAsync(() => FocusInput());
        });
        
        hubConnection.On<string>("Error", (error) =>
        {
            outputLines.Add($"<span class='error'>Error: {error}</span>");
            InvokeAsync(StateHasChanged);
        });
        
        // Start connection and session
        await hubConnection.StartAsync();
        await hubConnection.SendAsync("StartSession");
        
        // Report terminal width
        await hubConnection.SendAsync("UpdateTerminalWidth", 120);
    }
    
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Enter":
                await SubmitInput();
                break;
                
            case "Tab":
                await hubConnection!.SendAsync("SendKey", "Tab", e.ShiftKey, e.CtrlKey, e.AltKey);
                break;
                
            case "ArrowUp":
                NavigateHistory(-1);
                break;
                
            case "ArrowDown":
                NavigateHistory(1);
                break;
                
            case "ArrowLeft":
            case "ArrowRight":
                await hubConnection!.SendAsync("SendKey", e.Key, e.ShiftKey, e.CtrlKey, e.AltKey);
                break;
                
            case "Escape":
                currentInput = "";
                break;
                
            case "c" when e.CtrlKey:
                outputLines.Add($"{currentPrompt}{currentInput}^C");
                currentInput = "";
                await hubConnection!.SendAsync("SendKey", "C", false, true, false);
                break;
        }
    }
    
    private async Task HandleInput(ChangeEventArgs e)
    {
        currentInput = e.Value?.ToString() ?? "";
    }
    
    private async Task SubmitInput()
    {
        if (string.IsNullOrWhiteSpace(currentInput))
            return;
        
        // Show input in output
        outputLines.Add($"{currentPrompt}{currentInput}");
        
        // Add to history
        commandHistory.Add(currentInput);
        historyIndex = commandHistory.Count;
        
        // Send to server
        await hubConnection!.SendAsync("SendInput", currentInput);
        
        // Clear input
        currentInput = "";
        
        await ScrollToBottom();
    }
    
    private void NavigateHistory(int direction)
    {
        if (commandHistory.Count == 0)
            return;
        
        historyIndex = Math.Clamp(historyIndex + direction, 0, commandHistory.Count);
        
        if (historyIndex < commandHistory.Count)
            currentInput = commandHistory[historyIndex];
        else
            currentInput = "";
    }
    
    private async Task FocusInput()
    {
        await hiddenInput.FocusAsync();
    }
    
    private async Task ScrollToBottom()
    {
        await outputElement.FocusAsync();
        await JSRuntime.InvokeVoidAsync("scrollToBottom", outputElement);
    }
    
    private string ConvertAnsiToHtml(string text)
    {
        // Basic ANSI to HTML conversion
        return text
            .Replace("\x1b[30m", "<span style='color:#000'>")
            .Replace("\x1b[31m", "<span style='color:#f00'>")
            .Replace("\x1b[32m", "<span style='color:#0f0'>")
            .Replace("\x1b[33m", "<span style='color:#ff0'>")
            .Replace("\x1b[34m", "<span style='color:#00f'>")
            .Replace("\x1b[35m", "<span style='color:#f0f'>")
            .Replace("\x1b[36m", "<span style='color:#0ff'>")
            .Replace("\x1b[37m", "<span style='color:#fff'>")
            .Replace("\x1b[90m", "<span style='color:#888'>")
            .Replace("\x1b[0m", "</span>")
            .Replace("\n", "<br>");
    }
    
    public async ValueTask DisposeAsync()
    {
        cursorTimer?.Dispose();
        
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
```

### 2. Terminal Styles

```css
/* wwwroot/css/terminal.css */

.terminal-container {
    background: #1e1e1e;
    color: #d4d4d4;
    font-family: 'Cascadia Code', 'Consolas', 'Courier New', monospace;
    font-size: 14px;
    padding: 10px;
    height: 80vh;
    overflow-y: auto;
    border-radius: 5px;
    cursor: text;
    position: relative;
}

.terminal-output {
    white-space: pre-wrap;
    word-break: break-all;
}

.terminal-line {
    line-height: 1.4;
    min-height: 1.4em;
}

.terminal-input-line {
    display: inline-flex;
    align-items: center;
}

.prompt {
    color: #569cd6;
    margin-right: 5px;
}

.input-text {
    color: #9cdcfe;
}

.cursor {
    animation: none;
    background-color: #d4d4d4;
    color: #1e1e1e;
    display: inline-block;
    width: 0.6em;
}

.hidden-input {
    position: absolute;
    left: -9999px;
    opacity: 0;
}

.error {
    color: #f48771;
}

/* ANSI Color Support */
.ansi-black { color: #000000; }
.ansi-red { color: #cd3131; }
.ansi-green { color: #0dbc79; }
.ansi-yellow { color: #e5e510; }
.ansi-blue { color: #2472c8; }
.ansi-magenta { color: #bc3fbc; }
.ansi-cyan { color: #11a8cd; }
.ansi-white { color: #e5e5e5; }

/* Scrollbar styling */
.terminal-container::-webkit-scrollbar {
    width: 10px;
}

.terminal-container::-webkit-scrollbar-track {
    background: #2d2d30;
}

.terminal-container::-webkit-scrollbar-thumb {
    background: #464647;
    border-radius: 5px;
}

.terminal-container::-webkit-scrollbar-thumb:hover {
    background: #565657;
}
```

### 3. JavaScript Helper

```javascript
// wwwroot/js/terminal.js

window.scrollToBottom = (element) => {
    element.scrollTop = element.scrollHeight;
};
```

## Deployment Considerations

### Security
1. **Authentication**: Add authentication before `StartSession`
2. **Authorization**: Check user permissions for commands
3. **Input Validation**: Sanitize all input
4. **Rate Limiting**: Prevent abuse
5. **Session Timeout**: Auto-disconnect idle sessions

### Scalability
1. **Redis Backplane**: For multi-server deployments
2. **Session Limits**: Max sessions per user
3. **Resource Monitoring**: Track memory/CPU usage
4. **Load Balancing**: Sticky sessions for SignalR

### Features
1. **Tab Completion**: Implement client-side UI for suggestions
2. **Syntax Highlighting**: Color commands as typed
3. **File Upload/Download**: For file-based commands
4. **Multiple Tabs**: Support multiple terminal sessions
5. **Themes**: Light/dark mode support

## Example Use Cases

### 1. Admin Dashboard
Embed terminal in admin panel for direct server management.

### 2. Cloud CLI Tool
Deploy CLI tools to cloud without local installation.

### 3. Educational Platform
Provide interactive CLI tutorials in browser.

### 4. Multi-Tenant SaaS
Isolated CLI sessions per tenant.

### 5. Mobile Access
Responsive terminal for phone/tablet access.

## Testing the Web Terminal

```csharp
[Test]
public async Task WebTerminal_Should_Execute_Commands()
{
    // Arrange
    var hubContext = new MockHubContext();
    var io = new SignalRReplIO(hubContext, "test-connection");
    var app = CreateTestApp();
    
    // Act
    io.QueueInput("status");
    io.QueueInput("exit");
    
    await app.RunReplAsync(new ReplOptions { IO = io });
    
    // Assert
    hubContext.SentMessages.ShouldContain(m => m.Contains("operational"));
}
```

## Summary

The web terminal implementation demonstrates the power of the IReplIO abstraction, enabling:
- Browser-based access to any Nuru CLI
- Real-time bidirectional communication
- Full REPL functionality in web environment
- Cloud-native deployment options
- Cross-platform accessibility

This transforms traditional CLI tools into modern web applications while maintaining all CLI functionality.