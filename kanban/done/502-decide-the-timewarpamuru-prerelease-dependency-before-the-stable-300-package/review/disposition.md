# Disposition — task 502

**Date:** 2026-10-14
**Outcome:** clean
**Rounds:** 3
**Final open count:** 0

## Summary

There were three rounds with the `general` roster: round 1 at effort 1, rounds 2 and 3 at effort 2 by diff size. Four
findings were raised: M1 (bug), M2 (suggestion), M3 (bug, a regression from M1's suggested wording), and M4 (nit). All four
are fixed on this task id (06acf71a, b3ab7c4c). The product change is correct. `Directory.Packages.props` pins
`TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` at 2.0.0, and the user docs say that stable 3.0.0 depends on stable Amuru
2.0.0. Their beta claim (only beta.80 used 2.0.0-beta.2) checks against the nuget.org nuspecs.

## Exception log (if accepted-exceptions)

None.

## Escalations

- None.
