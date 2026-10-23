// Probe: AddRepl, driven through a PTY with -i.
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .AddRepl(options =>
  {
    options.Prompt = "probe> ";
    options.WelcomeMessage = "probe-welcome";
    options.GoodbyeMessage = "probe-goodbye";
  })
  .Map("add {x:int} {y:int}")
    .WithHandler((int x, int y) => $"sum={x + y}")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
