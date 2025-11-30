namespace TimeWarp.Nuru.Analyzers.Tests;

using TimeWarp.Nuru;

public static class TestSamples
{
    public static void InvalidParameterSyntax()
    {
        NuruAppBuilder builder = new();

        // NURU001: Should suggest {env} instead of <env>
        builder.Map("deploy <env>", () => { });
    }

    public static void UnbalancedBraces()
    {
        NuruAppBuilder builder = new();

        // NURU002: Missing closing brace
        builder.Map("deploy {env", () => { });

        // NURU002: Missing opening brace
        builder.Map("deploy env}", () => { });
    }

    public static void InvalidOptionFormat()
    {
        NuruAppBuilder builder = new();

        // NURU003: Should be --verbose or -v
        builder.Map("test -verbose", () => { });
    }

    public static void InvalidTypeConstraint()
    {
        NuruAppBuilder builder = new();

        // NURU004: Invalid type constraint
        builder.Map("wait {seconds:integer}", () => { });

        // NURU004: Should be DateTime not Date
        builder.Map("schedule {when:Date}", () => { });

        // NURU004: float is not supported (yet)
        builder.Map("calculate {value:float}", () => { });
    }

    public static void CatchAllNotAtEnd()
    {
        NuruAppBuilder builder = new();

        // NURU005: Catch-all must be last
        builder.Map("docker {*args} --verbose", () => { });

        // NURU005: Catch-all must be last
        builder.Map("kubectl {*commands} apply", () => { });

        // This is OK - catch-all is at the end
        builder.Map("docker run {*args}", () => { });
    }

    public static void DuplicateParameterNames()
    {
        NuruAppBuilder builder = new();

        // NURU006: Duplicate parameter name 'env'
        builder.Map("deploy {env} to {env}", () => { });

        // NURU006: Duplicate parameter name 'file'
        builder.Map("copy {file} {dest} {file}", () => { });

        // This is OK - different parameter names
        builder.Map("deploy {env} {tag}", () => { });
    }

    public static void ConflictingOptionalParameters()
    {
        NuruAppBuilder builder = new();

        // NURU007: Consecutive optional parameters
        builder.Map("deploy {env?} {tag?}", () => { });

        // NURU007: Three consecutive optional parameters
        builder.Map("backup {source?} {dest?} {format?}", () => { });

        // This is OK - required parameter before optional
        builder.Map("deploy {env} {tag?}", () => { });

        // This is OK - literal between optional parameters
        builder.Map("copy {source?} to {dest?}", () => { });
    }

    public static void MixedCatchAllWithOptional()
    {
        NuruAppBuilder builder = new();

        // NURU008: Cannot mix optional with catch-all
        builder.Map("deploy {env?} {*args}", () => { });

        // NURU008: Cannot mix optional with catch-all (different order)
        builder.Map("run {script} {config?} {*args}", () => { });

        // This is OK - required parameter with catch-all
        builder.Map("docker {command} {*args}", () => { });

        // This is OK - only catch-all
        builder.Map("exec {*args}", () => { });
    }

    public static void DuplicateOptionAlias()
    {
        NuruAppBuilder builder = new();

        // NURU009: Both options use -v
        builder.Map("test --verbose,-v --version,-v", () => { });

        // NURU009: Three options with same short form -d
        builder.Map("build --debug,-d --deploy,-d --dry-run,-d", () => { });

        // This is OK - different short forms
        builder.Map("run --verbose,-v --debug,-d", () => { });

        // This is OK - one has short form, other doesn't
        builder.Map("deploy --verbose,-v --force", () => { });
    }
}