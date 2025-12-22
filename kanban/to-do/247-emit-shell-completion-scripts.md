# Emit shell completion scripts

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Generate shell completion scripts (bash, zsh, fish, PowerShell) as compile-time string constants. The scripts are deterministic based on the route patterns.

## Requirements

- Emit completion scripts for all supported shells
- Scripts match current runtime-generated output
- Include command names, options, parameter hints
- Support `--completion` flag to output scripts

## Checklist

- [ ] Analyze current completion script generation
- [ ] Port bash completion generation to source generator
- [ ] Port zsh completion generation
- [ ] Port fish completion generation
- [ ] Port PowerShell completion generation
- [ ] Emit `GeneratedCompletionScripts` class
- [ ] Wire `--completion` handler to use generated scripts
- [ ] Test completions work in each shell

## Notes

### Generated Output Example

```csharp
internal static class GeneratedCompletionScripts
{
    internal const string Bash = """
        _calculator_completions() {
            local cur="${COMP_WORDS[COMP_CWORD]}"
            local commands="add subtract multiply divide help"
            
            if [[ ${COMP_CWORD} -eq 1 ]]; then
                COMPREPLY=($(compgen -W "${commands}" -- "${cur}"))
            fi
        }
        complete -F _calculator_completions calculator
        """;
    
    internal const string Zsh = """
        #compdef calculator
        _calculator() {
            local -a commands
            commands=(
                'add:Adds two numbers'
                'subtract:Subtracts b from a'
            )
            _describe 'command' commands
        }
        """;
    
    internal const string Fish = """
        complete -c calculator -n "__fish_use_subcommand" -a add -d "Adds two numbers"
        complete -c calculator -n "__fish_use_subcommand" -a subtract -d "Subtracts b from a"
        """;
    
    internal const string PowerShell = """
        Register-ArgumentCompleter -Native -CommandName calculator -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)
            @('add', 'subtract', 'multiply') | Where-Object { $_ -like "$wordToComplete*" }
        }
        """;
}
```

### Integration

```csharp
// In generated app
if (args is ["--completion", var shell])
{
    Console.Write(shell switch
    {
        "bash" => GeneratedCompletionScripts.Bash,
        "zsh" => GeneratedCompletionScripts.Zsh,
        "fish" => GeneratedCompletionScripts.Fish,
        "powershell" => GeneratedCompletionScripts.PowerShell,
        _ => ""
    });
    return 0;
}
```
