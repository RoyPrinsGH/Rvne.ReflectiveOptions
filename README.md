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

### 1) Generate option attributes (recommended)

The demo defines two option domains:

- Runner options on CLI members (`CommandRunnerOptions`)
- Descriptions on command types (`CommandInfo`)

The preferred path is the source generator. It creates both static and derived
attributes for every public settable property on an options type.

```csharp
using System;
using Rvne.ReflectiveOptions.Attributes;

[GenerateReflectiveOptions]
[ApplicableOn(GenerationTarget.Member)]
[Inherited(false)]
public sealed class CommandRunnerOptions
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
public sealed class CommandInfo
{
    [Alias("Description")]
    public string? Description { get; set; }
}
```

Use `[Ignore]` on a property to skip generation for that member.
Use `[ApplicableOn]`, `[Inherited]`, and `[AllowMultiple]` to control
the generated `AttributeUsage` targets, `Inherited`, and `AllowMultiple`.

From the `CommandRunnerOptions` example above, the generator produces:

- `TimeoutAttribute` and `DerivedTimeoutAttribute`
- `RetriesAttribute` and `DerivedRetriesAttribute`
- `RunnabilityAttribute` and `DerivedRunnabilityAttribute`
- `NameAttribute` and `DerivedNameAttribute`

From `CommandInfo`, it produces `DescriptionAttribute` and `DerivedDescriptionAttribute`.

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

var rootOptions = root.GetOptions<CommandInfo>();
// rootOptions.Description == "Root CLI"

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
