# Dynamic Bash completion for {{APP_NAME}}
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

_{{APP_NAME}}_completions()
{
    local cur prev words cword
    _init_completion || return

    # Call application for dynamic completions
    # Format: {{APP_NAME}} __complete <cursor_index> <word1> <word2> ...
    local completions
    completions=$({{APP_NAME}} __complete "$cword" "${words[@]}" 2>/dev/null)

    # Parse completions (one per line, optional tab-separated description)
    local -a suggestions=()
    local directive=0

    while IFS=$'\t' read -r value desc; do
        # Check if this is a directive line (starts with :)
        if [[ "$value" == :* ]]; then
            directive="${value:1}"
            break
        fi
        suggestions+=("$value")
    done <<< "$completions"

    # Apply directive-based filtering
    if (( directive & 4 )); then
        # NoFileComp: Don't fall back to file completion
        COMPREPLY=($(compgen -W "${suggestions[*]}" -- "$cur"))
    else
        # Default: Allow file completion as fallback
        if [[ ${#suggestions[@]} -eq 0 ]]; then
            _filedir
        else
            COMPREPLY=($(compgen -W "${suggestions[*]}" -- "$cur"))
        fi
    fi

    return 0
}

complete -F _{{APP_NAME}}_completions {{APP_NAME}}
