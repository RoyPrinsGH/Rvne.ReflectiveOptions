# Rvne.ReflectiveOptions

ReflectiveOptions builds option objects from attributes on a source type. It supports:

- Compile-time attributes that directly apply values to an options instance.
- Derived attributes that compute values by invoking parameterless methods on a context object.
- Inheritance-aware attribute discovery.

## Requirements

- .NET 10 (TargetFramework: `net10.0`)

## Installation

If you keep the project in the same solution, add a project reference:

```bash
dotnet add <your-project>.csproj reference Rvne.ReflectiveOptions/Rvne.ReflectiveOptions.csproj
```

If you package it elsewhere, reference it however you normally consume your libraries.

## Usage

### 1) Define option attributes

Implement `IOptionAttribute<TOptions>` for compile-time application:

```csharp
using Rvne.ReflectiveOptions;

public sealed class LayoutOptions
{
    public int Gap { get; set; }
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class GapAttribute(int value) : Attribute, IOptionAttribute<LayoutOptions>
{
    public void Apply(LayoutOptions options) => options.Gap = value;
}
```

Use `DerivedOptionAttribute<TOptions, TResult>` for derived values:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class DerivedNameAttribute(string methodName)
    : DerivedOptionAttribute<LayoutOptions, string?>(methodName)
{
    public override void Apply(string? derivationResult, LayoutOptions options)
        => options.Name = derivationResult;
}
```

### 2) Annotate your source type

```csharp
[Gap(8)]
[DerivedName(nameof(GetName))]
public sealed class Card
{
    private string? GetName() => "primary";
}
```

### 3) Calculate options

```csharp
var options = new Card().CalculateOptions<LayoutOptions>();
// options.Gap == 8
// options.Name == "primary"
```

## Derivation rules

- The derivation method name is provided by the attribute and looked up via reflection.
- Methods must be parameterless and return a value (non-void).
- If the method returns `null` for a non-nullable value type, an exception is thrown.
- The returned value must be assignable to the result type specified on the attribute.

## Derivation context

You can provide a different object for method lookup by passing `derivationContext`:

```csharp
public sealed class Dashboard
{
    private string? ComputeName() => "primary";

    [DerivedName(nameof(ComputeName))]
    public sealed class Card
    {
    }
}

var dashboard = new Dashboard();
var card = new Dashboard.Card();
var options = card.CalculateOptions<LayoutOptions>(dashboard);
// options.Name == "primary"
```

If `derivationContext` is `null` or omitted, the source object is used.

## Notes

- Attributes are discovered with `inherit: true`, so base-class attributes are applied.
- The builder is reflection-based and does not cache results.

## Tests

```bash
dotnet test
```
