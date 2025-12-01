# Create Mediator Dependency Analyzer (NURU_D001, NURU_D002)

## Description

Create a new analyzer that detects `Map<TCommand>` usage without proper Mediator packages configured, providing actionable error messages to developers.

## Requirements

- NURU_D001: Error when Mediator.Abstractions not referenced
- NURU_D002: Warning when services.AddMediator() not called
- Implementation plan in `.agent/workspace/2025-12-01T20-45-00_sample-standardization-and-analyzer-plan.md`

## Checklist

### Design
- [ ] Define diagnostic descriptors for NURU_D001 and NURU_D002
- [ ] Design detection logic for Map<TCommand> usage
- [ ] Plan assembly reference checking approach
- [ ] Plan AddMediator() call detection approach

### Implementation
- [ ] Create analyzer class in timewarp-nuru-analyzers project
- [ ] Implement NURU_D001: Missing Mediator.Abstractions reference detection
- [ ] Implement NURU_D002: Missing AddMediator() call detection
- [ ] Add code fix provider to suggest adding packages
- [ ] Add code fix provider to suggest adding AddMediator()

### Testing
- [ ] Add unit tests for NURU_D001 scenarios
- [ ] Add unit tests for NURU_D002 scenarios
- [ ] Test with real-world sample projects
- [ ] Verify no false positives on delegate-only projects

### Documentation
- [ ] Document analyzer rules in developer reference

## Notes

Tags: analyzer, mediator, developer-experience

Diagnostic details:
- NURU_D001 (Error): "Map<TCommand> requires Mediator.Abstractions package. Add PackageReference to Mediator.Abstractions."
- NURU_D002 (Warning): "Map<TCommand> requires Mediator registration. Call services.AddMediator() in your builder configuration."
