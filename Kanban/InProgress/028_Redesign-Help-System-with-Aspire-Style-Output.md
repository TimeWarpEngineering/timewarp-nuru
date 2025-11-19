# Redesign Help System with Aspire-Style Output

## Description

Redesign the help system to produce clean, aspire-style output that filters out help routes and provides structured sections (Description, Usage, Commands, Options) for better user experience.

## Requirements

- Filter out help routes (help, --help, command --help) from display
- Generate aspire-style structured output with proper sections
- Add configurable app description and usage information
- Support both REPL and non-REPL modes

## Checklist

### Design
- [ ] Add Description and AppName properties to NuruAppBuilder
- [ ] Design route filtering logic to exclude help routes
- [ ] Plan aspire-style output format structure

### Implementation  
- [ ] Implement route filtering helper method
- [ ] Rewrite HelpProvider.GetHelpText() for structured output
- [ ] Update GenerateHelpRoutes() to use new help system
- [ ] Add configuration methods for app metadata

### Documentation
- [ ] Update help system documentation
- [ ] Add examples of new help output format

## Notes

Current help output shows all routes including help routes themselves, creating clutter. The aspire format provides clean separation of commands vs options and includes descriptive headers. Nuru is new enough that breaking changes are acceptable for better UX.

## Implementation Notes

- Route filtering should exclude: "help", "--help", "* --help" patterns
- Commands section: routes without leading "--" (excluding help routes)
- Options section: routes with leading "--" (excluding "--help")
- Auto-detect app name from entry point or allow configuration
- No backward compatibility constraints - can break existing API for cleaner design

## Current Issues

The current help output shows:
```
Available Routes:
--help                                  Show available commands
clear                                   Clear the screen
clear-history                           Clear command history
...
Add Commands:
  add --help                            Show help for add command
  add {a:int} {b:int}
...
```

Problems:
1. Help routes themselves are displayed (polluting output)
2. Poor organization - flat list instead of structured sections
3. Missing app metadata (Description/Usage sections)
4. Command help routes mixed with actual commands

## Target Output Format

```
Description:
  [Configurable app description]

Usage:
  [app-name] [command] [options]

Commands:
  greet <name>        Say hello to someone
  status              Show system status
  echo <message>      Echo a message
  add <a> <b>         Add two numbers
  time                Show current time
  clear               Clear the screen
  history             Show command history
  exit                Exit the REPL

Options:
  -?, -h, --help      Show help and usage information
```