# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** local uncommitted CI wiring for M14/M15/M29 (run-ci-tests standaloneTests, CiTestExcludes, comment updates)

## Summary

The change correctly adds generator-17 and check-version-04 to the CI second phase, excludes check-version-04 from multi-mode so it is no longer a silent empty include, and lists generator-19/20 on standaloneTests so `#if !JARIBU_MULTI` cases run without extracting new files. generator-28..45 remain on both exclude and standalone lists. Smoke runs for generator-17, check-version-04, generator-19, generator-20, and generator-28 all exited 0. Overall risk is low; no defects found.

## Issues

<!-- none -->
