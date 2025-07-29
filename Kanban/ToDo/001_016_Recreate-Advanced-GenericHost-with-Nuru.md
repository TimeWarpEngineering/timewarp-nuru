# Recreate Advanced.GenericHost with Nuru

## Description

Port the Cocona Advanced.GenericHost sample to Nuru, demonstrating integration with .NET Generic Host for advanced hosting scenarios.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application using Generic Host
- Configure host services and logging
- Implement hosted services
- Create Overview.md comparing host integration

## Checklist

### Implementation
- [ ] Set up Generic Host builder
- [ ] Configure logging and services
- [ ] Integrate Nuru with host
- [ ] Test host lifecycle

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Host builder configuration
  - [ ] Service registration in host
  - [ ] Logging integration
  - [ ] Host lifecycle events

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/Advanced.GenericHost/`

Key features to compare:
- IHostBuilder usage
- ConfigureServices patterns
- Logging configuration
- Background service integration