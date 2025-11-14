#compdef {{APP_NAME}}

# Dynamic Zsh completion for {{APP_NAME}}
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

_{{APP_NAME}}() {
    local line state
    local -a completions

    # Call application for dynamic completions
    # Format: {{APP_NAME}} __complete <cursor_index> <word1> <word2> ...
    completions=(${(f)"$({{APP_NAME}} __complete $CURRENT "${words[@]}" 2>/dev/null)"})

    # Remove directive line (last line starting with :)
    local directive=0
    if [[ "${completions[-1]}" == :* ]]; then
        directive="${completions[-1]:1}"
        completions=("${(@)completions[1,-2]}")
    fi

    # Format completions with descriptions
    # Zsh supports descriptions with : separator
    local -a formatted
    for completion in "${completions[@]}"; do
        # Split on tab if present (value<tab>description)
        if [[ "$completion" == *$'\t'* ]]; then
            local value="${completion%%$'\t'*}"
            local desc="${completion#*$'\t'}"
            formatted+=("$value:$desc")
        else
            formatted+=("$completion")
        fi
    done

    _describe '{{APP_NAME}}' formatted
}

_{{APP_NAME}} "$@"
