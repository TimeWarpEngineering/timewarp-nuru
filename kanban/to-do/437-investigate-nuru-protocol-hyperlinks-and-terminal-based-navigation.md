# Investigate Nuru Protocol Hyperlinks and Terminal-Based Navigation

## Summary

Explore implementing clickable hyperlinks in terminal output that invoke Nuru routes, creating a HATEOAS-like navigation experience. When displaying tables (e.g., search results), cells should be clickable links that execute other Nuru commands.

## Goal

Enable terminal UI patterns like:
```
$ nuru getleads search electricians
╭──────────────────────┬───────────────────────────────┬───────────╮
│ Name                 │ Location                      │ Actions   │
├──────────────────────┼───────────────────────────────┼───────────┤
│ Grayson Electrical   │ 5619 Sunset Ridge, Austin     │ View │ Map │  ← clickable
╰──────────────────────┴───────────────────────────────┴───────────╯
```

Clicking "View" would invoke `nuru getlead-details {placeId}` either in same session (REPL) or new process (CLI mode).

## Background

- OSC 8 escape sequences are already supported via TimeWarp.Terminal (`SupportsHyperlinks`, `.Link()` method)
- Current implementation works for HTTP URLs but not for custom URI schemes like `nuru://`
- Challenge: Terminal hyperlinks invoke OS URI handlers, which launch new processes by default

## Phased Approach Considered

### Phase 1: CLI Mode with OS URI Handlers
- Register `nuru://` as OS-level URI scheme
- Links spawn new terminal process with the command
- **Problem**: Windows Terminal + WSL creates tab explosion (each click = new tab)

### Phase 2: Same-Session Links via Socket IPC
- REPL mode creates Unix socket/named pipe
- URI includes session identifier: `nuru://cmd/args?session=/tmp/nuru-{pid}.sock`
- Thin wrapper sends URI to socket instead of executing
- Routes to already-running REPL process

### Phase 3: Full TUI with Keyboard Navigation
- Integrate Tazor framework for component-based TUI
- Arrow keys, vim bindings, mouse support
- Full browser-like experience within terminal

## Key Constraints Identified

1. **Primary Environment**: Windows Terminal → WSL (Linux)
2. **Use Cases**: REPL mode (same session) AND CLI mode (new process acceptable)
3. **State Management**: Route + params only; CLI state via files/JSON/.db
4. **Long-term Vision**: CLI becomes client of local dotnet server (like OpenCode)

## Technical Challenges

### Windows Terminal + WSL Specific
- OS URI handlers run on Windows host, not inside WSL
- Each URI invocation spawns new WT tab (no "current pane" API)
- Workarounds: Quake mode (replaces content), tmux integration (fragile)

### OSC 8 Limitations
- Designed for HTTP URLs, not CLI workflows
- No standard way to send input to running process
- Terminal-specific proprietary extensions exist (iTerm2) but not universal

### Keyboard Navigation Alternative
Instead of mouse clicks, consider row numbers with keyboard selection:
```
│ 1) │ Grayson Electrical   │ 5619 Sunset Ridge, Austin     │
│ 2) │ Eskew Electric       │ 10421 Old Manchaca Rd...      │
```
Press `1` to execute corresponding action inline. Simpler, no OSC 8, works everywhere.

## Open Questions

### Architecture Decisions
1. **Keyboard vs Mouse**: Is keyboard selection (numbers + arrow keys) sufficient for 80% of use cases, or is mouse clicking essential?

2. **Phase Skipping**: Given Windows Terminal + WSL constraints, should we skip Phase 1 entirely and go straight to Phase 2 (socket IPC) or keyboard navigation?

3. **Tazor Timeline**: Is Tazor mature enough to begin integration experiments, or should it stabilize first?

4. **Server Architecture**: The long-term vision is CLI-as-client to dotnet server. Should we design for that now (e.g., structured output, API contracts)? Is this actually closer than Tazor integration?

5. **Scope**: Is this feature for internal tools or general Nuru framework users? If internal, tmux hacks acceptable. If general, needs robust solution.

### Implementation Details
6. **URI Format**: How should complex parameters be encoded?
   - Option A: `nuru://command/arg1/arg2?opt=val`
   - Option B: `nuru://command?args=urlencoded`

7. **Table API**: Should we add `AddHyperlinkedRow()` to TableBuilder, or enhance `AddRow()` to accept hyperlinked cell objects?

8. **Async Handling**: If user clicks a link while long-running command executes (e.g., `deploy --watch`), what happens? Queue it? Interrupt? Ignore?

9. **State/History**: REPL already maintains command history. For CLI mode, is terminal history sufficient? Should REPL support Back/Forward navigation like a browser?

10. **Windows Terminal Version**: OSC 8 support is evolving - do we need fallbacks for older WT versions?

## Reference Links

- TimeWarp.Terminal hyperlink docs: `documentation/user/features/terminal-abstractions.md`
- OSC 8 specification: https://gist.github.com/egmontkob/eb114294efbcd5adb1944c9f3cb5feda
- Windows Terminal issue on custom URI handlers: https://github.com/microsoft/terminal/issues/
- Related kanban: Task #112 (Add OSC 8 hyperlink support - completed)

## Checklist

- [ ] Document decision on keyboard vs mouse navigation approach
- [ ] Evaluate Tazor readiness for integration
- [ ] Design URI format and encoding scheme
- [ ] Create proof-of-concept for Windows Terminal + WSL
- [ ] Define API for hyperlinked table rows
- [ ] Determine if server-client architecture should be prioritized
- [ ] Decide on REPL vs CLI mode prioritization

## Notes

### User Context (2026-02-19)
- Primary use: Windows Terminal to WSL
- Wants REPL mode support but also same-session links
- Later maybe cross-session links
- Keyboard clicking desirable but requires more TUI-like experience
- CLI-as-server is long-term vision but wants nice migration path

### Options Ranked by Complexity
| Approach | Effort | UX Quality | Notes |
|----------|--------|------------|-------|
| Copy-paste IDs | None | Poor | Current state |
| OSC 8 + new process | Low | Okay | Tab explosion in WT |
| OSC 8 + tmux hack | Medium | Good | Linux only, fragile |
| Keyboard selection | Low | Good | Add row numbers, `1-9` keys |
| Tazor integration | High | Excellent | Full TUI, big project |
| Server + client | Very High | Excellent | Like OpenCode, major refactor |
