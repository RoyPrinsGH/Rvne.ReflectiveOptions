# Rvne.ReflectiveOptions

ReflectiveOptions builds option objects from attributes on a source type. It is most useful for configuring complex trees of nested nodes (for example, a CLI command tree). It supports:

- Compile-time attributes that directly apply values to an options instance.
- Derived attributes that compute values by invoking parameterless methods on a context object.
- Inheritance-aware attribute discovery.

## Targets

- `net6.0`
- `net8.0`
- `net10.0`

## Usage

### 1) Define option attributes

For compile-time application, inherit `ReflectiveOptionAttribute<TOptions, TMember>`:

```csharp
using Rvne.ReflectiveOptions.Attributes;

public sealed class CommandOptions
{
    public int TimeoutMs { get; set; } = 30_000;
    public int Retries { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class TimeoutAttribute(int value)
    : ReflectiveOptionAttribute<CommandOptions, int>(nameof(CommandOptions.TimeoutMs), value)
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class RetriesAttribute(int value)
    : ReflectiveOptionAttribute<CommandOptions, int>(nameof(CommandOptions.Retries), value)
{
}
```

For derived values, inherit `DerivedReflectiveOptionAttribute<TOptions, TDerivationResult>`:

```csharp
using Rvne.ReflectiveOptions.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class DerivedTimeoutAttribute(string methodName)
    : DerivedReflectiveOptionAttribute<CommandOptions, int>(nameof(CommandOptions.TimeoutMs), methodName)
{
}
```

If you need complete control, you can implement the middleware interfaces directly:

```csharp
using Rvne.ReflectiveOptions.Interfaces;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class CustomTimeoutAttribute(int value) : Attribute, IStaticOptionMiddleware<CommandOptions>
{
    public void Apply(CommandOptions options) => options.TimeoutMs = value;
}
```

### 2) Annotate your source type

```csharp
public enum ExecutionProfile
{
    Local,
    CI
}

[DerivedTimeout(nameof(ComputeTimeout))]
[Retries(2)]
public sealed class WarmCacheCommand
{
    public ExecutionProfile Profile { get; set; }

    private int ComputeTimeout()
        => Profile == ExecutionProfile.CI ? 90_000 : 20_000;
}
```

### 3) Calculate options

```csharp
var command = new WarmCacheCommand { Profile = ExecutionProfile.CI };
var options = command.GetOptions<CommandOptions>();
// options.TimeoutMs == 90_000
// options.Retries == 2
```

## Derivation rules

- The derivation method name is provided by the attribute and looked up via reflection.
- Methods can be instance or static, must be parameterless, and must return a value (non-void).
- If the method returns `null` for a non-nullable value type, an exception is thrown.
- If the method returns `null` for a non-nullable reference type, an exception is thrown.
- The returned value must be assignable to the option member referenced by the attribute.

## Declaring instance

You can provide a parent object for method lookup by using `GetMemberOptions` on the context:

```csharp
public sealed class CliRoot
{
    public ExecutionProfile Profile { get; set; }

    private int ComputeTimeout()
        => Profile == ExecutionProfile.CI ? 90_000 : 20_000;

    [DerivedTimeout(nameof(ComputeTimeout))]
    public WarmCacheCommand Warm { get; } = new();
}

var root = new CliRoot { Profile = ExecutionProfile.CI };
var options = root.GetMemberOptions<CommandOptions>(nameof(CliRoot.Warm));
// options.TimeoutMs == 90_000
```

If you already have the source instance and want to override the declaring instance, you can pass it directly:

```csharp
var command = new WarmCacheCommand { Profile = ExecutionProfile.Local };
var context = new CliRoot { Profile = ExecutionProfile.CI };
var options = command.GetOptions<CommandOptions>(context);
// options.TimeoutMs == 90_000
```

Attributes are discovered with `inherit: true`, so base-class attributes are applied.

## Mixing option domains

You can map different option types to different parts of the same tree. For example, a container can carry command options while a child command carries output options:

```csharp
using Rvne.ReflectiveOptions.Attributes;

public sealed class OutputOptions
{
    public string Format { get; set; } = "text";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class FormatAttribute(string value)
    : ReflectiveOptionAttribute<OutputOptions, string>(nameof(OutputOptions.Format), value)
{
}

[Retries(1)]
public sealed class ExportGroup
{
    [Format("json")]
    public ExportCommand Export { get; } = new();
}

public sealed class ExportCommand
{
}

var group = new ExportGroup();
var commandOptions = group.GetOptions<CommandOptions>();
var outputOptions = group.GetMemberOptions<OutputOptions>(nameof(ExportGroup.Export));
// commandOptions.Retries == 1
// outputOptions.Format == "json"
```

### Member-level attributes

`GetMemberOptions` applies only the attributes defined on the named member (property/field). It does not read attributes from the member's value type. Use `GetOptions` on the value itself if you need the value's own attributes applied.
