# Dynamic PowerShell completion for {{APP_NAME}}
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

Register-ArgumentCompleter -Native -CommandName {{APP_NAME}} -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)

    # Parse command line into words
    $words = $commandAst.ToString() -split ' '
    $position = $words.Count - 1

    # Adjust position if we're completing a partial word
    if ($wordToComplete -ne '') {
        $position = $words.Count - 1
    }

    # Call application for dynamic completions
    # Format: {{APP_NAME}} __complete <cursor_index> <word1> <word2> ...
    $completions = & {{APP_NAME}} __complete $position $words 2>$null

    # Parse completions (one per line, optional tab-separated description)
    $completions | ForEach-Object {
        # Check if this is a directive line (starts with :)
        if ($_ -match '^:(\d+)$') {
            # Directive line - skip for now
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

        # Create completion result
        [System.Management.Automation.CompletionResult]::new(
            $value,
            $value,
            [System.Management.Automation.CompletionResultType]::ParameterValue,
            $desc
        )
    }
}
