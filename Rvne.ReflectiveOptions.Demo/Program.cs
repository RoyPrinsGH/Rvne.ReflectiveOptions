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
        var resolved = CommandTree.BuildResolvedTree(root);

        // Print the resolved options at each node.
        Console.WriteLine($"Profile: {profile}");
        CommandTree.PrintTree(resolved, indent: 0);
    }
}

internal enum ExecutionProfile
{
    Local,
    CI
}

[Timeout(30_000)]
[Retries(1)]
internal sealed class CliRoot : CommandNode
{
    // Execution profile is used by derived options in children.
    public ExecutionProfile Profile { get; }

    // Static options on groups and actions flow through the tree.
    [MaxParallelism(4)]
    public SyncGroup Sync { get; } = new();

    public CacheGroup Cache { get; }

    // A leaf command can still carry options.
    [DryRun(true)]
    public CommandAction Lint { get; } = new("lint");

    public CliRoot(ExecutionProfile profile) : base("rvne")
    {
        Profile = profile;
        Cache = new CacheGroup(profile);

        Children.Add(Sync);
        Children.Add(Cache);
        Children.Add(Lint);
    }
}

internal sealed class SyncGroup : CommandGroup
{
    [Timeout(15_000)]
    public CommandAction Pull { get; } = new("pull");

    [Timeout(45_000)]
    [Retries(2)]
    public CommandAction Push { get; } = new("push");

    public SyncGroup() : base("sync")
    {
        Children.Add(Pull);
        Children.Add(Push);
    }
}

internal sealed class CacheGroup : CommandGroup
{
    // Commands that derive behavior from the execution profile.
    public WarmCacheCommand Warm { get; }
    public ClearCacheCommand Clear { get; }

    public CacheGroup(ExecutionProfile profile) : base("cache")
    {
        Warm = new WarmCacheCommand(profile);
        Clear = new ClearCacheCommand(profile);

        Children.Add(Warm);
        Children.Add(Clear);
    }
}

[DerivedTimeout(nameof(ComputeTimeout))]
internal sealed class WarmCacheCommand(ExecutionProfile profile) : CommandAction("warm")
{
    // Derived option: timeout changes between Local and CI.
    private readonly ExecutionProfile _profile = profile;

    private int ComputeTimeout()
        => _profile == ExecutionProfile.CI ? 90_000 : 20_000;
}

[DerivedVisibility(nameof(ComputeVisibility))]
internal sealed class ClearCacheCommand(ExecutionProfile profile) : CommandAction("clear")
{
    // Derived option: hide destructive command in CI.
    private readonly ExecutionProfile _profile = profile;

    private bool ComputeVisibility()
        => _profile != ExecutionProfile.CI;
}
