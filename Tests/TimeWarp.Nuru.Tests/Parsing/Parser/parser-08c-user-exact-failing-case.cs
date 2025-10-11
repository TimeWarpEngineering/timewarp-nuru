#!/usr/bin/dotnet --

// Test user's EXACT pattern that fails
using TimeWarp.Nuru;
using static System.Console;

NuruApp app =
  new NuruAppBuilder()
  .AddAutoHelp()
  .AddRoute
  (
    "{input|Text to generate avatar from (email, username, etc)} " +
    "--output,-o? {file?|Save SVG to file instead of stdout} " +
    "--no-env|Generate without environment circle " +
    "--output-hash|Display hash calculation details instead of SVG",
    GenerateAvatar,
    "Generate unique, deterministic SVG avatars from any text input"
  )
  .Build();

return await app.RunAsync(args);

static void GenerateAvatar(string input, string? file, bool noEnv, bool outputHash)
{
  WriteLine("Input: " + input);
  WriteLine("File: " + (file ?? "null"));
  WriteLine("NoEnv: " + noEnv);
  WriteLine("OutputHash: " + outputHash);
}
