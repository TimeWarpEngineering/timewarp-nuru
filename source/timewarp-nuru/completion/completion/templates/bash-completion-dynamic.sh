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
        [[ -n "$value" ]] && suggestions+=("$value")
    done <<< "$completions"

    # Directive bits (CompletionDirective): 4=NoFileComp 8=NoSpace 16=FilterDirs 64=KeepOrder.
    # Prefix-filter into COMPREPLY without unquoted $(compgen -W ...), which joins on
    # IFS and re-splits — corrupting spaced/glob candidates (M19 / 470-013).
    COMPREPLY=()
    local s
    for s in "${suggestions[@]}"; do
        if [[ "$s" == "$cur"* ]]; then
            COMPREPLY+=("$s")
        fi
    done

    # File/Directory candidates clear NoFileComp: add the shell's own path completion.
    if (( !(directive & 4) )); then
        if (( directive & 16 )); then
            _filedir -d
        else
            _filedir
        fi
    fi

    if (( directive & 8 )); then
        compopt -o nospace 2>/dev/null
    fi
    if (( directive & 64 )); then
        compopt -o nosort 2>/dev/null
    fi

    return 0
}

complete -F _{{APP_NAME}}_completions {{APP_NAME}}
