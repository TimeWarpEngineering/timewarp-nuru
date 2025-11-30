# Recreate InAction.AppConfiguration with Nuru

## Description

Port the Cocona InAction.AppConfiguration sample to Nuru, demonstrating integration with .NET configuration system including appsettings.json and environment-specific configurations.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with configuration support
- Implement appsettings.json and appsettings.Development.json loading
- Demonstrate configuration injection and usage
- Create Overview.md comparing configuration approaches
- Implementation location: `samples/cocona-comparison/InAction/app-configuration`

## Checklist

### Implementation
- [ ] Set up configuration file loading
- [ ] Create appsettings.json and appsettings.Development.json
- [ ] Implement configuration injection into commands
- [ ] Test configuration override behavior

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Configuration setup and registration
  - [ ] IConfiguration usage patterns
  - [ ] Environment-specific configuration
  - [ ] Configuration injection methods

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/InAction.AppConfiguration/`

Key features to compare:
- Configuration builder setup
- JSON file configuration sources
- Environment-based configuration
- IConfiguration dependency injection