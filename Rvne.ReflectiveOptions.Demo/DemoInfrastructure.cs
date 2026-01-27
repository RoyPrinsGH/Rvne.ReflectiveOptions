using Rvne.ReflectiveOptions;
using Rvne.ReflectiveOptions.Attributes;

namespace Rvne.ReflectiveOptions.Demo;

internal static class CommandTree
{
    public static void PrintTree(CliRoot root)
    {
        PrintNode(root.Name, root.GetOptions<CommandOptions>(), new MemberOptions(), indent: 0);

        var syncMemberOptions = root.GetMemberOptions<MemberOptions>(nameof(CliRoot.Sync));
        PrintNode(root.Sync.Name, root.Sync.GetOptions<CommandOptions>(), syncMemberOptions, indent: 1);

        var pullMemberOptions = root.Sync.GetMemberOptions<MemberOptions>(nameof(SyncGroup.Pull));
        PrintNode(root.Sync.Pull.Name, root.Sync.Pull.GetOptions<CommandOptions>(), pullMemberOptions, indent: 2);

        var pushMemberOptions = root.Sync.GetMemberOptions<MemberOptions>(nameof(SyncGroup.Push));
        PrintNode(root.Sync.Push.Name, root.Sync.Push.GetOptions<CommandOptions>(), pushMemberOptions, indent: 2);

        var cacheMemberOptions = root.GetMemberOptions<MemberOptions>(nameof(CliRoot.Cache));
        PrintNode(root.Cache.Name, root.Cache.GetOptions<CommandOptions>(), cacheMemberOptions, indent: 1);

        var warmMemberOptions = root.Cache.GetMemberOptions<MemberOptions>(nameof(CacheGroup.Warm));
        PrintNode(root.Cache.Warm.Name, root.Cache.Warm.GetOptions<CommandOptions>(), warmMemberOptions, indent: 2);

        var clearMemberOptions = root.Cache.GetMemberOptions<MemberOptions>(nameof(CacheGroup.Clear));
        PrintNode(root.Cache.Clear.Name, root.Cache.Clear.GetOptions<CommandOptions>(), clearMemberOptions, indent: 2);

        var lintMemberOptions = root.GetMemberOptions<MemberOptions>(nameof(CliRoot.Lint));
        PrintNode(root.Lint.Name, root.Lint.GetOptions<CommandOptions>(), lintMemberOptions, indent: 1);
    }

    private static void PrintNode(string name, CommandOptions options, MemberOptions memberOptions, int indent)
    {
        Console.WriteLine($"{new string(' ', indent * 2)}- {name} " +
                          $"(Timeout={options.TimeoutMs}ms, Retries={options.Retries}, " +
                          $"Parallelism={options.MaxParallelism}, DryRun={memberOptions.DryRun}, " +
                          $"Visible={options.Visible})");
    }
}

internal sealed class CommandOptions
{
    public int TimeoutMs { get; set; } = 30_000;
    public int Retries { get; set; }
    public int MaxParallelism { get; set; } = 1;
    public bool Visible { get; set; } = true;
}

internal sealed class MemberOptions
{
    public bool DryRun { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class TimeoutAttribute(int value)
    : ReflectiveOptionAttribute<CommandOptions, int>(nameof(CommandOptions.TimeoutMs), value)
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class RetriesAttribute(int value)
    : ReflectiveOptionAttribute<CommandOptions, int>(nameof(CommandOptions.Retries), value)
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class MaxParallelismAttribute(int value)
    : ReflectiveOptionAttribute<CommandOptions, int>(nameof(CommandOptions.MaxParallelism), value)
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class DryRunAttribute(bool value)
    : ReflectiveOptionAttribute<MemberOptions, bool>(nameof(MemberOptions.DryRun), value)
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class VisibilityAttribute(bool value)
    : ReflectiveOptionAttribute<CommandOptions, bool>(nameof(CommandOptions.Visible), value)
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class DerivedTimeoutAttribute(string methodName)
    : DerivedReflectiveOptionAttribute<CommandOptions, int>(nameof(CommandOptions.TimeoutMs), methodName)
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class DerivedVisibilityAttribute(string methodName)
    : DerivedReflectiveOptionAttribute<CommandOptions, bool>(nameof(CommandOptions.Visible), methodName)
{
}
