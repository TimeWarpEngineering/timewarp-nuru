# Round 2 — general
**Date:** 2026-11-14
**Scope reviewed:** Delta `e95f5752..0d3c82e3`. Product change is the Design regions in `source/timewarp-nuru/telemetry/nuru-telemetry-options.cs` and `source/timewarp-nuru-parsing/message-type.cs`. Checked against round-1 M1 and M2, the groomed task, and `## Goal (implementer restatement)`.

## Summary

The fix pass rewrites two Design regions and leaves the rest of the tree untouched. The goal restatement still matches the groomed task (Design only; Purpose already landed in 580). Recorded build, test, and audit results in `## Results` cover this comment-only delta. M1 matches `AssignmentFallback` and the Add path in `telemetry-options-extractor.cs`. M2's split matches `CompiledRoute.MessageType`, `MapMessageTypeToKind`, and `NuruMcpToolCatalog.CreateAnnotations`. The new MessageType region also says help shows Unspecified as `( )`. Generated help prints command and description only.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-parsing/message-type.cs:8
- Description: The revised Design says help shows Unspecified as `( )`. `HelpEmitter.EmitCommands` writes a Command/Description table and does not read `MessageType`. `RouteHelpEmitter.EmitRouteHelpContent` writes the pattern, description, parameters, options, and examples, and does not read `MessageType` either. The `( )` sentence exists only in the Unspecified XML remarks. A reader who trusts this region would treat the missing marker as a defect.
- Suggestion: Drop the help clause. State that help lists the command and its description and does not render a message-type marker. Leave the Unspecified XML remarks out of this region.
- Status: open
