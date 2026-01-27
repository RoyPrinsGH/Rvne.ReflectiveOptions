using Rvne.ReflectiveOptions.Interfaces;
using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions.Tests.Reflection;

public sealed class OptionAttributeResolutionHelpersTests
{
    [Fact]
    public void TryApplyAttribute_Ignores_Unrelated_Attributes()
    {
        var options = new ExampleOptions();

        OptionAttributeResolutionHelpers.TryApplyAttribute(new NoiseAttribute(), options, new ExampleContext());

        Assert.Equal(0, options.TimeoutMs);
    }

    [Fact]
    public void TryApplyAttribute_Applies_Static_Middleware()
    {
        var options = new ExampleOptions();

        OptionAttributeResolutionHelpers.TryApplyAttribute(new StaticTimeoutAttribute(42), options, new ExampleContext());

        Assert.Equal(42, options.TimeoutMs);
    }

    [Fact]
    public void TryApplyAttribute_Applies_Derived_Middleware_From_Instance_Method()
    {
        var options = new ExampleOptions();
        var context = new ExampleContext();

        OptionAttributeResolutionHelpers.TryApplyAttribute(new DerivedTimeoutAttribute(nameof(ExampleContext.ComputeTimeout)), options, context);

        Assert.Equal(155, options.TimeoutMs);
    }

    [Fact]
    public void TryApplyAttribute_Applies_Derived_Middleware_From_Static_Method()
    {
        var options = new ExampleOptions();
        var context = new ExampleContext();

        OptionAttributeResolutionHelpers.TryApplyAttribute(new DerivedTimeoutAttribute(nameof(ExampleContext.ComputeStaticTimeout)), options, context);

        Assert.Equal(777, options.TimeoutMs);
    }

    [Fact]
    public void TryApplyAttribute_Throws_When_Method_Missing()
    {
        var options = new ExampleOptions();

        Assert.Throws<MissingMethodException>(() =>
            OptionAttributeResolutionHelpers.TryApplyAttribute(new DerivedTimeoutAttribute("Missing"), options, new ExampleContext()));
    }

    [Fact]
    public void TryApplyAttribute_Throws_When_Method_Returns_Void()
    {
        var options = new ExampleOptions();

        Assert.Throws<InvalidOperationException>(() =>
            OptionAttributeResolutionHelpers.TryApplyAttribute(new DerivedTimeoutAttribute(nameof(ExampleContext.ComputeVoid)), options, new ExampleContext()));
    }

    [Fact]
    public void TryApplyAttribute_Throws_When_Method_Has_Parameters()
    {
        var options = new ExampleOptions();

        Assert.Throws<MissingMethodException>(() =>
            OptionAttributeResolutionHelpers.TryApplyAttribute(new DerivedTimeoutAttribute(nameof(ExampleContext.ComputeWithParameter)), options, new ExampleContext()));
    }

    private sealed class ExampleOptions
    {
        public int TimeoutMs { get; set; }
    }

    [AttributeUsage(AttributeTargets.All)]
    private sealed class NoiseAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All)]
    private sealed class StaticTimeoutAttribute(int value) : Attribute, IStaticOptionMiddleware<ExampleOptions>
    {
        public void Apply(ExampleOptions options) => options.TimeoutMs = value;
    }

    [AttributeUsage(AttributeTargets.All)]
    private sealed class DerivedTimeoutAttribute(string methodName) : Attribute, IDerivedOptionMiddleware<ExampleOptions>
    {
        public string DerivationMethod { get; } = methodName;

        public void Apply(ExampleOptions options, object? value)
            => OptionMemberReflectionHelpers.Apply(options, nameof(ExampleOptions.TimeoutMs), (int)value!);
    }

    private sealed class ExampleContext
    {
        public static int ComputeTimeout() => 155;
        public static int ComputeStaticTimeout() => 777;
        public static void ComputeVoid() { }
        public static int ComputeWithParameter(int value) => value;
    }
}
