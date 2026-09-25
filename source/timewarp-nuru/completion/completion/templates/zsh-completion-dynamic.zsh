#compdef {{APP_NAME}}

# Dynamic Zsh completion for {{APP_NAME}}
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

_{{APP_NAME}}() {
    local line state
    local -a completions

    # Call application for dynamic completions
    # Format: {{APP_NAME}} __complete <cursor_index> <word1> <word2> ...
    # Note: Convert zsh's 1-based $CURRENT to 0-based index for C# handler
    local output
    output="$({{APP_NAME}} __complete $((CURRENT - 1)) "${words[@]}" 2>/dev/null)"
    completions=(${(f)output})

    # Strip only the directive line (starts with :). Do NOT also strip a trailing
    # bare numeric line — DynamicCompletionHandler never emits an exit-code line on
    # stdout, so that filter would drop a legitimate numeric candidate (e.g. "0").
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

compdef _{{APP_NAME}} {{APP_NAME}}
