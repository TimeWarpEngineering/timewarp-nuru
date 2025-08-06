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

    public static void InvalidTypeConstraint()
    {
        var builder = new NuruAppBuilder();

        // NURU004: Invalid type constraint
        builder.AddRoute("wait {seconds:integer}", () => { });
        
        // NURU004: Should be DateTime not Date
        builder.AddRoute("schedule {when:Date}", () => { });
        
        // NURU004: float is not supported (yet)
        builder.AddRoute("calculate {value:float}", () => { });
    }
}