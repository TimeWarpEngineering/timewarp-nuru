# Conceptual Documentation

Design documents for TimeWarp.Nuru's route-based CLI framework.

## Documents

### Lexer (`lexer/`)
- **[token-types.md](lexer/token-types.md)** - Token type definitions and validation rules
- **[tokenization-algorithm.md](lexer/tokenization-algorithm.md)** - Character-by-character scanning algorithm

### Parser (`parser/`)
- **[syntax-rules.md](parser/syntax-rules.md)** - Route pattern syntax rules and validation

### Resolver (`resolver/`)
- **[specificity-algorithm.md](resolver/specificity-algorithm.md)** - Route matching and scoring algorithm

### Source Generators (`source-generators/`)
- **[endpoint-generator.md](source-generators/endpoint-generator.md)** - Auto-registration from `[NuruRoute]` attributes

### Cross-Cutting (`cross-cutting/`)
- **[parameter-optionality.md](cross-cutting/parameter-optionality.md)** - Nullability-based optional/required approach
- **[error-handling.md](cross-cutting/error-handling.md)** - Error handling strategy

## Related Documentation

See also: **[Developer Guides](../guides/overview.md)** - Practical guides for building CLI apps
