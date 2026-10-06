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
    # Quoted (@f) split: one array element per output line, so a suggestion that
    # contains spaces stays a single candidate (matches the bash template).
    completions=("${(@f)output}")

    # Strip only the directive line (starts with :). Do NOT also strip a trailing
    # bare numeric line — DynamicCompletionHandler never emits an exit-code line on
    # stdout, so that filter would drop a legitimate numeric candidate (e.g. "0").
    local directive=0
    if [[ "${completions[-1]}" == :* ]]; then
        directive="${completions[-1]:1}"
        completions=("${(@)completions[1,-2]}")
    fi

    # Format completions with descriptions
    # Zsh supports descriptions with : separator, so escape ':' inside the value.
    local -a formatted
    local completion value desc
    for completion in "${completions[@]}"; do
        [[ -z "$completion" ]] && continue
        # Split on tab if present (value<tab>description)
        if [[ "$completion" == *$'\t'* ]]; then
            value="${completion%%$'\t'*}"
            desc="${completion#*$'\t'}"
            formatted+=("${value//:/\\:}:$desc")
        else
            formatted+=("${completion//:/\\:}")
        fi
    done

    # Directive bits (CompletionDirective): 4=NoFileComp 8=NoSpace 16=FilterDirs 64=KeepOrder.
    local -a describe_flags compadd_opts
    (( directive & 64 )) && describe_flags+=(-V)
    (( directive & 8 )) && compadd_opts+=(-S '')

    local ret=1
    if (( ${#formatted} )); then
        _describe "${describe_flags[@]}" '{{APP_NAME}}' formatted "${compadd_opts[@]}" && ret=0
    fi

    # File/Directory candidates clear NoFileComp: add the shell's own path completion.
    if (( !(directive & 4) )); then
        if (( directive & 16 )); then
            _files -/ && ret=0
        else
            _files && ret=0
        fi
    fi

    return $ret
}

compdef _{{APP_NAME}} {{APP_NAME}}
