# Parsing (`source/timewarp-nuru-parsing`)

Baseline `5a06e900`. Re-checked against the tree on 2026-10-05.

## P-1 — The syntax-versus-semantics reference names a pipeline that is not in the source

- Disposition: **fix-now** (482-013, with D-1)
- Evidence: `documentation/developer/reference/parser-classes-syntax-vs-semantics.md` lists `RoutePatternAst`, `SegmentNode`, `LiteralNode`, `ParameterNode`, `OptionNode`, and `NewRoutePatternParser`. None of those types exist under `source/`. The implementation is `Lexer` / `Token` / `RouteTokenType`, syntax nodes (`Syntax`, `LiteralSyntax`, `ParameterSyntax`, `OptionSyntax`, `EndOfOptionsSyntax`), `Parser`, `SemanticValidator`, `Compiler`, and `CompiledRoute` with `LiteralMatcher`, `ParameterMatcher`, `OptionMatcher`, and `RouteMatcher`.
- The same document's option example treats `OptionSyntax.LongForm` / `ShortForm` as including the dashes. The matcher stores the name without dashes and keeps the dash form on `MatchPattern` / `AlternateForm`.
- `documentation/developer/reference/parsing-flow-dependency-analysis.md` repeats `NewRoutePatternParser` and `RoutePatternAst`.
- Why it matters: This is the developer reference for the parser. It cannot be used to find the types that actually parse routes.

## Checked, not a 3.0 blocker

- `common-strings.cs` is an `internal` intern table (dashes, boolean spellings, braces). It is not a user-facing message catalog. No text-quality defect.
- `MessageType` is a small enum on `CompiledRoute` (default `Command`). Help emission does not print the enum name at the user. No message-text defect beyond P-1.
- Boolean spellings in `CommonStrings` (`true`/`yes`/`1`/`on`/`enabled` and the false counterparts) match the converter the runtime uses. Not a parser/docs split of its own.
- Analyzer validation calls this package's `PatternParser` (see `review/analyzers.md` A-2). The grammar is shared. The defect is the three errors the generator drops, which is an analyzer disposition, not a second parser.
