# Dynamic PowerShell completion for {{APP_NAME}}
# Generated with absolute path: {{APP_PATH}}

Register-ArgumentCompleter -Native -CommandName {{APP_NAME}} -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    $words = $commandAst.ToString() -split ' '
    $position = if ($wordToComplete -ne '') { $words.Count - 1 } else { $words.Count }
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = "{{APP_PATH}}"
    $psi.Arguments = "__complete $position $($words -join ' ')"
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    try {
        $proc = [System.Diagnostics.Process]::Start($psi)
        $output = $proc.StandardOutput.ReadToEnd()
        $proc.WaitForExit()
        $completions = $output -split "`n" | Where-Object { $_ -ne '' }
    } catch {
        $completions = @()
    }

    # Parse completions and extract directive
    # Use ArrayList for O(1) append instead of array concatenation O(n)
    $results = [System.Collections.ArrayList]::new()

    foreach ($line in $completions) {
        # Skip directive line (starts with :) and exit code line (standalone number)
        if ($line -match '^:' -or $line -match '^\d+$') {
            continue
        }

        # Parse value and description (tab-separated)
        $parts = $line -split "`t", 2
        if ($parts.Count -eq 2) {
            $value = $parts[0]
            $desc = $parts[1]
        } else {
            $value = $line
            $desc = $line
        }

        # Filter by wordToComplete prefix (PowerShell doesn't filter automatically like bash/zsh/fish)
        if (-not $wordToComplete -or $value -like "$wordToComplete*") {
            [void]$results.Add([System.Management.Automation.CompletionResult]::new(
                $value,
                $value,
                [System.Management.Automation.CompletionResultType]::ParameterValue,
                $desc
            ))
        }
    }

    # Return results
    $results
}
