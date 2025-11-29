# Blazor WASM Terminal REPL Sample

## Description

Create a web-based terminal component that communicates with a server running Nuru REPL. This enables browser-based CLI experiences with full telemetry integration.

## Parent

082_Aspire-Host-OpenTelemetry-Sample

## Requirements

- Blazor WASM terminal component for browser-based CLI
- ASP.NET Core WebServer hosting Nuru REPL backend
- Real-time communication via SignalR or WebSocket
- Integration with Aspire Host from task 082
- Telemetry flows through to Aspire Dashboard

## Checklist

- [ ] Create ASP.NET Core WebServer project
- [ ] Implement Nuru REPL hosting in server
- [ ] Create Blazor WASM Terminal component
- [ ] Implement SignalR/WebSocket communication layer
- [ ] Wire up to Aspire Host
- [ ] Verify telemetry from web REPL appears in Dashboard
- [ ] Create Overview.md explaining the architecture
- [ ] Document how to run the sample

## Notes

### Architecture
```
┌─────────────────────────────────────────────────────────────┐
│ Aspire AppHost                                              │
│  ├─ OpenTelemetry Collector                                 │
│  ├─ Aspire Dashboard                                        │
│  ├─ ASP.NET Core WebServer                                  │
│  │    └─ Nuru REPL Backend (SignalR hub)                    │
│  └─ Blazor WASM Client                                      │
│       └─ Terminal Component (connects via SignalR)          │
└─────────────────────────────────────────────────────────────┘
```

### Communication Flow
1. User types command in Blazor terminal component
2. Command sent to server via SignalR
3. Server executes command through Nuru REPL
4. Output streamed back to client
5. Telemetry captured and sent to OTLP collector
