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

    # Apply directive-based filtering.
    # Prefix-filter into COMPREPLY without unquoted $(compgen -W ...), which joins on
    # IFS and re-splits — corrupting spaced/glob candidates (M19 / 470-013).
    if (( !(directive & 4) )) && [[ ${#suggestions[@]} -eq 0 ]]; then
        # No suggestions and file completion allowed: fall back to files
        _filedir
    else
        COMPREPLY=()
        local s
        for s in "${suggestions[@]}"; do
            if [[ "$s" == "$cur"* ]]; then
                COMPREPLY+=("$s")
            fi
        done
    fi

    return 0
}

complete -F _{{APP_NAME}}_completions {{APP_NAME}}
