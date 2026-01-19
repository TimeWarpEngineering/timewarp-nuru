namespace TimeWarp.Nuru;

/// <summary>
/// Represents the end-of-options separator (--) in a route pattern.
/// Everything after this separator is treated as positional arguments,
/// not options. This is a POSIX convention.
/// Example: "git checkout -- {file}" - the -- prevents file from being
/// interpreted as an option or branch name.
/// </summary>
internal record EndOfOptionsSyntax() : SegmentSyntax
{
  public override string ToString() => "EndOfOptions: '--'";
}
