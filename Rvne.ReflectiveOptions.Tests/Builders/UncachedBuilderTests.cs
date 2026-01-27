using Rvne.ReflectiveOptions.Interfaces;
using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions.Tests.Builders;

public sealed class UncachedBuilderTests
{
    [Fact]
    public void BuildFrom_Applies_Multiple_Attributes()
    {
        var attributes = new Attribute[]
        {
            new TimeoutAttribute(10),
            new DerivedTimeoutAttribute(nameof(DerivationContext.ComputeTimeout)),
            new NoiseAttribute()
        };

        var options = UncachedBuilder.BuildFrom<ExampleOptions>(attributes, new DerivationContext());

        Assert.Equal(30, options.TimeoutMs);
    }

    [Fact]
    public void BuildFrom_Leaves_Defaults_When_No_Matching_Attributes()
    {
        var attributes = new Attribute[]
        {
            new NoiseAttribute()
        };

        var options = UncachedBuilder.BuildFrom<ExampleOptions>(attributes, new DerivationContext());

        Assert.Equal(0, options.TimeoutMs);
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
    private sealed class TimeoutAttribute(int value) : Attribute, IStaticOptionMiddleware<ExampleOptions>
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

    private sealed class DerivationContext
    {
        public static int ComputeTimeout() => 30;
    }
}
