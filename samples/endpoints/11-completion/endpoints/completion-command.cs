using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("completion", Description = "Generate shell completion scripts")]
public sealed class CompletionCommand : ICommand<Unit>
{
  [Parameter(Description = "Shell: bash, zsh, ps, fish")]
  public string Shell { get; set; } = "";

  public sealed class Handler : ICommandHandler<CompletionCommand, Unit>
  {
    public ValueTask<Unit> Handle(CompletionCommand c, CancellationToken ct)
    {
      string script = c.Shell.ToLower() switch
      {
        "bash" => GenerateBashCompletion(),
        "zsh" => GenerateZshCompletion(),
        "ps" or "powershell" => GeneratePowerShellCompletion(),
        "fish" => GenerateFishCompletion(),
        _ => throw new ArgumentException($"Unknown shell: {c.Shell}")
      };

      WriteLine(script);
      return default;
    }

    private static string GenerateBashCompletion() => """
      _nuru_completions() {
          local cur prev opts
          COMPREPLY=()
          cur="${COMP_WORDS[COMP_CWORD]}"
          prev="${COMP_WORDS[COMP_CWORD-1]}"
          opts="deploy config logs status completion --help --version -h -v"

          if [[ ${cur} == -* ]] ; then
              COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
              return 0
          fi

          case "${prev}" in
              deploy)
                  COMPREPLY=( $(compgen -W "dev staging prod" -- ${cur}) )
                  ;;
              config)
                  COMPREPLY=( $(compgen -W "get set list" -- ${cur}) )
                  ;;
              logs)
                  COMPREPLY=( $(compgen -W "show tail clear" -- ${cur}) )
                  ;;
              *)
                  COMPREPLY=( $(compgen -W "${opts}" -- ${cur}) )
                  ;;
          esac
      }
      complete -F _nuru_completions completion
      """;

    private static string GenerateZshCompletion() => """
      #compdef completion

      _nuru() {
          local curcontext="$curcontext" state line
          typeset -A opt_args

          _arguments -C \
          '1: :->command' \
          '*: :->args' && ret=0

          case "$state" in
              command)
                  _values 'commands' \
                      'deploy[Deploy application]' \
                      'config[Manage configuration]' \
                      'logs[View logs]' \
                      'status[Show status]' \
                      'completion[Generate completions]'
                  ;;
              args)
                  case "$line[1]" in
                      deploy)
                          _values 'environments' 'dev' 'staging' 'prod'
                          ;;
                      config)
                          _values 'actions' 'get' 'set' 'list'
                          ;;
                  esac
                  ;;
          esac
      }

      _nuru "$@"
      """;

    private static string GeneratePowerShellCompletion() => """
      Register-ArgumentCompleter -Native -CommandName completion -ScriptBlock {
          param($wordToComplete, $commandAst, $cursorPosition)

          $commands = @('deploy', 'config', 'logs', 'status', 'completion')
          $environments = @('dev', 'staging', 'prod')

          if ($wordToComplete -match '^-') {
              @('--help', '--version', '--force', '--verbose', '-h', '-v', '-f') | ForEach-Object {
                  [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
              }
          }
          else {
              $commands | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
                  [System.Management.Automation.CompletionResult]::new($_, $_, 'Command', $_)
              }
          }
      }
      """;

    private static string GenerateFishCompletion() => """
      complete -c completion -f

      complete -c completion -n "__fish_use_subcommand" -a "deploy" -d "Deploy application"
      complete -c completion -n "__fish_use_subcommand" -a "config" -d "Manage configuration"
      complete -c completion -n "__fish_use_subcommand" -a "logs" -d "View logs"
      complete -c completion -n "__fish_use_subcommand" -a "status" -d "Show status"
      complete -c completion -n "__fish_use_subcommand" -a "completion" -d "Generate completions"

      complete -c completion -n "__fish_seen_subcommand_from deploy" -a "dev staging prod"
      complete -c completion -n "__fish_seen_subcommand_from config" -a "get set list"
      complete -c completion -n "__fish_seen_subcommand_from logs" -a "show tail clear"

      complete -c completion -l help -s h -d "Show help"
      complete -c completion -l version -s v -d "Show version"
      complete -c completion -l force -s f -d "Force operation"
      complete -c completion -l verbose -d "Verbose output"
      """;
  }
}
