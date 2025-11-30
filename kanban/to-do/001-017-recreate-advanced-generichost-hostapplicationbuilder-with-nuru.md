# Recreate Advanced.GenericHost.HostApplicationBuilder with Nuru

## Description

Port the Cocona Advanced.GenericHost.HostApplicationBuilder sample to Nuru, demonstrating modern host building with HostApplicationBuilder API.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application using HostApplicationBuilder
- Implement modern configuration patterns
- Use simplified host building API
- Create Overview.md comparing builder approaches
- Implementation location: `samples/cocona-comparison/Advanced/generic-host-builder`

## Checklist

### Implementation
- [ ] Use HostApplicationBuilder API
- [ ] Configure services and logging
- [ ] Set up configuration sources
- [ ] Test application lifecycle

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] HostApplicationBuilder vs IHostBuilder
  - [ ] Modern configuration patterns
  - [ ] Service registration syntax
  - [ ] Simplified API usage

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/CoconaSample.Advanced.GenericHost.HostApplicationBuilder/`

Key features to compare:
- HostApplicationBuilder usage
- Simplified configuration
- Modern hosting patterns
- Builder method chaining