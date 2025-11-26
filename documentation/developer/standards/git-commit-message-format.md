# Commit Message Format

Follow Conventional Commits v1.0.0 specification:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

## Required Fields

### Type (REQUIRED)
Core types:
- `feat`: New feature (correlates with MINOR in SemVer)
- `fix`: Bug fix (correlates with PATCH in SemVer)

Common additional types:
- `build`: Build system or external dependencies
- `chore`: Maintenance tasks
- `ci`: CI configuration files and scripts
- `docs`: Documentation only
- `style`: Formatting, white-space, no code change
- `refactor`: Code change that neither fixes bug nor adds feature
- `perf`: Performance improvement
- `test`: Adding missing tests or correcting existing

### Description (REQUIRED)
- Short summary immediately after colon and space
- Focus on WHY from user perspective, not WHAT
- No period at end

## Optional Fields

### Scope
- Noun describing section of codebase in parentheses
- Project-specific: `documentation` → use `docs` type instead
- Omit for cross-cutting changes

### Body
- Additional context about the changes
- Free-form, multiple paragraphs allowed
- One blank line after description
- Explain motivation and contrast with previous behavior

### Footer(s)
- One blank line after body
- Format: `token: value` or `token #value`
- Common footers:
  - `BREAKING CHANGE: <description>` - Breaking API changes
  - `Closes #123`, `Fixes #456` - Issue references
  - `Reviewed-by: Name`
  - `Refs: #123, #456`

## Breaking Changes

Two ways to indicate:
1. Add `!` after type/scope: `feat!:` or `feat(api)!:`
2. Include `BREAKING CHANGE:` footer

## Examples

### Simple commit
```
fix: prevent racing of requests
```

### With scope
```
feat(parser): add ability to parse arrays
```

### Breaking change with `!`
```
feat!: send email when product ships
```

### With body and footer
```
fix: prevent racing of requests

Introduce a request id and reference to latest request.
Dismiss incoming responses other than from latest request.

Remove timeouts which were used to mitigate the racing issue.

Reviewed-by: Z
Refs: #123
```

### Breaking change with footer
```
feat: allow config object to extend other configs

BREAKING CHANGE: `extends` key in config file is now used for extending other config files
```

## Key Differences from Angular Convention
- Description instead of subject (same concept, different name)
- `!` notation for breaking changes is explicitly supported
- More flexible footer format following git trailer convention
- Case insensitive (except BREAKING CHANGE must be uppercase)
