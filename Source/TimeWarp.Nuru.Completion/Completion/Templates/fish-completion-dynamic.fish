# Dynamic Fish completion for {{APP_NAME}}
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

# Fish completion is simpler - we just call the app and let it return candidates
complete -c {{APP_NAME}} -f -a '({{APP_NAME}} __complete (count (commandline -opc)) (commandline -opc) 2>/dev/null | string match -v -r "^:")'

# The string match -v -r "^:" filters out directive lines (starting with :)
