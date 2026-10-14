Output shot6.gif
Set Shell "bash"
Set FontSize 32
Set FontFamily "DejaVu Sans Mono"
Set Width 1600
Set Height 900
Set Padding 28
Set Theme "Catppuccin Mocha"
Set TypingSpeed 8ms
Set CursorBlink false

Hide
Type `cd /tmp/nuru-487-clean && export PATH="$PWD:$PATH" && ./app greet x >/dev/null && export PS1='$ ' && clear` Enter
Show
Type "ls -lh app" Enter
Sleep 400ms
Type "time ./app greet x" Enter
Sleep 800ms
Sleep 1s
