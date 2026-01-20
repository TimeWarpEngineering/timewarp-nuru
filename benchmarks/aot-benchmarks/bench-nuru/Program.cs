using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map("--str {str} -i {i:int} -b")
    .WithHandler((string str, int i) => { })
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(args);
