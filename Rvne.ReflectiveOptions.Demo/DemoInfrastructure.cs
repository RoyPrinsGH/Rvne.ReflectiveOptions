using Rvne.ReflectiveOptions;

namespace Rvne.ReflectiveOptions.Demo;

internal static class CommandTree
{
    public static ResolvedCommand BuildResolvedTree(CommandNode node)
    {
        var options = node.GetOptions<CommandOptions>();
        var resolved = new ResolvedCommand(node.Name, options);

        foreach (var child in node.Children)
        {
            resolved.Children.Add(BuildResolvedTree(child));
        }

        return resolved;
    }

    public static void PrintTree(ResolvedCommand node, int indent)
    {
        Console.WriteLine($"{new string(' ', indent * 2)}- {node.Name} " +
                          $"(Timeout={node.Options.TimeoutMs}ms, Retries={node.Options.Retries}, " +
                          $"Parallelism={node.Options.MaxParallelism}, DryRun={node.Options.DryRun}, " +
                          $"Visible={node.Options.Visible})");

        foreach (var child in node.Children)
        {
            PrintTree(child, indent + 1);
        }
    }
}

internal sealed class CommandOptions
{
    public int TimeoutMs { get; set; } = 30_000;
    public int Retries { get; set; }
    public int MaxParallelism { get; set; } = 1;
    public bool DryRun { get; set; }
    public bool Visible { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class TimeoutAttribute(int value) : Attribute, IOptionAttribute<CommandOptions>
{
    public void Apply(CommandOptions options)
        => options.TimeoutMs = value;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class RetriesAttribute(int value) : Attribute, IOptionAttribute<CommandOptions>
{
    public void Apply(CommandOptions options)
        => options.Retries = value;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class MaxParallelismAttribute(int value) : Attribute, IOptionAttribute<CommandOptions>
{
    public void Apply(CommandOptions options)
        => options.MaxParallelism = value;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class DryRunAttribute(bool value) : Attribute, IOptionAttribute<CommandOptions>
{
    public void Apply(CommandOptions options)
        => options.DryRun = value;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class VisibilityAttribute(bool value) : Attribute, IOptionAttribute<CommandOptions>
{
    public void Apply(CommandOptions options)
        => options.Visible = value;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class DerivedTimeoutAttribute(string methodName)
    : DerivedOptionAttribute<CommandOptions, int>(methodName)
{
    public override void Apply(int derivationResult, CommandOptions options)
        => options.TimeoutMs = derivationResult;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
internal sealed class DerivedVisibilityAttribute(string methodName)
    : DerivedOptionAttribute<CommandOptions, bool>(methodName)
{
    public override void Apply(bool derivationResult, CommandOptions options)
        => options.Visible = derivationResult;
}

internal abstract class CommandNode(string name)
{
    public string Name { get; } = name;
    public List<CommandNode> Children { get; } = [];
}

internal class CommandGroup(string name) : CommandNode(name)
{
}

internal class CommandAction(string name) : CommandNode(name)
{
}

internal class ResolvedCommand(string name, CommandOptions options)
{
    public string Name { get; } = name;
    public CommandOptions Options { get; } = options;
    public List<ResolvedCommand> Children { get; } = [];
}
