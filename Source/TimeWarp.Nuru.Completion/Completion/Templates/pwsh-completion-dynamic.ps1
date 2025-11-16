# Dynamic PowerShell completion for {{APP_NAME}}
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

# Cache the resolved path at registration time to avoid expensive PATH lookups on each Tab press
# Use a hashtable to store paths by command name (handles hyphens and special chars)
if (-not $script:__NuruCompletionPaths) {
    $script:__NuruCompletionPaths = @{}
}
$script:__NuruCompletionPaths['{{APP_NAME}}'] = (Get-Command {{APP_NAME}} -ErrorAction SilentlyContinue).Source
if (-not $script:__NuruCompletionPaths['{{APP_NAME}}']) {
    $script:__NuruCompletionPaths['{{APP_NAME}}'] = "{{APP_NAME}}"
}

Register-ArgumentCompleter -Native -CommandName {{APP_NAME}} -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)

    # Parse command line into words
    $words = $commandAst.ToString() -split ' '

    # Calculate cursor position (0-based index of word being completed)
    # If completing a partial word, position is the index of that word (Count - 1)
    # If cursor is after a space (completing next word), position is Count
    if ($wordToComplete -ne '') {
        $position = $words.Count - 1
    } else {
        $position = $words.Count
    }

    # Call application for dynamic completions using cached absolute path
    # Format: {{APP_NAME}} __complete <cursor_index> <word1> <word2> ...
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $script:__NuruCompletionPaths['{{APP_NAME}}']
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

        # Add to results (ArrayList.Add returns index, suppress with [void])
        [void]$results.Add([System.Management.Automation.CompletionResult]::new(
            $value,
            $value,
            [System.Management.Automation.CompletionResultType]::ParameterValue,
            $desc
        ))
    }

    # Return results
    $results
}
