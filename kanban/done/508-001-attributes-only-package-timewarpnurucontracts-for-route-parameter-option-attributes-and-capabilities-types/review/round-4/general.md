# Round 4 — general
**Date:** 2026-10-18
**Scope reviewed:** fix delta `bb2d2b9f..50e92396` (product paths), and re-verification of M33–M49

## Summary

Fix pass 4 makes init-only construction linear, always assigns `required` members, exits 1 before an `INuruBehavior` lambda when there is no generated mediator, and matches option names exactly before it ignores case. The same delta covers nullable enum defaults, primary-constructor defaults, constructor selection, capabilities display, the overhead script, and the docs. The derivation suite (35) and generator-55 (12) pass. Generated `NuruGenerated.g.cs` for the derivation app matches those fixes. No new defect.

## Issues

None.
