namespace TimeWarp.Nuru.Analyzers.Tests;

using TimeWarp.Nuru;

public static class TestSamples
{
    public static void InvalidParameterSyntax()
    {
        var builder = new NuruAppBuilder();

        // NURU001: Should suggest {env} instead of <env>
        builder.AddRoute("deploy <env>", () => { });
    }

    public static void UnbalancedBraces()
    {
        var builder = new NuruAppBuilder();

        // NURU002: Missing closing brace
        builder.AddRoute("deploy {env", () => { });

        // NURU002: Missing opening brace
        builder.AddRoute("deploy env}", () => { });
    }

    public static void InvalidOptionFormat()
    {
        var builder = new NuruAppBuilder();

        // NURU003: Should be --verbose or -v
        builder.AddRoute("test -verbose", () => { });
    }
}