#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - SHELL TAB COMPLETION ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Static shell tab completion for Bash, Zsh, PowerShell, and Fish.
//
// DSL: Endpoint with completion script generation
//
// USAGE:
//   ./endpoint-completion.cs completion bash  # Generate bash completions
//   ./endpoint-completion.cs completion zsh   # Generate zsh completions
//   ./endpoint-completion.cs completion ps   # Generate PowerShell completions
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("deploy", Description = "Deploy application to environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter(Description = "Target environment")]
  public string Env { get; set; } = "";

  [Option("version", "v", Description = "Version to deploy")]
  public string? Version { get; set; }

  [Option("force", "f", Description = "Force deployment")]
  public bool Force { get; set; }

  public sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployCommand c, CancellationToken ct)
    {
      WriteLine($"Deploying to {c.Env}");
      if (c.Version != null)
        WriteLine($"  Version: {c.Version}");
      WriteLine($"  Force: {c.Force}");
      return default;
    }
  }
}

[NuruRoute("config", Description = "Manage configuration")]
public sealed class ConfigCommand : ICommand<Unit>
{
  [Parameter(Description = "Action: get, set, list")]
  public string Action { get; set; } = "";

  [Parameter(Description = "Configuration key")]
  public string Key { get; set; } = "";

  [Parameter(Description = "Configuration value (for set)")]
  public string? Value { get; set; }

  public sealed class Handler : ICommandHandler<ConfigCommand, Unit>
  {
    public ValueTask<Unit> Handle(ConfigCommand c, CancellationToken ct)
    {
      WriteLine($"Config {c.Action}: {c.Key} = {c.Value ?? "(null)"}");
      return default;
    }
  }
}

[NuruRoute("logs", Description = "View and manage logs")]
public sealed class LogsCommand : ICommand<Unit>
{
  [Parameter(Description = "Action: show, tail, clear")]
  public string Action { get; set; } = "";

  [Option("lines", "n", Description = "Number of lines to show")]
  public int Lines { get; set; } = 50;

  [Option("follow", "f", Description = "Follow log output")]
  public bool Follow { get; set; }

  public sealed class Handler : ICommandHandler<LogsCommand, Unit>
  {
    public ValueTask<Unit> Handle(LogsCommand c, CancellationToken ct)
    {
      WriteLine($"Logs {c.Action}");
      WriteLine($"  Lines: {c.Lines}");
      WriteLine($"  Follow: {c.Follow}");
      return default;
    }
  }
}

[NuruRoute("status", Description = "Show system status")]
public sealed class StatusQuery : IQuery<Unit>
{
  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : IQueryHandler<StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(StatusQuery q, CancellationToken ct)
    {
      WriteLine("System Status: OK");
      if (q.Verbose)
      {
        WriteLine("  CPU: 45%");
        WriteLine("  Memory: 2.1GB");
        WriteLine("  Disk: 78%");
      }
      return default;
    }
  }
}

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
      complete -F _nuru_completions endpoint-completion
      """;

    private static string GenerateZshCompletion() => """
      #compdef endpoint-completion

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
      Register-ArgumentCompleter -Native -CommandName endpoint-completion -ScriptBlock {
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
      complete -c endpoint-completion -f

      complete -c endpoint-completion -n "__fish_use_subcommand" -a "deploy" -d "Deploy application"
      complete -c endpoint-completion -n "__fish_use_subcommand" -a "config" -d "Manage configuration"
      complete -c endpoint-completion -n "__fish_use_subcommand" -a "logs" -d "View logs"
      complete -c endpoint-completion -n "__fish_use_subcommand" -a "status" -d "Show status"
      complete -c endpoint-completion -n "__fish_use_subcommand" -a "completion" -d "Generate completions"

      complete -c endpoint-completion -n "__fish_seen_subcommand_from deploy" -a "dev staging prod"
      complete -c endpoint-completion -n "__fish_seen_subcommand_from config" -a "get set list"
      complete -c endpoint-completion -n "__fish_seen_subcommand_from logs" -a "show tail clear"

      complete -c endpoint-completion -l help -s h -d "Show help"
      complete -c endpoint-completion -l version -s v -d "Show version"
      complete -c endpoint-completion -l force -s f -d "Force operation"
      complete -c endpoint-completion -l verbose -d "Verbose output"
      """;
  }
}
