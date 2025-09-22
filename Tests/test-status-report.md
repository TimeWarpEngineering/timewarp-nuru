# Test Status Report

## Summary
- Total Tests: 54
- Passing: 28 (51.9%)
- Failing: 26 (48.1%)

## Lexer Tests (4/4 - 100% PASS)
| Test File | Status |
|-----------|--------|
| test-lexer-double-dash-separator.cs | ✓ PASS |
| test-lexer-hang.cs | ✓ PASS |
| test-lexer-only.cs | ✓ PASS |
| test-lexer-optional-modifiers.cs | ✓ PASS |

## Parser Tests (9/10 - 90% PASS)
| Test File | Status |
|-----------|--------|
| test-analyzer-patterns.cs | ✓ PASS |
| test-hanging-patterns-fixed.cs | ✓ PASS |
| test-hanging-patterns.cs | ✓ PASS |
| test-parser-end-of-options.cs | ✓ PASS |
| test-parser-error-cases.cs | ✓ PASS |
| test-parser-errors.cs | ✓ PASS |
| test-parser-mixed-modifiers.cs | ✓ PASS |
| test-parser-optional-flags.cs | ✓ PASS |
| test-parser-repeated-options.cs | ✓ PASS |
| test-specific-hanging.cs | ✗ FAIL |

## Routing Tests (0/3 - 0% PASS)
| Test File | Status |
|-----------|--------|
| test-all-routes.cs | ✗ FAIL |
| test-boolean-option.cs | ✗ FAIL |
| test-route-matching.cs | ✗ FAIL |

## Features Tests (4/11 - 36.4% PASS)
| Test File | Status |
|-----------|--------|
| test-auto-help.cs | ✗ FAIL |
| test-desc.cs | ✗ FAIL |
| test-four-optional-options.cs | ✗ FAIL |
| test-kubectl.cs | ✗ FAIL |
| test-logging.cs | ✗ FAIL |
| test-mixed-options.cs | ✓ PASS |
| test-option-combinations.cs | ✓ PASS |
| test-option-params.cs | ✗ FAIL |
| test-optional-option-params.cs | ✓ PASS |
| test-shell-behavior.cs | ✗ FAIL |
| test-truly-optional-options.cs | ✓ PASS |

## Options Tests (test-matrix) (7/20 - 35% PASS)
| Test File | Status |
|-----------|--------|
| test-array-parameters.cs | ✓ PASS |
| test-boolean-flags.cs | ✓ PASS |
| test-catch-all-with-options.cs | ✗ FAIL |
| test-catch-all.cs | ✓ PASS |
| test-catchall-validation.cs | ✓ PASS |
| test-combined-patterns.cs | ✗ FAIL |
| test-interception-patterns.cs | ✗ FAIL |
| test-invalid-positional-patterns.cs | ✗ FAIL |
| test-mixed-required-optional.cs | ✗ FAIL |
| test-nurucontext.cs | ✗ FAIL |
| test-optional-flag-optional-value.cs | ✗ FAIL |
| test-optional-flag-required-value.cs | ✗ FAIL |
| test-optional-flags-syntax.cs | ✓ PASS |
| test-positional-optional-after-required.cs | ✗ FAIL |
| test-repeated-options.cs | ✗ FAIL |
| test-required-flag-optional-value.cs | ✗ FAIL |
| test-required-flag-required-value.cs | ✓ PASS |
| test-specificity-ordering.cs | ✓ PASS |
| test-typed-optional-parameters.cs | ✗ FAIL |
| test-typed-parameters.cs | ✗ FAIL |

## MCP Tests (4/6 - 66.7% PASS)
| Test File | Status |
|-----------|--------|
| test-dynamic-examples.cs | ✗ FAIL |
| test-error-handling.cs | ✓ PASS |
| test-generate-handler.cs | ✓ PASS |
| test-get-syntax.cs | ✓ PASS |
| test-mcp-server.cs | ✗ FAIL |
| test-validate-route.cs | ✓ PASS |

## Failed Tests List (26 total)
1. Parser/test-specific-hanging.cs
2. Routing/test-all-routes.cs
3. Routing/test-boolean-option.cs
4. Routing/test-route-matching.cs
5. Features/test-auto-help.cs
6. Features/test-desc.cs
7. Features/test-four-optional-options.cs
8. Features/test-kubectl.cs
9. Features/test-logging.cs
10. Features/test-option-params.cs
11. Features/test-shell-behavior.cs
12. Options/test-catch-all-with-options.cs
13. Options/test-combined-patterns.cs
14. Options/test-interception-patterns.cs
15. Options/test-invalid-positional-patterns.cs
16. Options/test-mixed-required-optional.cs
17. Options/test-nurucontext.cs
18. Options/test-optional-flag-optional-value.cs
19. Options/test-optional-flag-required-value.cs
20. Options/test-positional-optional-after-required.cs
21. Options/test-repeated-options.cs
22. Options/test-required-flag-optional-value.cs
23. Options/test-typed-optional-parameters.cs
24. Options/test-typed-parameters.cs
25. MCP/test-dynamic-examples.cs
26. MCP/test-mcp-server.cs