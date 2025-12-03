# AOT Testing and Verification

## Description

Verify AOT compilation works without warnings and test performance. This is the validation step that confirms the source generator implementation achieves its AOT compatibility goals.

## Parent

008-implement-source-generators-for-reflection-free-routing

## Dependencies

- 008c-wire-invokers-to-delegate-executor

## Requirements

- Build test app with PublishAot=true
- Verify no IL2XXX or IL3XXX trimming warnings
- Verify no DynamicInvoke calls remain for generated routes
- Benchmark performance vs reflection-based invocation
- Document AOT compatibility status

## Checklist

### AOT Compilation Verification
- [ ] Create AOT test application with representative routes
- [ ] Build with `<PublishAot>true</PublishAot>`
- [ ] Capture and analyze all trimming warnings
- [ ] Verify zero IL2XXX warnings (trimming)
- [ ] Verify zero IL3XXX warnings (AOT)
- [ ] Test application runs correctly after AOT publish

### DynamicInvoke Verification
- [ ] Add compile-time check that generated invokers exist
- [ ] Verify DynamicInvoke only used for explicit fallback scenarios
- [ ] Consider analyzer warning when DynamicInvoke fallback is triggered

### Performance Benchmarking
- [ ] Create benchmark comparing generated vs DynamicInvoke
- [ ] Measure invocation latency
- [ ] Measure memory allocations
- [ ] Document performance improvement percentage
- [ ] Add results to benchmarks/ directory

### Documentation
- [ ] Update AOT compatibility documentation
- [ ] Document any limitations or edge cases
- [ ] Add migration guide for existing users
- [ ] Update README with AOT status

## Notes

Expected performance improvement: DynamicInvoke is known to be 10-100x slower than direct invocation due to reflection overhead.

Trimming warning categories:
- IL2XXX: Trimming warnings (code might be trimmed that's needed)
- IL3XXX: AOT warnings (patterns incompatible with AOT)

Test with both:
- `dotnet publish -c Release -p:PublishAot=true`
- `dotnet publish -c Release -p:PublishTrimmed=true`
