Output shot3.gif
Set Shell "bash"
Set FontSize 26
Set FontFamily "DejaVu Sans Mono"
Set Width 1600
Set Height 900
Set Padding 28
Set Theme "Catppuccin Mocha"
Set TypingSpeed 8ms
Set CursorBlink false

Hide
Type `cd /tmp/nuru-487-demo-official && export PATH="$PWD:$PATH" && export PS1='$ ' && clear` Enter
Show
Type `./app deploy --json-args '{"env":"staging","tag":"3.0.0"}'` Enter
Sleep 700ms
Type `./app deploy --json-args '{"env":"x","bogus":1}'` Enter
Sleep 700ms
Sleep 1.2s
