# Dynamic PowerShell completion for {{APP_NAME}}
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

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

    # Call application for dynamic completions
    # Format: {{APP_NAME}} __complete <cursor_index> <word1> <word2> ...
    $completions = & {{APP_NAME}} __complete $position $words 2>$null

    # Parse completions and extract directive
    $directive = 0
    $results = @()

    $completions | ForEach-Object {
        # Check if this is a directive line (starts with :)
        if ($_ -match '^:(\d+)$') {
            $directive = [int]$matches[1]
            return
        }

        # Skip exit code line (standalone number)
        if ($_ -match '^\d+$') {
            return
        }

        # Parse value and description (tab-separated)
        if ($_ -match '^(.+?)\t(.+)$') {
            $value = $matches[1]
            $desc = $matches[2]
        } else {
            $value = $_
            $desc = $_
        }

        # Add to results
        $results += [System.Management.Automation.CompletionResult]::new(
            $value,
            $value,
            [System.Management.Automation.CompletionResultType]::ParameterValue,
            $desc
        )
    }

    # Return results (PowerShell will fall back to file completion if empty,
    # but that's acceptable behavior when no completions are available)
    $results
}
