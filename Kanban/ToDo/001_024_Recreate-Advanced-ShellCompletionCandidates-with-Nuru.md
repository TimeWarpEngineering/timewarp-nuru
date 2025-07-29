# Recreate Advanced.ShellCompletionCandidates with Nuru

## Description

Port the Cocona Advanced.ShellCompletionCandidates sample to Nuru, demonstrating dynamic shell completion suggestions for parameters.

## Parent
001_Recreate-All-Cocona-Samples-Using-Nuru

## Requirements

- Create a Nuru CLI application with completion candidates
- Implement dynamic completion providers
- Support bash/zsh completion
- Create Overview.md comparing completion approaches

## Checklist

### Implementation
- [ ] Create completion candidate providers
- [ ] Implement dynamic suggestions
- [ ] Add shell integration scripts
- [ ] Test completion behavior

### Documentation
- [ ] Create Overview.md with side-by-side comparison
- [ ] Document differences in:
  - [ ] Completion provider interfaces
  - [ ] Dynamic suggestion generation
  - [ ] Shell integration methods
  - [ ] Completion data sources

## Notes

Original Cocona sample location: `/home/steventcramer/worktrees/github.com/mayuki/Cocona/master/samples/Advanced.ShellCompletionCandidates/`

Key features to compare:
- CompletionCandidates attribute
- ICoconaCompletionCandidates
- Shell script generation
- Context-aware suggestions