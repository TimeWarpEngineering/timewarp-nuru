## Git Guidelines

### Merge Strategy

- **NEVER use `--squash` when merging PRs** - use `gh pr merge --merge --delete-branch`
- **NEVER use rebase**
- Always use `--head` flag when creating pull requests with `gh pr create`

### Worktree Awareness

When working in worktrees:
- Cannot checkout master or delete local branches (worktrees are tied to their branch)
- Use full paths when referencing files across worktrees

### Code Comments

- **NEVER use temporal comments** like "currently", "now", "at this time" in code
- Temporal state changes constantly - code comments should be timeless
- Reference specific versions if needed: "As of version X.Y.Z..."
- Focus on what the code does, not when it was written

### Commit Messages

Use conventional commit format:
- `feat:` - new feature
- `fix:` - bug fix
- `docs:` - documentation
- `test:` - tests
- `chore:` - maintenance
- `refactor:` - code restructuring

Include co-author line when AI assisted:
```
Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
```
