# Implement per-route help support (command --help shows command-specific help)

## Description

Add support for per-route help where `command --help` shows help specific to that command instead of full app help. Similar pattern to per-app `PrintHelp_N` fix we just implemented for #157.

Example:
```bash
$ mytool deploy --help
deploy {env} [--dry-run,-d] [--force,-f]

  Deploy to the specified environment

Parameters:
  env         Target environment (required)

Options:
  --dry-run, -d    Show what would be deployed without making changes
  --force, -f      Skip confirmation prompts
```

## Checklist

### Analysis
- [x] Review how `--help` is currently handled in `interceptor-emitter.cs`
- [x] Determine how to detect `command --help` vs just `--help`
- [x] Review route metadata available for per-route help output

### Implementation
- [x] Detect `--help` after command literals in argument parsing
- [x] Generate per-route help method or inline help emission
- [x] Format output: pattern, description, parameters, options
- [x] Handle grouped commands (e.g., `git remote --help`)

### Testing
- [x] Test `command --help` shows command-specific help
- [x] Test `--help` alone still shows full app help
- [x] Test grouped command help (e.g., `group command --help`)
- [x] Test command with no options/parameters

## Results

### Implementation

1. **Created `RouteHelpEmitter`** (`source/timewarp-nuru-analyzers/generators/emitters/route-help-emitter.cs`)
   - Generates per-route help output
   - Checks if args end with `--help`/`-h` and match route's literal prefix
   - Outputs: pattern, description, Parameters section, Options section

2. **Modified `RouteMatcherEmitter`** (`source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`)
   - Added call to `RouteHelpEmitter.EmitPerRouteHelpCheck()` at start of each route's matching block

3. **Updated `routing-15-help-route-priority.cs`** - Removed `[Skip]` from passing test

4. **Created `help/help-01-per-route-help.cs`** - 9 comprehensive tests

### Test Results

All 533 CI tests pass (532 passed, 1 skipped as expected)

## Notes

### How It Works

Before each route's matching logic, the emitter generates a check for `[literal1, literal2, ..., "--help" or "-h"]`. If args match this pattern (route's literal prefix + help flag), it outputs inline help showing:
- Full pattern (e.g., `deploy {env} [--force,-f] [--dry-run,-d]`)
- Description (if present)
- Parameters section with names and required/optional status
- Options section with long/short forms and descriptions

Then returns 0 without executing the handler.

### Example Output

```bash
$ mytool deploy --help
deploy {env} [--force,-f] [--dry-run,-d]

  Deploy to the specified environment

Parameters:
  env            (required)

Options:
  --force, -f              (optional)
  --dry-run, -d            (optional)
```
