// AOT Benchmark: TimeWarp.Nuru (Full Builder)
// Full DI/configuration support with Mediator source generator
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;

NuruAppBuilder builder = NuruApp.CreateBuilder(args);
builder.Services.AddMediator();

builder.Map("--str {str} -i {i:int} -b")
    .WithHandler((string str, int i) => { })
    .AsQuery()
    .Done();

NuruCoreApp app = builder.Build();
return await app.RunAsync(args);
