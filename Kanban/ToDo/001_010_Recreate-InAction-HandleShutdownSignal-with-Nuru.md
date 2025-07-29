# Recreate InAction.HandleShutdownSignal with Nuru

## Description

Port the Cocona InAction.HandleShutdownSignal sample to Nuru, demonstrating graceful shutdown handling for SIGINT/SIGTERM signals.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with shutdown signal handling
- Implement graceful cleanup on Ctrl+C
- Handle cancellation tokens properly
- Create Overview.md comparing shutdown approaches

## Checklist

### Implementation
- [ ] Set up signal handlers for SIGINT/SIGTERM
- [ ] Implement long-running command with cancellation
- [ ] Add cleanup logic on shutdown
- [ ] Test graceful shutdown behavior

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Signal handler registration
  - [ ] CancellationToken usage
  - [ ] Cleanup patterns
  - [ ] Timeout handling

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/InAction.HandleShutdownSignal/`

Key features to compare:
- Console.CancelKeyPress handling
- IHostApplicationLifetime usage
- CancellationToken propagation
- Graceful shutdown timeouts