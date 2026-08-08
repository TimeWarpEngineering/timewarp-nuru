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
    # Strip only the directive line (starts with ':'). Do NOT also strip standalone
    # numbers — no exit-code line is printed to stdout, so that would drop a legitimate
    # "0" completion candidate.
    {{APP_NAME}} __complete $index $words 2>/dev/null | string match -v -r '^:'
end

complete -c {{APP_NAME}} -f -a '(__fish_{{APP_NAME}}_complete)'
