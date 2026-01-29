using System.Reflection;
using Rvne.ReflectiveOptions.Attributes;

namespace Rvne.ReflectiveOptions.Demo;

internal enum Environment
{
    Local,
    CI
}

internal abstract class EnvironmentAwareBase(Environment env)
{
    protected bool IsLocalEnv()
        => env == Environment.Local;
}

[GenerateReflectiveOptions]
[ApplicableOn(GenerationTarget.Member)]
[Inherited(false)]
internal sealed class CommandRunnerOptions
{
    [Alias("Timeout")]
    public int TimeoutMs { get; set; } = 30_000;

    public int Retries { get; set; }

    [Alias("Runnability")]
    public bool Runnable { get; set; } = true;

    public string? Name { get; set; }
}

[GenerateReflectiveOptions]
[ApplicableOn(GenerationTarget.Class)]
[Inherited(false)]
internal sealed class CommandInfo
{
    public string? Description { get; set; }
}

internal interface ICommand
{
    void Execute();
}

internal static partial class Program
{
    private static void PrintCliTreeForEnv(Environment env)
    {
        var root = new Cli(env);
        var rootInfo = root.GetOptions<CommandInfo>();

        Console.WriteLine($"Environment: {env}");

        if (!string.IsNullOrWhiteSpace(rootInfo.Description))
        {
            Console.WriteLine(rootInfo.Description);
        }

        foreach (var property in root.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetValue(root) is not ICommand command)
                continue;

            var options = root.GetMemberOptions<CommandRunnerOptions>(property.Name);
            if (!options.Runnable)
                continue;

            var commandInfo = command.GetOptions<CommandInfo>();
            var displayName = options.Name ?? property.Name.ToLower();
            var description = string.IsNullOrWhiteSpace(commandInfo.Description)
                ? "no description"
                : commandInfo.Description;

            Console.WriteLine(
                $"- {displayName}: timeout={options.TimeoutMs}ms, retries={options.Retries}, description={description}");
        }
    }

    private static int RunCommand(Environment env, string commandName)
    {
        var root = new Cli(env);

        foreach (var property in root.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetValue(root) is not ICommand command)
                continue;

            var options = root.GetMemberOptions<CommandRunnerOptions>(property.Name);
            var displayName = options.Name ?? property.Name.ToLowerInvariant();

            if (!string.Equals(displayName, commandName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!options.Runnable)
            {
                Console.WriteLine($"Command '{displayName}' is not runnable in {env}.");
                return 2;
            }

            Console.WriteLine(
                $"Running '{displayName}' (timeout={options.TimeoutMs}ms, retries={options.Retries})...");

            for (var attempt = 0; attempt <= options.Retries; attempt++)
            {
                try
                {
                    if (!ExecuteWithTimeout(command, options.TimeoutMs))
                    {
                        Console.WriteLine($"Attempt {attempt + 1} timed out.");
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                }
            }

            Console.WriteLine($"Command '{displayName}' failed after {options.Retries + 1} attempts.");
            return 1;
        }

        Console.WriteLine($"Unknown command '{commandName}'.");
        Console.WriteLine("Available commands:");
        PrintCliTreeForEnv(env);
        return 2;
    }

    private static bool ExecuteWithTimeout(ICommand command, int timeoutMs)
    {
        Task task = Task.Run(command.Execute);

        if (task.Wait(timeoutMs))
        {
            task.GetAwaiter().GetResult();
            return true;
        }
        else
        {
            return false;
        }
    }
}
