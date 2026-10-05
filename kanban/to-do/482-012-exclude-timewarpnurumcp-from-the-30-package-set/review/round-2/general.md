# Round 2 — general
**Date:** 2026-10-05
**Scope reviewed:** M1 fix on `documentation/user/tools/overview.md`, plus a scan of that delta for new defects. Pack-gate behavior from round 1 was not reopened.

## Summary

The tools overview highlights row now says the MCP server is frozen and not part of 3.0. The Installation subsection says the same and limits `dotnet tool install --global TimeWarp.Nuru.Mcp` to older prereleases, matching the root readme. The banner and the `skills/tw-nuru/SKILL.md` pointer are unchanged. No new defects on the fix delta.

## Issues

### Issue 1 — Severity: suggestion
- File: documentation/user/tools/overview.md:29
- Description: Round 1 M1. The highlights row and Installation section now state that `TimeWarp.Nuru.Mcp` is not part of the 3.0 release, and the install command is qualified as older prereleases.
- Suggestion: Applied on this task.
- Status: fixed
