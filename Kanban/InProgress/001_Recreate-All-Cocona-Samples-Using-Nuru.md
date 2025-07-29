# Recreate All Cocona Samples Using Nuru

## Description

Parent task to track the recreation of all 29 Cocona sample applications using Nuru, including comprehensive comparison documentation for each sample. This effort will demonstrate Nuru's capabilities as a modern CLI framework alternative to Cocona and provide valuable learning resources for developers migrating from Cocona to Nuru.

## Requirements

- Recreate all 29 Cocona samples found in the Cocona repository
- Create an Overview.md for each sample that contrasts the differences between Cocona and Nuru implementations
- Ensure each Nuru implementation maintains functional parity with the original Cocona sample
- Document architectural and API differences between the two frameworks

## Checklist

### Getting Started Samples (3 tasks)
- [x] 001_001_Recreate-GettingStarted-MinimalApp-with-Nuru
- [x] 001_002_Recreate-GettingStarted-SubCommandApp-with-Nuru
- [ ] 001_003_Recreate-GettingStarted-TypicalSimpleApp-with-Nuru

### In Action Samples (11 tasks)
- [ ] 001_004_Recreate-InAction-AppConfiguration-with-Nuru
- [ ] 001_005_Recreate-InAction-CommandFilter-with-Nuru
- [ ] 001_006_Recreate-InAction-CommandOptionOverload-with-Nuru
- [ ] 001_007_Recreate-InAction-CommandOptions-with-Nuru
- [ ] 001_008_Recreate-InAction-DependencyInjection-with-Nuru
- [ ] 001_009_Recreate-InAction-ExitCode-with-Nuru
- [ ] 001_010_Recreate-InAction-HandleShutdownSignal-with-Nuru
- [ ] 001_011_Recreate-InAction-ManyArguments-with-Nuru
- [ ] 001_012_Recreate-InAction-MultipleCommandTypes-with-Nuru
- [ ] 001_013_Recreate-InAction-ParameterSet-with-Nuru
- [ ] 001_014_Recreate-InAction-Validation-with-Nuru

### Advanced Samples (10 tasks)
- [ ] 001_015_Recreate-Advanced-CommandMethodForwarding-with-Nuru
- [ ] 001_016_Recreate-Advanced-GenericHost-with-Nuru
- [ ] 001_017_Recreate-Advanced-GenericHost-HostApplicationBuilder-with-Nuru
- [ ] 001_018_Recreate-Advanced-HelpOnDemand-with-Nuru
- [ ] 001_019_Recreate-Advanced-HelpTransformer-with-Nuru
- [ ] 001_020_Recreate-Advanced-JsonValueConverter-with-Nuru
- [ ] 001_021_Recreate-Advanced-Localization-with-Nuru
- [ ] 001_022_Recreate-Advanced-OptionLikeCommand-with-Nuru
- [ ] 001_023_Recreate-Advanced-PreventMultipleInstances-with-Nuru
- [ ] 001_024_Recreate-Advanced-ShellCompletionCandidates-with-Nuru

### Minimal API Samples (4 tasks)
- [ ] 001_025_Recreate-MinimalApi-QuickStart-with-Nuru
- [ ] 001_026_Recreate-MinimalApi-InAction-with-Nuru
- [ ] 001_027_Recreate-MinimalApi-MultipleCommands-with-Nuru
- [ ] 001_028_Recreate-MinimalApi-SubCommand-with-Nuru

### Summary Task
- [ ] 001_029_Create-Comprehensive-Cocona-to-Nuru-Comparison-Document

## Notes

- Each child task should result in a working Nuru sample that demonstrates equivalent functionality to the original Cocona sample
- Overview.md files should highlight key differences in:
  - API design and usage
  - Attribute/annotation differences
  - Configuration approaches
  - Dependency injection patterns
  - Command organization
  - Help system customization
  - Performance characteristics
- The final comparison document (task 001_029) will aggregate findings from all samples into a comprehensive migration guide