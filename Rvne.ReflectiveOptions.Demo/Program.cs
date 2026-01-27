namespace Rvne.ReflectiveOptions.Demo;

internal static class Program
{
    private static void Main()
    {
        // Compare resolved options for two execution contexts.
        RunDemo(ExecutionProfile.Local);

        Console.WriteLine();

        RunDemo(ExecutionProfile.CI);
    }

    private static void RunDemo(ExecutionProfile profile)
    {
        // Build a command tree and resolve reflective options from attributes.
        var root = new CliRoot(profile);

        // Print the resolved options at each node.
        Console.WriteLine($"Profile: {profile}");
        CommandTree.PrintTree(root);
    }
}

internal enum ExecutionProfile
{
    Local,
    CI
}

[Timeout(30_000)]
[Retries(1)]
internal sealed class CliRoot(ExecutionProfile profile)
{
    public string Name { get; } = "rvne";

    // Execution profile is used by derived options in children.
    public ExecutionProfile Profile { get; } = profile;

    // Static options on groups and actions flow through the tree.
    public SyncGroup Sync { get; } = new();

    public CacheGroup Cache { get; } = new CacheGroup(profile);

    // A leaf command can still carry options.
    [DryRun(true)]
    public LintCommand Lint { get; } = new();
}

[MaxParallelism(4)]
internal sealed class SyncGroup
{
    public string Name { get; } = "sync";

    public PullCommand Pull { get; } = new();

    public PushCommand Push { get; } = new();
}

internal sealed class CacheGroup(ExecutionProfile profile)
{
    public string Name { get; } = "cache";

    // Commands that derive behavior from the execution profile.
    public WarmCacheCommand Warm { get; } = new WarmCacheCommand(profile);
    public ClearCacheCommand Clear { get; } = new ClearCacheCommand(profile);
}

[Timeout(15_000)]
internal sealed class PullCommand
{
    public string Name { get; } = "pull";
}

[Timeout(45_000)]
[Retries(2)]
internal sealed class PushCommand
{
    public string Name { get; } = "push";
}

[DerivedTimeout(nameof(ComputeTimeout))]
internal sealed class WarmCacheCommand(ExecutionProfile profile)
{
    public string Name { get; } = "warm";

    // Derived option: timeout changes between Local and CI.
    private readonly ExecutionProfile _profile = profile;

    private int ComputeTimeout()
        => _profile == ExecutionProfile.CI ? 90_000 : 20_000;
}

[DerivedVisibility(nameof(ComputeVisibility))]
internal sealed class ClearCacheCommand(ExecutionProfile profile)
{
    public string Name { get; } = "clear";

    // Derived option: hide destructive command in CI.
    private readonly ExecutionProfile _profile = profile;

    private bool ComputeVisibility()
        => _profile != ExecutionProfile.CI;
}

internal sealed class LintCommand
{
    public string Name { get; } = "lint";
}
