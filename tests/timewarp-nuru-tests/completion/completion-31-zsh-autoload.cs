#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:package TimeWarp.Amuru

using TimeWarp.Amuru;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.ZshAutoload
{

/// <summary>
/// The generated zsh script must complete on the first Tab when compinit autoloads it
/// from fpath, and must still register with compdef when sourced or eval'd — kanban task 509.
/// </summary>
[TestTag("Completion")]
public class ZshAutoloadTests
{
  private const string AppStub =
    """
    app() {
      print -r -- HIT >> "$HITS"
      print -r -- $'nuru509candidate\tUnique desc\nstatus\n:4'
    }
    """;

  private const string DescribeStub =
    """
    _describe() {
      local name
      for name in "$@"; do
        if [[ ${(tP)name} == *array* ]]; then
          print -r -- CANDIDATES
          print -l -- "${(@P)name}"
          return 0
        fi
      done
      return 1
    }
    _files() { print -r -- FILES_CALLED; return 1; }
    """;

  private const string AutoloadDriver =
    """
    dir=$1
    hits=$dir/hits
    export HITS="$hits"
    : > "$hits"
    fpath=("$dir" $fpath)
    autoload -Uz compinit
    compinit -u -C -d "$dir/zcompdump"
    whence_before=$(whence -v _app)
    [[ "$whence_before" == *autoload* ]] || { print -r -- "not autoload: $whence_before" >&2; exit 2; }
    source "$dir/stub.zsh"
    source "$dir/describe-stub.zsh"
    words=(app '')
    CURRENT=2
    print -r -- BEGIN
    _app
    print -r -- END
    n=$(grep -c HIT "$hits" || true)
    print -r -- "HITS:$n"
    [[ "$n" == 1 ]] || exit 6
    """;

  private const string SourceDriver =
    """
    dir=$1
    script=$dir/_app
    hits=$dir/hits
    export HITS="$hits"
    # A user's zshrc has run compinit before sourcing a completion script. -u skips the
    # insecure-directory prompt, which aborts compinit when there is no terminal (CI).
    autoload -Uz compinit
    compinit -u -C -d "$dir/zcompdump"
    run_registered() {
      local label=$1
      : > "$hits"
      source "$dir/stub.zsh"
      source "$dir/describe-stub.zsh"
      words=(app '')
      CURRENT=2
      print -r -- "BEGIN $label"
      _app
      print -r -- "END $label"
      local n
      n=$(grep -c HIT "$hits" || true)
      print -r -- "HITS:$label:$n"
      [[ "$n" == 1 ]] || exit 6
    }
    (
      source <(cat "$script")
      print -r -- "COMP:${_comps[app]-UNSET}"
      [[ ${_comps[app]} == _app ]] || exit 3
      run_registered SOURCE
    )
    (
      eval "$(cat "$script")"
      print -r -- "COMP:${_comps[app]-UNSET}"
      [[ ${_comps[app]} == _app ]] || exit 4
      run_registered EVAL
    )
    """;

  private const string TabDriver =
    """
    dir=$1
    hits=$dir/hits
    export HITS="$hits"
    : > "$hits"
    zmodload zsh/zpty
    zpty -d nuru509 2>/dev/null || true
    zpty nuru509 zsh -f -i
    acc=""
    wait_for() {
      local pat="$1" limit="${2:-1500}" chunk tries=0
      while (( tries < limit )); do
        if zpty -r -t nuru509 chunk; then
          acc+="$chunk"
          if [[ "$acc" == *"$pat"* ]]; then
            return 0
          fi
        else
          sleep 0.02
          tries=$((tries + 1))
        fi
      done
      print -r -- "TIMEOUT waiting for [$pat]" >&2
      print -r -- "$acc" >&2
      return 1
    }
    zpty -w nuru509 'PS1=__P__\ '
    wait_for '__P__' 200 || exit 2
    zpty -w nuru509 "fpath=('$dir' \$fpath); autoload -Uz compinit && compinit -u -C -d '$dir/zcompdump'; print __COMPINIT_DONE__"
    wait_for '__COMPINIT_DONE__' 1500 || exit 3
    zpty -w nuru509 "source '$dir/stub.zsh'; print __STUB_DONE__"
    wait_for '__STUB_DONE__' 200 || exit 4
    acc=""
    zpty -w -n nuru509 'app '
    zpty -w -n nuru509 $'\t'
    if wait_for 'nuru509candidate' 200; then
      n=$(grep -c HIT "$hits" || true)
      print -r -- "TAB_HITS:$n"
      print -r -- "$acc"
      [[ "$n" == 1 ]] || exit 6
      print -r -- TAB_OK
      zpty -d nuru509
      exit 0
    fi
    n=$(grep -c HIT "$hits" || true)
    print -r -- "TAB_HITS:$n" >&2
    zpty -d nuru509
    exit 5
    """;

  [ModuleInitializer]
  internal static void Register() => RegisterTests<ZshAutoloadTests>();

  public static async Task Zsh_template_calls_the_function_when_autoloaded()
  {
    string script = DynamicCompletionScriptGenerator.GenerateZsh("app");

    int load = script.IndexOf("loadautofunc", StringComparison.Ordinal);
    int call = script.IndexOf("_app \"$@\"", StringComparison.Ordinal);
    int register = script.IndexOf("compdef _app app", StringComparison.Ordinal);
    (load >= 0 && call > load && register > call).ShouldBeTrue(
      "autoload must call _app before compdef registers it"
    );
    script.TrimEnd().EndsWith("compdef _app app", StringComparison.Ordinal).ShouldBeFalse(
      "the file must not end with a bare compdef"
    );

    await Task.CompletedTask;
  }

  public static async Task Should_complete_on_the_first_autoloaded_call()
  {
    (int exitCode, string stdout, string stderr) = await RunDriver(AutoloadDriver);

    exitCode.ShouldBe(0, stderr + stdout);
    stderr.ShouldNotContain("command not found");
    stdout.ShouldContain("BEGIN");
    stdout.ShouldContain("CANDIDATES");
    stdout.ShouldContain("nuru509candidate:Unique desc");
    stdout.ShouldContain("status");
    stdout.ShouldContain("HITS:1");
    stdout.ShouldNotContain("FILES_CALLED");
  }

  public static async Task Should_complete_when_sourced_and_when_evald()
  {
    (int exitCode, string stdout, string stderr) = await RunDriver(SourceDriver);

    exitCode.ShouldBe(0, stderr + stdout);
    stderr.ShouldNotContain("command not found");
    stdout.ShouldContain("COMP:_app");
    stdout.ShouldContain("BEGIN SOURCE");
    stdout.ShouldContain("BEGIN EVAL");
    stdout.ShouldContain("HITS:SOURCE:1");
    stdout.ShouldContain("HITS:EVAL:1");
    stdout.ShouldContain("nuru509candidate:Unique desc");
    stdout.ShouldNotContain("FILES_CALLED");
  }

  public static async Task Should_list_the_candidate_on_the_first_tab()
  {
    (int exitCode, string stdout, string stderr) = await RunDriver(TabDriver);

    exitCode.ShouldBe(0, stderr + stdout);
    stderr.ShouldNotContain("command not found");
    stderr.ShouldNotContain("TIMEOUT");
    stdout.ShouldContain("nuru509candidate");
    stdout.ShouldContain("TAB_HITS:1");
    stdout.ShouldContain("TAB_OK");
  }

  private static async Task<(int ExitCode, string Stdout, string Stderr)> RunDriver(string driver)
  {
    string dir = Path.Combine(Path.GetTempPath(), $"nuru-509-zsh-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    try
    {
      await File.WriteAllTextAsync(Path.Combine(dir, "_app"), DynamicCompletionScriptGenerator.GenerateZsh("app"));
      await File.WriteAllTextAsync(Path.Combine(dir, "stub.zsh"), AppStub);
      await File.WriteAllTextAsync(Path.Combine(dir, "describe-stub.zsh"), DescribeStub);
      string driverPath = Path.Combine(dir, "driver.zsh");
      await File.WriteAllTextAsync(driverPath, driver);

      CommandOutput output = await Shell.Builder("zsh")
        .WithArguments("-f", driverPath, dir)
        .WithEnvironmentVariable("ZDOTDIR", dir)
        .WithNoValidation()
        .CaptureAsync();
      return (output.ExitCode, output.Stdout, output.Stderr);
    }
    finally
    {
      Directory.Delete(dir, recursive: true);
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Completion.ZshAutoload
