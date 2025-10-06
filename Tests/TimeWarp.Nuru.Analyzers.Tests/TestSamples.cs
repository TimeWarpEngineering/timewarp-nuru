namespace TimeWarp.Nuru.Analyzers.Tests;

using TimeWarp.Nuru;

public static class TestSamples
{
    public static void InvalidParameterSyntax()
    {
        NuruAppBuilder builder = new();

        // NURU001: Should suggest {env} instead of <env>
        builder.AddRoute("deploy <env>", () => { });
    }

    public static void UnbalancedBraces()
    {
        NuruAppBuilder builder = new();

        // NURU002: Missing closing brace
        builder.AddRoute("deploy {env", () => { });

        // NURU002: Missing opening brace
        builder.AddRoute("deploy env}", () => { });
    }

    public static void InvalidOptionFormat()
    {
        NuruAppBuilder builder = new();

        // NURU003: Should be --verbose or -v
        builder.AddRoute("test -verbose", () => { });
    }

    public static void InvalidTypeConstraint()
    {
        NuruAppBuilder builder = new();

        // NURU004: Invalid type constraint
        builder.AddRoute("wait {seconds:integer}", () => { });

        // NURU004: Should be DateTime not Date
        builder.AddRoute("schedule {when:Date}", () => { });

        // NURU004: float is not supported (yet)
        builder.AddRoute("calculate {value:float}", () => { });
    }

    public static void CatchAllNotAtEnd()
    {
        NuruAppBuilder builder = new();

        // NURU005: Catch-all must be last
        builder.AddRoute("docker {*args} --verbose", () => { });

        // NURU005: Catch-all must be last
        builder.AddRoute("kubectl {*commands} apply", () => { });

        // This is OK - catch-all is at the end
        builder.AddRoute("docker run {*args}", () => { });
    }

    public static void DuplicateParameterNames()
    {
        NuruAppBuilder builder = new();

        // NURU006: Duplicate parameter name 'env'
        builder.AddRoute("deploy {env} to {env}", () => { });

        // NURU006: Duplicate parameter name 'file'
        builder.AddRoute("copy {file} {dest} {file}", () => { });

        // This is OK - different parameter names
        builder.AddRoute("deploy {env} {tag}", () => { });
    }

    public static void ConflictingOptionalParameters()
    {
        NuruAppBuilder builder = new();

        // NURU007: Consecutive optional parameters
        builder.AddRoute("deploy {env?} {tag?}", () => { });

        // NURU007: Three consecutive optional parameters
        builder.AddRoute("backup {source?} {dest?} {format?}", () => { });

        // This is OK - required parameter before optional
        builder.AddRoute("deploy {env} {tag?}", () => { });

        // This is OK - literal between optional parameters
        builder.AddRoute("copy {source?} to {dest?}", () => { });
    }

    public static void MixedCatchAllWithOptional()
    {
        NuruAppBuilder builder = new();

        // NURU008: Cannot mix optional with catch-all
        builder.AddRoute("deploy {env?} {*args}", () => { });

        // NURU008: Cannot mix optional with catch-all (different order)
        builder.AddRoute("run {script} {config?} {*args}", () => { });

        // This is OK - required parameter with catch-all
        builder.AddRoute("docker {command} {*args}", () => { });

        // This is OK - only catch-all
        builder.AddRoute("exec {*args}", () => { });
    }

    public static void DuplicateOptionAlias()
    {
        NuruAppBuilder builder = new();

        // NURU009: Both options use -v
        builder.AddRoute("test --verbose,-v --version,-v", () => { });

        // NURU009: Three options with same short form -d
        builder.AddRoute("build --debug,-d --deploy,-d --dry-run,-d", () => { });

        // This is OK - different short forms
        builder.AddRoute("run --verbose,-v --debug,-d", () => { });

        // This is OK - one has short form, other doesn't
        builder.AddRoute("deploy --verbose,-v --force", () => { });
    }
}