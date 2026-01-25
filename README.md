# Rvne.ReflectiveOptions

ReflectiveOptions builds option objects from attributes on a source type. It is most useful for configuring complex trees of nested nodes (for example, a layout made of components that are themselves layouts). It supports:

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

public enum CarouselOrientation
{
    Horizontal,
    Vertical
}

public sealed class CarouselOptions
{
    public int Gap { get; set; }
    public CarouselOrientation Orientation { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class GapAttribute(int value) : Attribute, IOptionAttribute<CarouselOptions>
{
    public void Apply(CarouselOptions options) => options.Gap = value;
}
```

Use `DerivedOptionAttribute<TOptions, TResult>` for derived values:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class DerivedOrientationAttribute(string methodName)
    : DerivedOptionAttribute<CarouselOptions, CarouselOrientation>(methodName)
{
    public override void Apply(CarouselOrientation derivationResult, CarouselOptions options)
        => options.Orientation = derivationResult;
}
```

### 2) Annotate your source type

```csharp
[Gap(8)]
[DerivedOrientation(nameof(GetOrientation))]
public sealed class Carousel
{
    public bool IsNarrow { get; set; }

    private CarouselOrientation GetOrientation()
        => IsNarrow ? CarouselOrientation.Vertical : CarouselOrientation.Horizontal;
}
```

### 3) Calculate options

```csharp
var carousel = new Carousel { IsNarrow = true };
var options = carousel.CalculateOptions<CarouselOptions>();
// options.Gap == 8
// options.Orientation == CarouselOrientation.Vertical
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
    private CarouselOrientation ComputeOrientation()
        => CarouselOrientation.Horizontal;

    [DerivedOrientation(nameof(ComputeOrientation))]
    public sealed class Carousel
    {
    }
}

var dashboard = new Dashboard();
var carousel = new Dashboard.Carousel();
var options = carousel.CalculateOptions<CarouselOptions>(dashboard);
// options.Orientation == CarouselOrientation.Horizontal
```

If `derivationContext` is `null` or omitted, the source object is used.

Attributes are discovered with `inherit: true`, so base-class attributes are applied.
