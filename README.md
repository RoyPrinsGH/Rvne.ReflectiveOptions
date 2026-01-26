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

Implement `IOptionAttribute<TOptions>` for compile-time application:

```csharp
using Rvne.ReflectiveOptions;

public sealed class CommandOptions
{
    public int TimeoutMs { get; set; } = 30_000;
    public int Retries { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class TimeoutAttribute(int value) : Attribute, IOptionAttribute<CommandOptions>
{
    public void Apply(CommandOptions options) => options.TimeoutMs = value;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class RetriesAttribute(int value) : Attribute, IOptionAttribute<CommandOptions>
{
    public void Apply(CommandOptions options) => options.Retries = value;
}
```

Use `DerivedOptionAttribute<TOptions, TResult>` for derived values:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class DerivedTimeoutAttribute(string methodName)
    : DerivedOptionAttribute<CommandOptions, int>(methodName)
{
    public override void Apply(int derivationResult, CommandOptions options)
        => options.TimeoutMs = derivationResult;
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
var options = command.CalculateOptions<CommandOptions>();
// options.TimeoutMs == 90_000
// options.Retries == 2
```

## Derivation rules

- The derivation method name is provided by the attribute and looked up via reflection.
- Methods must be parameterless and return a value (non-void).
- If the method returns `null` for a non-nullable value type, an exception is thrown.
- The returned value must be assignable to the result type specified on the attribute.

## Derivation context

You can provide a parent object for method lookup by using `CalculateOptionsFor` on the context:

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
var options = root.CalculateOptionsFor<CommandOptions>(nameof(CliRoot.Warm));
// options.TimeoutMs == 90_000
```

If you already have the source instance and want to override the derivation context, you can pass it directly:

```csharp
var command = new WarmCacheCommand { Profile = ExecutionProfile.Local };
var context = new CliRoot { Profile = ExecutionProfile.CI };
var options = command.CalculateOptions<CommandOptions>(context);
// options.TimeoutMs == 90_000
```

Attributes are discovered with `inherit: true`, so base-class attributes are applied.

## Mixing option domains

You can map different option types to different parts of the same tree. For example, a container can carry command options while a child command carries output options:

```csharp
public sealed class OutputOptions
{
    public string Format { get; set; } = "text";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class FormatAttribute(string value) : Attribute, IOptionAttribute<OutputOptions>
{
    public void Apply(OutputOptions options) => options.Format = value;
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
var commandOptions = group.CalculateOptions<CommandOptions>();
var outputOptions = group.CalculateOptionsFor<OutputOptions>(nameof(ExportGroup.Export));
// commandOptions.Retries == 1
// outputOptions.Format == "json"
```

### Value types and member-level attributes

Member-level attributes (on context properties/fields that point at the source object) rely on reference identity. That means they only work for reference types. For value types, there is no reliable way to tell which member you intended if multiple fields/properties share the same value. If you need member-level attributes, use reference types for those components.
