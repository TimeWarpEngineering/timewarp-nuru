# Add Shell Completion Support

## Description

Implement shell completion (tab completion) support for Nuru CLI applications, providing automatic command and parameter suggestions in bash, zsh, PowerShell, and other shells. This would achieve parity with Cocona's `EnableShellCompletionSupport` feature.

## Requirements

- Generate shell completion scripts for major shells (bash, zsh, PowerShell, fish)
- Support completion for:
  - Command names and sub-commands
  - Option names (both long and short forms)
  - Parameter values (including enum values)
  - File paths where appropriate
- Provide easy installation mechanism for completion scripts
- Support dynamic completion for custom types

## Implementation Plan

### Phase 1: Core Completion Infrastructure
- Create completion generator that analyzes route patterns
- Build data structure representing all possible completions
- Implement completion script templates for each shell type

### Phase 2: Static Completion Generation
- Add `--generate-completion` command to output completion scripts
- Support for bash completion using complete/compgen
- Support for zsh completion using _arguments
- Support for PowerShell using Register-ArgumentCompleter
- Support for fish completion

### Phase 3: Dynamic Completion
- Support completion providers for custom parameter types
- Allow commands to provide dynamic completion values
- Implement file path completion for appropriate parameters
- Add enum value completion with case-insensitive matching

### Phase 4: Integration and Installation
- Add `.EnableShellCompletion()` method to NuruAppBuilder
- Automatic script generation during build/publish
- Installation helpers for each platform
- Documentation for manual installation

## Example Usage

```csharp
// Enable shell completion in the app
var app = new NuruAppBuilder()
    .AddRoute("deploy {env} --version {ver}", ...)
    .EnableShellCompletion()  // Add this to enable completion
    .Build();

// Generate completion scripts
./myapp --generate-completion bash > myapp-completion.bash
./myapp --generate-completion zsh > _myapp
./myapp --generate-completion powershell > myapp-completion.ps1

// Usage after installation
./myapp dep<TAB>          # Completes to "deploy"
./myapp deploy <TAB>       # Shows available environments
./myapp deploy prod --v<TAB>  # Completes to "--version"
```

## Technical Considerations

### Bash Completion
- Use `complete` builtin with `-W` for word lists
- Implement `COMP_WORDS` and `COMP_CWORD` handling
- Support both bash-completion v1 and v2

### Zsh Completion
- Use `_arguments` for sophisticated completion
- Support option grouping and mutual exclusion
- Implement `_files` for file completion

### PowerShell Completion
- Use `Register-ArgumentCompleter` cmdlet
- Support parameter sets and dynamic parameters
- Implement proper error handling for Windows

### Performance
- Cache completion data to avoid repeated parsing
- Minimize subprocess calls during completion
- Consider lazy loading for large command sets

## Dependencies

- Requires analysis of all registered routes
- Needs platform detection for appropriate script generation
- Should integrate with existing help system

## Comparison with Cocona

Cocona's implementation:
```csharp
CoconaApp.Run<Program>(args, options =>
{
    options.EnableShellCompletionSupport = true;
});
```

Proposed Nuru implementation:
```csharp
var app = new NuruAppBuilder()
    .EnableShellCompletion()
    .Build();
```

## Benefits

- Improved developer experience with tab completion
- Reduced typing and command errors
- Better discoverability of commands and options
- Professional feel for CLI applications
- Feature parity with Cocona

## Notes

- Shell completion is a significant usability feature for CLI apps
- Implementation complexity varies significantly by shell
- Should provide graceful degradation if completion isn't available
- Consider providing completion as a separate NuGet package to keep core lightweight