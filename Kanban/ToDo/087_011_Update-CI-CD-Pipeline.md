# Update CI CD Pipeline

## Description

Update the CI/CD pipeline configuration for the new directory and file structure.

## Parent

087_Migrate-Repository-To-Kebab-Case-Naming

## Requirements

- Update `.github/workflows/ci-cd.yml` with new paths
- Update any hardcoded directory references
- Update solution file reference
- Verify pipeline runs successfully

## Checklist

- [ ] Audit ci-cd.yml for hardcoded paths
- [ ] Update solution file reference
- [ ] Update any project-specific paths
- [ ] Update any script references
- [ ] Push and verify CI passes
- [ ] Verify all workflow jobs succeed

## Notes

- Final validation step
- May need to run pipeline multiple times to catch all issues
- Consider running on a test branch first
