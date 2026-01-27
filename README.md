# Rvne.ReflectiveOptions

ReflectiveOptions builds option objects from attributes on a source type. It is most useful for configuring complex trees of nested nodes (for example, a CLI command tree). It supports:

- Compile-time attributes that directly apply values to an options instance.
- Derived attributes that compute values by invoking parameterless methods on a context object.
- Inheritance-aware attribute discovery.

## Targets

- `net8.0`
- `net10.0`

## Demo Examples

There is a small runnable CLI demo that exercises the core scenarios in this README:

- Demo overview and run instructions: `Rvne.ReflectiveOptions.Demo/README.md`
- Annotated CLI + commands: `Rvne.ReflectiveOptions.Demo/Program.cs`
- Option types, attributes, and a compact runner: `Rvne.ReflectiveOptions.Demo/DemoInfrastructure.cs`

## Usage

### 1) Define option attributes

The demo defines two option domains:

- Runner options on CLI members (`CommandRunnerOptions`)
- Descriptions on command types (`CommandInfo`)

For compile-time application, inherit `ReflectiveOptionAttribute<TOptions, TMember>`:

```csharp
using Rvne.ReflectiveOptions.Attributes;

public sealed class CommandRunnerOptions
{
    public int TimeoutMs { get; set; } = 30_000;
    public int Retries { get; set; }
    public bool Runnable { get; set; } = true;
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class TimeoutAttribute(int value)
    : ReflectiveOptionAttribute<CommandRunnerOptions, int>(nameof(CommandRunnerOptions.TimeoutMs), value);

[AttributeUsage(AttributeTargets.Property)]
public sealed class RetriesAttribute(int value)
    : ReflectiveOptionAttribute<CommandRunnerOptions, int>(nameof(CommandRunnerOptions.Retries), value);

[AttributeUsage(AttributeTargets.Property)]
public sealed class NameAttribute(string value)
    : ReflectiveOptionAttribute<CommandRunnerOptions, string>(nameof(CommandRunnerOptions.Name), value);

public sealed class CommandInfo
{
    public string? Description { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class DescriptionAttribute(string description)
    : ReflectiveOptionAttribute<CommandInfo, string>(nameof(CommandInfo.Description), description);
```

For derived values, inherit `DerivedReflectiveOptionAttribute<TOptions, TDerivationResult>`:

```csharp
using Rvne.ReflectiveOptions.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DerivedTimeoutAttribute(string methodName)
    : DerivedReflectiveOptionAttribute<CommandRunnerOptions, int>(nameof(CommandRunnerOptions.TimeoutMs), methodName);

[AttributeUsage(AttributeTargets.Property)]
public sealed class DerivedRunnabilityAttribute(string methodName)
    : DerivedReflectiveOptionAttribute<CommandRunnerOptions, bool>(nameof(CommandRunnerOptions.Runnable), methodName);
```

If you need complete control, you can implement the middleware interfaces directly:

```csharp
using Rvne.ReflectiveOptions.Interfaces;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CustomTimeoutAttribute(int value) : Attribute, IStaticOptionMiddleware<CommandRunnerOptions>
{
    public void Apply(CommandRunnerOptions options) => options.TimeoutMs = value;
}
```

### 2) Annotate your source type

This is shown end-to-end in the demo CLI: see `Rvne.ReflectiveOptions.Demo/Program.cs`.

```csharp
public enum Environment
{
    Local,
    CI
}

public abstract class EnvironmentAwareBase(Environment env)
{
    protected bool IsLocalEnv() => env == Environment.Local;
}

[Description("Root CLI")]
public sealed class Cli(Environment env) : EnvironmentAwareBase(env)
{
    [Retries(2)]
    [Timeout(45_000)]
    [DerivedRunnability(nameof(IsLocalEnv))]
    [Name("push-remote")]
    public PushCommand Push { get; } = new();

    [DerivedTimeout(nameof(GetPullCommandTimeout))]
    [Name("pull-remote")]
    public PullCommand Pull { get; } = new();

    private int GetPullCommandTimeout() => IsLocalEnv() ? 15_000 : 90_000;
}
```

### 3) Calculate options

```csharp
var root = new Cli(Environment.CI);

var pullOptions = root.GetMemberOptions<CommandRunnerOptions>(nameof(Cli.Pull));
// pullOptions.TimeoutMs == 90_000
// pullOptions.Name == "pull-remote"

var pushOptions = root.GetMemberOptions<CommandRunnerOptions>(nameof(Cli.Push));
// pushOptions.Runnable == false (derived from IsLocalEnv in CI)
```

## Derivation rules

- The derivation method name is provided by the attribute and looked up via reflection.
- Methods can be instance or static, must be parameterless, and must return a value (non-void).
- If the method returns `null` for a non-nullable value type, an exception is thrown.
- If the method returns `null` for a non-nullable reference type, an exception is thrown.
- The returned value must be assignable to the option member referenced by the attribute.

## Mixing option domains

You can map different option types to different parts of the same tree. The demo uses:

- `CommandRunnerOptions` on CLI members (properties)
- `CommandInfo` on command classes

```csharp
var root = new Cli(Environment.Local);

var pullRunner = root.GetMemberOptions<CommandRunnerOptions>(nameof(Cli.Pull));
var pullInfo = root.Pull.GetOptions<CommandInfo>();

// pullRunner.TimeoutMs comes from member attributes (and derivation on the parent).
// pullInfo.Description comes from attributes on the command type itself.
```

### Member-level attributes

`GetMemberOptions` applies only the attributes defined on the named member (property/field). It does not read attributes from the member's value type. Use `GetOptions` on the value itself if you need the value's own attributes applied.
