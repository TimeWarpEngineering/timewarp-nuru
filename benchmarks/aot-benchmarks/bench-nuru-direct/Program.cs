// AOT Benchmark: TimeWarp.Nuru (Direct/Empty Builder)
// Minimal overhead - no DI, no configuration
using TimeWarp.Nuru;

NuruCoreApp app = NuruCoreApp.CreateEmptyBuilder(args)
    .Map("--str {str} -i {i:int} -b")
    .WithHandler((string str, int i) => { })
    .AsQuery()
    .Done()
    .Build();

return await app.RunAsync(args);
