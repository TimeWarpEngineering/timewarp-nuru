#region Purpose
// Workflow handler: step bookkeeping, package verification, attestation policy step, and repo helpers.
#endregion

namespace DevCli;

public sealed partial class WorkflowCommand
{
  internal sealed partial class Handler
  {
    private string BeginStep(int stepNumber, List<string> stepNames)
    {
      string label = $"Step {stepNumber}/{stepNames.Count}";
      if (stepNumber > 1)
      {
        Terminal.WriteLine("");
      }

      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine($"  {label}: {stepNames[stepNumber - 1]}");
      Terminal.WriteLine("===============================================================================");
      return label;
    }

    // Package layout and Release-configuration gates. Returns false (and sets
    // the exit code) when the built packages must not ship.
    private bool VerifyBuiltPackages()
    {
      // Package layout gate (kanban 461): the nupkgs this run uploads are the
      // exact bytes a future release promotes — verify the critical payload is
      // inside them, because pack can silently produce hollow packages and
      // tests never look inside a nupkg.
      string layoutRepoRoot = ResolveRepoRoot();
      string? layoutVersion = ReadPropsVersion(layoutRepoRoot);
      if (string.IsNullOrWhiteSpace(layoutVersion))
      {
        Terminal.WriteErrorLine("Package layout gate failed: could not read package version from directory.build.props — cannot locate the TimeWarp.Nuru nupkg to verify.");
        Environment.ExitCode = 1;
        return false;
      }

      string nuruNupkg = Path.Combine(layoutRepoRoot, "artifacts", "packages", $"TimeWarp.Nuru.{layoutVersion}.nupkg");
      if (!File.Exists(nuruNupkg))
      {
        Terminal.WriteErrorLine($"Package layout gate failed: expected {nuruNupkg} to exist after build (GeneratePackageOnBuild).");
        Environment.ExitCode = 1;
        return false;
      }

      IReadOnlyList<string> missing = NupkgLayoutCheck.FindMissing(nuruNupkg, NupkgLayoutCheck.NuruRequiredPackageEntries);
      if (missing.Count > 0)
      {
        Terminal.WriteErrorLine($"Package layout gate failed: TimeWarp.Nuru.{layoutVersion}.nupkg is missing required entries: {string.Join(", ", missing)}. Pack produced a hollow package (kanban 461 class) — do not ship this artifact.");
        Environment.ExitCode = 1;
        return false;
      }

      Terminal.WriteLine($"Package layout verified: TimeWarp.Nuru.{layoutVersion}.nupkg contains all {NupkgLayoutCheck.NuruRequiredPackageEntries.Length} required payload entries.");

      // Release-configuration gate (task 503): published betas up to beta.80 shipped
      // Debug assemblies because later steps repacked over the Release package.
      AssemblyBuildInfo packedBuildInfo = NupkgLayoutCheck.ReadPackedAssemblyBuildInfo(nuruNupkg, "lib/net11.0/TimeWarp.Nuru.dll");
      if (!string.Equals(packedBuildInfo.Configuration, "Release", StringComparison.Ordinal) || !packedBuildInfo.IsOptimized)
      {
        Terminal.WriteErrorLine($"Package layout gate failed: TimeWarp.Nuru.{layoutVersion}.nupkg lib/net11.0/TimeWarp.Nuru.dll is not a Release build (Configuration='{packedBuildInfo.Configuration}', Debuggable flags=0x{packedBuildInfo.DebuggableFlags:X}). Do not ship this artifact.");
        Environment.ExitCode = 1;
        return false;
      }

      Terminal.WriteLine("Package configuration verified: lib/net11.0/TimeWarp.Nuru.dll is a Release, optimized build.");
      return true;
    }

    // Package integrity gate (task 503), part 2.
    private bool VerifyPackagesUnchanged(string packagesDir, Dictionary<string, string> packageHashes)
    {
      Dictionary<string, string> finalHashes = HashPackages(packagesDir);
      List<string> changedPackages = [];
      foreach ((string packageName, string packageHash) in packageHashes)
      {
        if (!finalHashes.TryGetValue(packageName, out string? finalHash))
        {
          changedPackages.Add($"{packageName} (disappeared)");
        }
        else if (!string.Equals(packageHash, finalHash, StringComparison.Ordinal))
        {
          changedPackages.Add($"{packageName} (changed)");
        }
      }

      foreach (string packageName in finalHashes.Keys)
      {
        if (!packageHashes.ContainsKey(packageName))
        {
          changedPackages.Add($"{packageName} (appeared)");
        }
      }

      if (changedPackages.Count > 0)
      {
        Terminal.WriteErrorLine($"Package integrity gate failed: the samples/tests steps rebuilt or repacked a package after the verified build: {string.Join(", ", changedPackages.Order(StringComparer.Ordinal))}. The artifact must not ship.");
        Environment.ExitCode = 1;
        return false;
      }

      Terminal.WriteLine($"Package integrity verified: {packageHashes.Count} packages unchanged since build.");
      return true;
    }

    // Shared attestation policy for PR/merge and release (kanban task 458-011).
    // Precedence: CLI --attestation (is not null so empty still overrides) >
    // config attestation.mode > context default (whenUnset: DefaultPrMode /
    // DefaultReleaseMode). Mode interpretation is pure in
    // AttestationConfigResolver; this method only applies the switch:
    // Off → skip verify; Require → verify + abort on non-Valid; Warn → verify
    // + advisory, never abort (RepeatAdvisory set so PR can re-print before
    // SUCCEEDED). Unrecognized non-blank raw values surface a typo warning
    // then EffectiveMode(whenUnset) — never silently Off.
    private async Task<AttestationStepResult> RunAttestationPolicyStepAsync(
      string repoRoot,
      string? cliAttestationMode,
      AttestationMode whenUnset)
    {
      RepoConfig config = await ConfigService.GetConfigAsync(CancellationToken.None).ConfigureAwait(false);
      string? rawMode = cliAttestationMode is not null
        ? cliAttestationMode
        : config.Attestation?.Mode;
      AttestationModeResolution resolution = AttestationConfigResolver.ResolveMode(rawMode);
      AttestationMode mode = AttestationConfigResolver.EffectiveMode(resolution, whenUnset);

      if (resolution.UnrecognizedValue is not null)
      {
        string defaultLabel = whenUnset.ToString().ToLowerInvariant();
        Terminal.WriteLine($"Warning: unrecognized attestation.mode '{resolution.UnrecognizedValue}' — treating as '{defaultLabel}'. Valid values: off, warn, require.".Yellow());
      }

      if (mode == AttestationMode.Off)
      {
        Terminal.WriteLine("Attestation skipped (mode=off).");
        return new AttestationStepResult(ShouldAbort: false, RepeatAdvisory: null);
      }

      AttestationVerifyOutcome outcome = await VerifyAttestationAsync(repoRoot).ConfigureAwait(false);

      if (outcome.Status == AttestationVerificationStatus.Valid)
      {
        Terminal.WriteLine($"Attestation valid: check_set {ShortSha(outcome.CheckSet)} ts {outcome.Ts}");
        return new AttestationStepResult(ShouldAbort: false, RepeatAdvisory: null);
      }

      string message = DescribeAttestationOutcome(outcome);

      if (mode == AttestationMode.Require)
      {
        Terminal.WriteErrorLine($"Attestation required (mode=require): {message}");
        return new AttestationStepResult(ShouldAbort: true, RepeatAdvisory: null);
      }

      string advisory = $"Attestation advisory (mode=warn, not enforced): {message}";
      Terminal.WriteLine("");
      Terminal.WriteLine("*******************************************************************************".Yellow());
      Terminal.WriteLine($"  {advisory}".Yellow());
      Terminal.WriteLine("  Set attestation.mode = \"require\" in .timewarp/dev.jsonc (or pass".Yellow());
      Terminal.WriteLine("  --attestation require) to enforce this in CI (kanban task 458-011).".Yellow());
      Terminal.WriteLine("*******************************************************************************".Yellow());

      return new AttestationStepResult(ShouldAbort: false, RepeatAdvisory: advisory);
    }

    private static Dictionary<string, string> HashPackages(string packagesDir)
    {
      Dictionary<string, string> hashes = new(StringComparer.Ordinal);
      if (!Directory.Exists(packagesDir))
      {
        return hashes;
      }

      foreach (string file in Directory.EnumerateFiles(packagesDir, "*.nupkg"))
      {
        using FileStream stream = File.OpenRead(file);
        hashes[Path.GetFileName(file)] = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
      }

      return hashes;
    }

    private void AbortPipeline(string reason)
    {
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine($"  Pipeline ABORTED — {reason}");
      Terminal.WriteLine("===============================================================================");
      Environment.ExitCode = 1;
    }

    // Repo root heuristic shared by both pipeline modes: prefer the
    // AOT-published binary's on-disk layout (bin/<rid>/ -> repo root is four
    // levels up); fall back to CWD when that guess misses (e.g. running via
    // `dotnet run tools/dev-cli/dev.cs` instead of the published binary).
    private static string ResolveRepoRoot()
    {
      string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        repoRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
      }

      return repoRoot;
    }
  }
}
