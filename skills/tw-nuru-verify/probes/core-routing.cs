// Probe: the README surface. Map, WithHandler, AsQuery/AsCommand/AsIdempotentCommand, typed,
// optional, option and catch-all parameters, WithName/WithDescription, exit codes.
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .WithName("probe")
  .WithDescription("Nuru consumer probe")
  .Map("add {x:int} {y:int}")
    .WithDescription("Add two integers")
    .WithHandler((int x, int y) => $"sum={x + y}")
    .AsQuery()
    .Done()
  .Map("deploy {env} {tag?} --force,-f")
    .WithDescription("Deploy with an optional tag")
    .WithHandler((string env, string? tag, bool force) => $"env={env} tag={tag ?? "none"} force={force}")
    .AsCommand()
    .Done()
  .Map("set {key} {value}")
    .WithHandler((string key, string value) => $"set {key}={value}")
    .AsIdempotentCommand()
    .Done()
  .Map("docker {*args}")
    .WithHandler((string[] args) => $"docker args={string.Join(',', args)}")
    .AsCommand()
    .Done()
  .Map("fail")
    .WithHandler(() =>
    {
      Environment.ExitCode = 3;
      return "failed";
    })
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);
