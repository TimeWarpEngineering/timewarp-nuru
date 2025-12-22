# Emit help text and capabilities JSON

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Generate help text and capabilities JSON as compile-time string constants instead of building them at runtime. The generator knows all routes and their metadata.

## Requirements

- Emit pre-formatted help text strings
- Emit capabilities JSON as string constant
- Match current help output format exactly
- Support `--help` for root and per-command

## Checklist

- [ ] Analyze current help text generation logic
- [ ] Port help formatting to source generator
- [ ] Emit `GeneratedHelpText` class with string constants
- [ ] Emit root help (all commands)
- [ ] Emit per-command help
- [ ] Emit capabilities JSON string
- [ ] Wire `--help` handler to use generated strings
- [ ] Verify help output matches runtime version

## Notes

### Generated Output Example

```csharp
internal static class GeneratedHelpText
{
    internal const string RootHelp = """
        calculator v1.0.0
        
        Usage: calculator <command> [options]
        
        Commands:
          add <a> <b>         Adds two numbers
          subtract <a> <b>    Subtracts b from a
          multiply <a> <b>    Multiplies two numbers
        
        Options:
          --help, -h          Show help
          --version           Show version
        """;
    
    internal const string AddHelp = """
        add - Adds two numbers
        
        Usage: calculator add <a> <b>
        
        Parameters:
          a    First number (int)
          b    Second number (int)
        """;
    
    // Per-command help for each route...
    
    internal const string CapabilitiesJson = """
        {
          "commands": [
            {
              "pattern": "add {a:int} {b:int}",
              "description": "Adds two numbers",
              "messageType": "Query"
            }
          ]
        }
        """;
}
```

### Benefits

- Zero runtime string building
- Instant help display
- Capabilities JSON ready for AI tools
