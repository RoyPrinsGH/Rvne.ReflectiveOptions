namespace Rvne.ReflectiveOptions.Demo;

[Description("Root CLI")]
internal sealed class Cli(Environment env) : EnvironmentAwareBase(env)
{
    [Retries(2)]
    [Timeout(45_000)]
    [DerivedRunnability(nameof(IsLocalEnv))]
    [Name("push-remote")]
    public PushCommand Push { get; } = new();

    [DerivedTimeout(nameof(GetPullCommandTimeout))]
    [Name("pull-remote")]
    public PullCommand Pull { get; } = new();

    int GetPullCommandTimeout() => IsLocalEnv() ? 15_000 : 90_000;
}

[Description("Pulls data from the API")]
internal sealed class PullCommand : ICommand
{
    public void Execute()
    {
        Console.WriteLine("Pulling!");
    }
}

[Description("Pushes data to the API")]
internal sealed class PushCommand : ICommand
{
    public void Execute()
    {
        Console.WriteLine("Pushing!");
    }
}

internal static partial class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            return RunCommand(Environment.Local, args[0]);
        }
        else
        {
            PrintCliTreeForEnv(Environment.Local);
            Console.WriteLine();
            PrintCliTreeForEnv(Environment.CI);
            Console.WriteLine();
            Console.WriteLine("Try: dotnet run --project Rvne.ReflectiveOptions.Demo -- push-remote");

            return 0;
        }
    }
}
