# Recreate InAction.DependencyInjection with Nuru

## Description

Port the Cocona InAction.DependencyInjection sample to Nuru, demonstrating dependency injection integration with command handlers.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with DI container
- Register and inject services into commands
- Demonstrate scoped and singleton lifetimes
- Create Overview.md comparing DI approaches

## Checklist

### Implementation
- [ ] Set up DI container configuration
- [ ] Create sample services and interfaces
- [ ] Inject services into command methods
- [ ] Test service lifetime behaviors

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] DI container setup
  - [ ] Service registration patterns
  - [ ] Constructor vs method injection
  - [ ] Service lifetime management

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/InAction.DependencyInjection/`

Key features to compare:
- IServiceCollection configuration
- [FromService] attribute usage
- Service registration methods
- Scoped service handling