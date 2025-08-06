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

    public static void CatchAllNotAtEnd()
    {
        var builder = new NuruAppBuilder();

        // NURU005: Catch-all must be last
        builder.AddRoute("docker {*args} --verbose", () => { });

        // NURU005: Catch-all must be last
        builder.AddRoute("kubectl {*commands} apply", () => { });

        // This is OK - catch-all is at the end
        builder.AddRoute("docker run {*args}", () => { });
    }

    public static void DuplicateParameterNames()
    {
        var builder = new NuruAppBuilder();

        // NURU006: Duplicate parameter name 'env'
        builder.AddRoute("deploy {env} to {env}", () => { });

        // NURU006: Duplicate parameter name 'file'
        builder.AddRoute("copy {file} {dest} {file}", () => { });

        // This is OK - different parameter names
        builder.AddRoute("deploy {env} {tag}", () => { });
    }

    public static void ConflictingOptionalParameters()
    {
        var builder = new NuruAppBuilder();

        // NURU007: Consecutive optional parameters
        builder.AddRoute("deploy {env?} {tag?}", () => { });

        // NURU007: Three consecutive optional parameters
        builder.AddRoute("backup {source?} {dest?} {format?}", () => { });

        // This is OK - required parameter before optional
        builder.AddRoute("deploy {env} {tag?}", () => { });

        // This is OK - literal between optional parameters
        builder.AddRoute("copy {source?} to {dest?}", () => { });
    }
}