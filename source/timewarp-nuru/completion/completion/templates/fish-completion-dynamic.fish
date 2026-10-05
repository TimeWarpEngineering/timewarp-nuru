# Dynamic Fish completion for {{APP_NAME}}
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

function __fish_{{APP_NAME}}_complete
    set -l words (commandline -opc)
    set -l current (commandline -ct)
    set -l index
    # If there's a partial word being typed, add it to the words list
    if test -n "$current"
        set words $words $current
        set index (math (count $words) - 1)
    else
        # Cursor is after a space, completing the next word
        set index (count $words)
    end
    # Print candidates and capture only the directive line (starts with ':'). Do NOT also
    # strip standalone numbers — no exit-code line is printed to stdout, so that would drop
    # a legitimate "0" completion candidate. Command substitution splits on newlines only,
    # so a candidate that contains spaces stays one candidate.
    set -l directive 0
    for line in ({{APP_NAME}} __complete $index $words 2>/dev/null)
        if string match -q -r '^:' -- $line
            set directive (string sub -s 2 -- $line)
        else
            printf '%s\n' $line
        end
    end
    # Directive bits (CompletionDirective): 4=NoFileComp 16=FilterDirs. NoSpace and KeepOrder
    # have no per-call equivalent in fish completions.
    if test (math "bitand($directive, 4)") -eq 0
        if test (math "bitand($directive, 16)") -ne 0
            __fish_complete_directories $current
        else
            __fish_complete_path $current
        end
    end
end

complete -c {{APP_NAME}} -f -a '(__fish_{{APP_NAME}}_complete)'
