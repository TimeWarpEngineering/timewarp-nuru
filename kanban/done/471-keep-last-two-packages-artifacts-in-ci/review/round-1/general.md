# Round 1 — general
**Date:** 2026-09-09
**Scope reviewed:** branch task/471-keep-last-two-packages-artifacts-in-ci vs origin/master

## Summary

Green-master-only `Packages-*` upload with `retention-days: 7`, `if-no-files-found: error`, and a post-upload keep-last-two prune that DELETEs older non-expired names starting with `Packages-`. Risk is low: upload gates match the brief, prune is scoped and skips safely when the just-uploaded name is not listed yet, and 458-002 promote code is comment-only. Docs no longer describe always-run / 90-day / `actions: read` as current policy.

## Issues

