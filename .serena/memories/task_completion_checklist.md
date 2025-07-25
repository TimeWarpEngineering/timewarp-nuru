# Task Completion Checklist

When you complete a coding task in the TimeWarp.Nuru project, ensure you:

## 1. Build Verification
- Run `dotnet build` to ensure the code compiles without errors
- Check that no new analyzer warnings are introduced

## 2. Testing
- If modifying existing functionality, run the integration tests:
  ```bash
  cd Samples/TimeWarp.Nuru.IntegrationTests
  ./test-all.sh
  ```
- If adding new routes or commands, consider adding test cases to the integration test sample

## 3. Code Quality
- Ensure code follows the established patterns and conventions
- Add XML documentation comments for new public APIs
- Use required properties for mandatory data
- Follow the existing namespace and folder structure

## 4. Format Code
- Run `dotnet format` if you've made significant changes

## 5. Version Considerations
- If preparing for release, ensure the version in TimeWarp.Nuru.csproj is updated
- Version must be unique for NuGet publishing (checked by CI/CD)

## Important Notes
- Do NOT commit unless explicitly asked by the user
- The main branch is `master` (not `main`)
- CI/CD runs on push to master and on pull requests
- NuGet publishing happens automatically on GitHub release creation