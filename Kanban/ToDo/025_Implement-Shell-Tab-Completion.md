# Implement Shell Tab Completion for Command Arguments

## Status: TODO
## Priority: Medium-High
## Category: Feature Enhancement
## Related Issue: [#30](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/30)

## Problem

Users want shell tab completion for CLI arguments to improve discoverability and reduce typing. When typing partial command names like `cre<TAB>`, the shell should complete to `createorder` if unique, or show available options if ambiguous.

### User Request (Issue #30)

> Would it be easy to add "auto completion" of arguments? Like if you have a command "createorder" you can start to type "cre" and hit tab and it will be completed to "createorder" if unique?

### Current State

TimeWarp.Nuru currently provides no shell completion support, requiring users to:
- Type complete command names manually
- Remember exact option names and syntax
- Refer to help documentation frequently

This affects developer experience and makes the CLI feel less polished compared to frameworks with completion support.

## Technical Background

### How Shell Completion Works

Shell completion operates through a **three-tier architecture**:

1. **Shell-specific hook** - Installed in user's shell profile (.bashrc, .zshrc, profile.ps1, config.fish)
2. **Completion function** - Called by shell when Tab is pressed, receives current command line context
3. **Application** - May provide completion data either statically or dynamically

### Shell-Specific Mechanisms

**Bash:**
- Uses `complete -F function_name app_name` to register completion function
- Completion function uses `COMP_WORDS`, `COMP_CWORD` variables to understand context
- Generates candidates using `compgen` builtin
- Populates `COMPREPLY` array with matching completions

**Zsh:**
- Uses `compsys` completion system with `_arguments` function
- More sophisticated than bash with option grouping and mutual exclusion
- Defined in files under `~/.zsh/completions/` or inline

**PowerShell:**
- Uses `Register-ArgumentCompleter` cmdlet with ScriptBlock
- Returns `CompletionResult` objects with value and description
- Most native PowerShell integration of all shells

**Fish:**
- Declarative syntax with separate `complete` commands
- Stored in `~/.config/fish/completions/app.fish`
- Simplest syntax but requires separate file per command

### How Other .NET CLI Frameworks Handle This

| Framework | Approach | Complexity | Current Status |
|-----------|----------|------------|----------------|
| **System.CommandLine** | `dotnet-suggest` global tool as mediator | High - requires global tool install | Active but disabled by default |
| **Cocona** | Built-in `--completion` command generates static scripts | Medium | **Disabled by default as of recent versions** |
| **Spectre.Console** | No built-in support | N/A | Third-party package `JKToolKit.Spectre.AutoCompletion` exists |

**Key Insight:** Even major frameworks struggle with completion. Cocona disabled it by default, suggesting maintenance burden or complexity issues. System.CommandLine requires an entire separate tool installation.

### TimeWarp.Nuru's Advantages

The excellent news: **Nuru has exceptional infrastructure already in place!**

1. **Comprehensive Route Parsing** - `CompiledRoute` class contains structured segments:
   - `PositionalMatchers` - for parameter completion
   - `OptionMatchers` - for option name completion
   - `LiteralSegment` - for command name completion
   - Type information for all parameters

2. **EndpointCollection** - Central registry of all routes:
   - Already sorted by specificity
   - Contains descriptions for help text
   - Easy iteration to build completion candidates

3. **Type Conversion System** - `ITypeConverterRegistry`:
   - Knows about enums (can generate value completions)
   - Knows appropriate types for file path completion
   - Extensible for custom completion providers

4. **Help System** - `HelpProvider` already extracts:
   - Command names
   - Parameter names and types
   - Option names and descriptions
   - Can be adapted for completion descriptions

5. **Route Matching Logic** - `EndpointResolver` understands:
   - Positional arguments
   - Options (long and short forms)
   - Optional parameters
   - Catch-all parameters
   - Can be adapted to return "what's valid here" instead of "does this match"

**Critical Advantage:** The same infrastructure works for **both Direct Delegate and Mediator approaches** since both use identical `EndpointCollection` and `CompiledRoute` structures.

## Recommended Approach

### Start with Static Script Generation (Cocona-inspired but improved)

**Why Static First:**
- Covers 90% of use cases
- Zero runtime overhead (no subprocess calls on Tab press)
- No additional tools to install (unlike System.CommandLine's dotnet-suggest)
- Simple to test and maintain
- Can add dynamic completion later if needed

**Improvements over Cocona:**
- Better error handling
- Support for Fish shell (Cocona only does bash/zsh)
- Clearer installation instructions
- Optional separate NuGet package to keep core lightweight

### Phase 1: Core Completion Engine (Foundation)

**New Components:**

1. **CompletionProvider Class**
   ```csharp
   public class CompletionProvider
   {
       public IEnumerable<CompletionCandidate> GetCompletions(
           CompletionContext context,
           EndpointCollection endpoints);
   }
   ```

2. **CompletionContext Record**
   ```csharp
   public record CompletionContext(
       string[] Args,              // Arguments typed so far
       int CursorPosition,         // Where cursor is in args
       EndpointCollection Endpoints // All registered routes
   );
   ```

3. **CompletionCandidate Record**
   ```csharp
   public record CompletionCandidate(
       string Value,               // What to complete to
       string? Description,        // Help text for this completion
       CompletionType Type         // Command, Option, Parameter, File, etc.
   );

   public enum CompletionType
   {
       Command,      // Literal command names
       Option,       // --force, -m, etc.
       Parameter,    // Parameter values
       File,         // File path completion
       Directory,    // Directory path completion
       Enum,         // Enum value completion
       Custom        // From ICompletionSource
   }
   ```

**Core Logic:**
1. Parse args up to cursor position
2. Determine what's expected next (command literal, parameter value, option, etc.)
3. Query `EndpointCollection` for matching routes
4. Extract appropriate candidates from `CompiledRoute` segments
5. Return candidates with descriptions

### Phase 2: Static Script Generation

**New Components:**

1. **Shell Script Templates** (embedded resources)
   - Bash completion function template
   - Zsh `_arguments` specification template
   - PowerShell `Register-ArgumentCompleter` template
   - Fish `complete` commands template

2. **CompletionScriptGenerator Class**
   ```csharp
   public class CompletionScriptGenerator
   {
       public string GenerateBash(EndpointCollection endpoints);
       public string GenerateZsh(EndpointCollection endpoints);
       public string GeneratePowerShell(EndpointCollection endpoints);
       public string GenerateFish(EndpointCollection endpoints);
   }
   ```

3. **Built-in Route for Generation**
   ```csharp
   // Auto-registered when EnableShellCompletion() is called
   .AddRoute("--generate-completion {shell}", (string shell) => {
       // Validate shell is bash|zsh|pwsh|fish
       // Generate appropriate script
       // Output to stdout
   })
   ```

**Implementation Details:**

For **static generation** (Phase 2):
- Extract all literal commands from route patterns
- Extract all options from `OptionMatcher` instances
- Generate parameter completions based on types:
  - Enum types → generate all enum values as candidates
  - FileInfo/file types → shell's file completion directive
  - DirectoryInfo/directory types → shell's directory completion directive
  - String/custom types → no completion or custom provider
- Include descriptions from route definitions in completion help text

### Phase 3: Dynamic Completion (Optional, Future Enhancement)

**For apps that need runtime-computed suggestions:**

1. **ICompletionSource Interface**
   ```csharp
   public interface ICompletionSource
   {
       IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context);
   }
   ```

2. **Registration API**
   ```csharp
   .AddRoute("deploy {env}", ...)
   .WithCompletionSource("env", new EnvironmentCompletionSource())
   ```

3. **Runtime Query Mechanism**
   - App exports hidden route like `__complete {*args}` or `--complete {index} {*words}`
   - Shell completion script calls app with special args
   - App returns candidates to stdout (one per line, optionally tab-separated with descriptions)
   - Shell filters based on current input

**Example:**
```bash
# Shell calls: myapp --complete 1 deploy prod
# App returns:
production     Deploy to production environment
preview        Deploy to preview environment
staging        Deploy to staging environment
```

### Phase 4: Builder API Integration

**User-facing API:**

```csharp
var app = new NuruAppBuilder()
    .AddRoute("createorder {product}", ...)
    .AddRoute("status", ...)
    .AddRoute("deploy {env} --version {ver}", ...)
    .EnableShellCompletion()  // Adds --generate-completion route
    .Build();

await app.RunAsync(args);
```

**Generation and Installation:**
```bash
# Generate and install bash completion
./myapp --generate-completion bash > ~/.bash_completion.d/myapp
source ~/.bash_completion.d/myapp

# Or inline for current session
source <(./myapp --generate-completion bash)

# Zsh
./myapp --generate-completion zsh > ~/.zsh/completions/_myapp

# PowerShell
./myapp --generate-completion pwsh >> $PROFILE

# Fish
./myapp --generate-completion fish > ~/.config/fish/completions/myapp.fish
```

**Usage:**
```bash
./myapp cre<TAB>              # Completes to "createorder"
./myapp createorder <TAB>      # Shows {product} help
./myapp deploy <TAB>           # Shows {env} help or suggestions
./myapp deploy prod --v<TAB>   # Completes to "--version"
```

## Implementation Plan

### Phase 1: Core Completion Engine

**Files to Create:**
- `/Source/TimeWarp.Nuru/Completion/CompletionProvider.cs`
- `/Source/TimeWarp.Nuru/Completion/CompletionContext.cs`
- `/Source/TimeWarp.Nuru/Completion/CompletionCandidate.cs`
- `/Source/TimeWarp.Nuru/Completion/CompletionType.cs`

**Implementation:**
1. Create `CompletionProvider` class that analyzes `EndpointCollection`
2. Implement logic to determine what's expected at cursor position
3. Extract candidates from route segments:
   - Commands from `LiteralSegment` instances
   - Options from `OptionMatcher` instances
   - Parameters from `ParameterSegment` types
4. Handle enum types by reflecting available values
5. Handle file/directory types with appropriate completion type flags

**Tests to Add:**
- Test command name completion (partial match)
- Test option name completion (--long and -short)
- Test parameter position detection
- Test enum value completion
- Test optional parameter handling
- Test catch-all parameter handling

### Phase 2: Static Script Generation

**Files to Create:**
- `/Source/TimeWarp.Nuru/Completion/CompletionScriptGenerator.cs`
- `/Source/TimeWarp.Nuru/Completion/Templates/bash-completion.sh` (embedded resource)
- `/Source/TimeWarp.Nuru/Completion/Templates/zsh-completion.zsh` (embedded resource)
- `/Source/TimeWarp.Nuru/Completion/Templates/pwsh-completion.ps1` (embedded resource)
- `/Source/TimeWarp.Nuru/Completion/Templates/fish-completion.fish` (embedded resource)

**Files to Update:**
- `/Source/TimeWarp.Nuru/NuruAppBuilder.cs` - Add `EnableShellCompletion()` method
- `/Source/TimeWarp.Nuru/NuruApp.cs` - Auto-register `--generate-completion {shell}` route when enabled

**Implementation:**
1. Create shell script templates with placeholders for:
   - App name
   - Command names
   - Option names
   - Option descriptions
   - Parameter types
2. Implement `CompletionScriptGenerator` to populate templates
3. Add route handler for `--generate-completion {shell}`
4. Validate shell parameter (bash|zsh|pwsh|fish)
5. Generate and output appropriate script to stdout

**Tests to Add:**
- Test script generation for each shell type
- Test generated scripts contain all route commands
- Test generated scripts contain all route options
- Test invalid shell name handling
- Integration test: actual completion in bash/zsh (if possible in CI)

### Phase 3: Dynamic Completion (Optional)

**Files to Create:**
- `/Source/TimeWarp.Nuru/Completion/ICompletionSource.cs`
- `/Source/TimeWarp.Nuru/Completion/DynamicCompletionExtensions.cs`

**Files to Update:**
- Shell script templates to support runtime queries
- `NuruApp.cs` to handle `--complete` directive

**Implementation:**
1. Create `ICompletionSource` interface
2. Add `.WithCompletionSource(paramName, source)` API
3. Auto-register hidden `--complete {index} {*words}` route
4. Update shell templates to call app for dynamic completions
5. Implement filtering logic (app returns all, shell filters, or app filters)

**Decision:** Consider only if users request it based on Phase 2 usage.

### Phase 4: Documentation and Samples

**Files to Create:**
- `/documentation/user/guide/shell-completion.md` - Comprehensive guide
- `/Samples/ShellCompletion/` - Sample app demonstrating completion

**Files to Update:**
- `/readme.md` - Add shell completion feature highlight
- `/documentation/user/getting-started.md` - Mention completion setup

**Documentation Content:**
- How shell completion works (brief explanation)
- Installation instructions for each shell
- Troubleshooting common issues
- How to test if completion is working
- Comparison to other frameworks
- Limitations and considerations

**Sample Application:**
- Demonstrate various completion scenarios:
  - Multiple commands with partial matching
  - Options (long and short forms)
  - Enum parameters
  - File/directory parameters
  - Optional parameters
  - Both Direct and Mediator examples

## Package Consideration

**Option A: Include in Core** (`TimeWarp.Nuru`)
- Pros: Convenient, discoverable, single package
- Cons: Increases core size, optional feature for many users

**Option B: Separate Package** (`TimeWarp.Nuru.Completion`)
- Pros: Keeps core minimal, users opt-in, clearer dependency
- Cons: Extra package to install, less discoverable

**Recommendation:** Start with **separate package** to keep core lightweight, similar to how Cocona's approach is optional. Can always merge later if it proves universally useful.

## Files to Update Summary

### Phase 1 (Core Engine)
**New:**
- `/Source/TimeWarp.Nuru.Completion/Completion/CompletionProvider.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/CompletionContext.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/CompletionCandidate.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/CompletionType.cs`
- `/Tests/TimeWarp.Nuru.Completion.Tests/` (test project)

### Phase 2 (Static Generation)
**New:**
- `/Source/TimeWarp.Nuru.Completion/Completion/CompletionScriptGenerator.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/bash-completion.sh`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/zsh-completion.zsh`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/pwsh-completion.ps1`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/fish-completion.fish`

**Updated:**
- `/Source/TimeWarp.Nuru.Completion/CompletionExtensions.cs` (adds `EnableShellCompletion()`)

### Phase 3 (Dynamic - Optional)
**New:**
- `/Source/TimeWarp.Nuru.Completion/Completion/ICompletionSource.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/DynamicCompletionExtensions.cs`

**Updated:**
- Shell script templates
- Completion route registration

### Phase 4 (Documentation & Samples)
**New:**
- `/documentation/user/guide/shell-completion.md`
- `/Samples/ShellCompletion/`

**Updated:**
- `/readme.md`
- `/documentation/user/getting-started.md`

## Success Criteria

### Phase 1 (Core Engine)
- [ ] `CompletionProvider` analyzes `EndpointCollection` successfully
- [ ] Can extract command names from routes
- [ ] Can extract option names from routes
- [ ] Can determine expected parameter types at cursor position
- [ ] Handles enum types with value extraction
- [ ] Handles file/directory types appropriately
- [ ] Unit tests cover all completion scenarios

### Phase 2 (Static Generation)
- [ ] `CompletionScriptGenerator` generates valid bash scripts
- [ ] `CompletionScriptGenerator` generates valid zsh scripts
- [ ] `CompletionScriptGenerator` generates valid PowerShell scripts
- [ ] `CompletionScriptGenerator` generates valid fish scripts
- [ ] `--generate-completion {shell}` route works correctly
- [ ] Invalid shell names return helpful error message
- [ ] Generated scripts contain all commands from routes
- [ ] Generated scripts contain all options from routes
- [ ] Generated scripts include descriptions where available
- [ ] Manual testing confirms completion works in bash
- [ ] Manual testing confirms completion works in zsh
- [ ] Manual testing confirms completion works in PowerShell
- [ ] Manual testing confirms completion works in fish

### Phase 3 (Dynamic - Optional)
- [ ] `ICompletionSource` interface defined
- [ ] `.WithCompletionSource()` API works
- [ ] Runtime query mechanism functional
- [ ] Shell scripts call back to app correctly
- [ ] Dynamic completions appear in shell

### Phase 4 (Documentation & Samples)
- [ ] Shell completion guide is comprehensive and clear
- [ ] Installation instructions work for all shells
- [ ] Troubleshooting section covers common issues
- [ ] Sample app demonstrates all completion types
- [ ] README mentions shell completion feature
- [ ] Getting started guide includes completion setup

### Issue Resolution
- [ ] Issue #30 resolved with working implementation
- [ ] Response posted to issue with usage examples
- [ ] User's request for `cre<TAB>` → `createorder` works

## Related Tasks and Issues

- **Issue #30**: Command argument completion (this task)
- **Task 024**: Support-Custom-Type-Converters (custom types could provide completion candidates)
- **Task 001_024**: Recreate-Advanced-ShellCompletionCandidates-with-Nuru (Cocona comparison)

## Related Frameworks Research

- **System.CommandLine**: Uses `dotnet-suggest` global tool approach
  - Repository: https://github.com/dotnet/command-line-api
  - Completion docs: https://github.com/dotnet/command-line-api/blob/main/docs/Features-overview.md#suggestions

- **Cocona**: Built-in `--completion` command (now disabled by default)
  - Repository: https://github.com/mayuki/Cocona
  - Completion implementation: https://github.com/mayuki/Cocona/tree/main/src/Cocona.Core/ShellCompletion

- **Spectre.Console**: No built-in support, third-party package exists
  - Repository: https://github.com/spectreconsole/spectre.console
  - Third-party: https://github.com/joachimgoris/Spectre.Console.AutoCompletion

## Breaking Changes

**None** - This is a new optional feature:
- Core Nuru package unchanged (if using separate package approach)
- No changes to existing route APIs
- Purely additive functionality
- Users opt-in via `EnableShellCompletion()`

## Benefits

### For Users
- ✅ Professional CLI experience with tab completion
- ✅ Improved discoverability of commands and options
- ✅ Reduced typing and fewer errors
- ✅ Better than System.CommandLine (no global tool required)
- ✅ Better than Cocona (supports more shells, actively maintained)

### For Framework
- ✅ Feature parity with major CLI frameworks
- ✅ Competitive advantage in UX
- ✅ Leverages existing infrastructure exceptionally well
- ✅ Single implementation works for both Direct and Mediator
- ✅ Demonstrates framework maturity and completeness

## Technical Challenges

### Shell Diversity
- Each shell has different syntax and capabilities
- Must maintain 4+ different script templates
- Testing across different shells/platforms

**Mitigation:**
- Start with bash/zsh (covers most developers)
- Use embedded resource templates for maintainability
- Provide clear testing instructions for each shell

### Installation Friction
- Users must manually add scripts to shell profiles
- Different shells have different locations and methods
- May need to restart shell or source files

**Mitigation:**
- Clear, step-by-step installation docs for each shell
- Provide copy-paste commands that work
- Consider future: installer script that detects shell

### Testing Complexity
- Must test actual completion in real shells
- CI environments may not have interactive shell support
- Manual testing required across platforms

**Mitigation:**
- Unit tests for completion logic and script generation
- Manual test checklist for each shell
- Document testing procedure for contributors

### Dynamic Completion Performance
- If Phase 3 implemented, every Tab press = subprocess call
- Could be slow for complex apps or slow systems

**Mitigation:**
- Make dynamic completion optional (Phase 3)
- Start with static (Phase 2) which has zero overhead
- Recommend caching in custom completion sources

## Timeline Estimate

- **Phase 1 (Core Engine)**: 6-8 hours
  - CompletionProvider implementation: 3-4 hours
  - Unit tests: 3-4 hours

- **Phase 2 (Static Generation)**: 10-14 hours
  - Script templates (4 shells): 4-6 hours
  - Generator implementation: 2-3 hours
  - Integration and manual testing: 4-5 hours

- **Phase 3 (Dynamic - Optional)**: 6-8 hours
  - Interface and API: 2-3 hours
  - Runtime query mechanism: 2-3 hours
  - Template updates and testing: 2-3 hours

- **Phase 4 (Documentation & Samples)**: 4-6 hours
  - Documentation: 2-3 hours
  - Sample application: 2-3 hours

**Total Estimate (Phase 1+2+4)**: 20-28 hours
**Total Estimate (All Phases)**: 26-36 hours

**Complexity**: Medium-High
- Core logic: Medium (good infrastructure exists)
- Script generation: Medium (multiple shell syntaxes)
- Testing: High (cross-platform, cross-shell)
- Documentation: Medium (clear examples needed)

## Priority Justification

**Medium-High Priority** because:

1. **User Request**: Active issue #30 with community interest
2. **UX Impact**: Dramatically improves developer experience
3. **Competitive**: Major frameworks have this, absence looks incomplete
4. **Feasible**: Excellent infrastructure already exists in Nuru
5. **Differentiator**: Can be better than competitors (no global tool, more shells)
6. **Professional Polish**: Makes CLI feel production-ready
7. **Not Blocking**: Nice-to-have enhancement, not critical path

**Not Higher Priority Because:**
- Doesn't block core functionality
- Other frameworks made it optional/disabled
- Implementation effort is substantial
- Users can still use CLI effectively without it

**Recommendation:** Implement after core type system features (like Task 024) are complete, but before 1.0 release for maximum impact.
