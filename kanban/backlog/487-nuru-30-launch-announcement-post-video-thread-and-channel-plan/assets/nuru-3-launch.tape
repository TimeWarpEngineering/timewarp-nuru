Output nuru-3.mp4
Set Shell "bash"
Set FontSize 24
Set Width 1600
Set Height 900
Set Padding 28
Set Theme "Catppuccin Mocha"
Set TypingSpeed 35ms
Set CursorBlink false

Hide
Type `cd <demo-dir> && export PATH=$PWD:$PATH && source /usr/share/bash-completion/bash_completion && source <(app --generate-completion bash) && export PS1='$ ' && clear` Enter
Show

Type "# TimeWarp.Nuru 3.0 — a CLI in one file" Enter
Sleep 1.2s
Type@6ms "cat > app.cs <<'CS'" Enter
Type@6ms "#!/usr/bin/env -S dotnet --" Enter
Type@6ms "#:package TimeWarp.Nuru@3.0.0-beta.79" Enter
Type@6ms "using TimeWarp.Nuru;" Enter
Enter
Type@6ms "NuruApp app = NuruApp.CreateBuilder()" Enter
Type@6ms `  .Map("greet {name}").WithHandler((string name) => $"Hello, {name}!").AsQuery().Done()` Enter
Type@6ms `  .Map("deploy {env} --tag? {tag?}")` Enter
Type@6ms `    .WithHandler((string env, string? tag) => $"Deploying {tag ?? "latest"} to {env}")` Enter
Type@6ms "    .AsCommand().Done()" Enter
Type@6ms "  .EnableCompletion()" Enter
Type@6ms "  .AddRepl()" Enter
Type@6ms "  .Build();" Enter
Enter
Type@6ms "return await app.RunAsync(args);" Enter
Type@6ms "CS" Enter
Hide
Type `cp app.full.cs app.cs; printf "\033[1A\033[2K"` Enter
Show
Sleep 1.5s
Type "dotnet run app.cs -- greet World" Enter
Wait+Screen@30s /Hello, World!/
Sleep 1.5s

Type "# Shell completion, generated from your routes" Enter
Type "app dep" Tab
Sleep 700ms
Type "staging --t" Tab
Sleep 700ms
Type "3.0.0" Enter
Sleep 1.5s

Type "# Built-in REPL" Enter
Type "app --interactive" Enter
Sleep 1s
Type "greet Nuru" Enter
Sleep 1s
Type "deploy prod --tag 3.0.0" Enter
Sleep 1.2s
Type "exit" Enter
Sleep 800ms
Type "clear" Enter

Type "# Your agent reads the contract..." Enter
Type "app --capabilities | tail -14" Enter
Sleep 3.5s
Type "# ...and calls your CLI. No shell quoting. No MCP server." Enter
Type `app deploy --json-args '{"env":"staging","tag":"3.0.0"}'` Enter
Sleep 2.5s

Type "# Native AOT" Enter
Type `ls -lh app | awk '{print $5, $9}'` Enter
Sleep 1s
Type "time app greet x" Enter
Sleep 2.5s
Type "clear" Enter
Type "echo; echo '   TimeWarp.Nuru 3.0'; echo; echo '   github.com/TimeWarpEngineering/timewarp-nuru'; echo; echo '   ⭐ if useful'" Enter
Sleep 4s
