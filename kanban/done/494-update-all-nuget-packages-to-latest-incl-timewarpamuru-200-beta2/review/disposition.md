# Disposition — task 494

**Date:** 2026-10-09
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

Single general reviewer, effort 1. Found no bugs. The pin bumps and the Shouldly 5 test fix are correct, and CI covers them. One suggestion (M1) is wontfix: the prerelease Amuru dependency becomes a release gate for stable Nuru 3.0.0.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | Pre-releases were requested; Nuru is still beta. Stable 3.0.0 needs Amuru 2.0.0 stable first (NU5104), recorded in task Notes | review oracle |

## Escalations

- None
