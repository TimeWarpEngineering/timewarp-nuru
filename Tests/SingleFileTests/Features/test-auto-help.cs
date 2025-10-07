#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Test auto-help with multiple route variations and descriptions
NuruApp app =
  new NuruAppBuilder()
    .AddRoute
    (
      "deploy {env|Target environment (dev, staging, prod)}"
      , (string env) => WriteLine($"Deploying to {env}")
      , "Deploy to environment"
    )
    .AddRoute
    (
      "deploy {env|Target environment} --dry-run,-d|Preview changes without deploying"
      , (string env) => WriteLine($"Dry run deploy to {env}")
      , "Deploy with dry run"
    )
    .AddRoute
    (
      "deploy {env|Target environment} --force,-f|Skip confirmation prompts"
      , (string env) => WriteLine($"Force deploy to {env}")
      , "Deploy with force"
    )
    .AddRoute
    (
      "deploy {env|Target environment} --version|Deploy specific version {ver|Version to deploy}",
      (string env, string ver) => WriteLine($"Deploy version {ver} to {env}"),
      "Deploy specific version"
    )
    .AddAutoHelp()
    .Build();

return await app.RunAsync(args);