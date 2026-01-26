using System.Diagnostics.CodeAnalysis;
using Rvne.ReflectiveOptions.Attributes;
using Rvne.ReflectiveOptions.Interfaces;
using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions.Tests.Extensions;

public sealed class ReflectiveOptionsExtensionsTests
{
    [Fact]
    public void GetOptions_Throws_When_Source_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ReflectiveOptionsExtensions.GetOptions<ExampleOptions>(null!));
    }

    [Fact]
    public void GetOptions_Throws_When_Derivation_Context_Is_Null()
    {
        var source = new DerivedTimeoutSource();

        Assert.Throws<ArgumentNullException>(() =>
            ReflectiveOptionsExtensions.GetOptions<ExampleOptions>(source, null!));
    }

    [Fact]
    public void GetOptions_Uses_Source_As_Derivation_Context()
    {
        var source = new DerivedTimeoutSource();

        var options = source.GetOptions<ExampleOptions>();

        Assert.Equal(240, options.TimeoutMs);
    }

    [Fact]
    public void GetOptions_Uses_Provided_Derivation_Context()
    {
        var source = new DerivedTimeoutSource();
        var context = new AlternateDerivationContext();

        var options = source.GetOptions<ExampleOptions>(context);

        Assert.Equal(900, options.TimeoutMs);
    }

    [Fact]
    public void GetOptions_Applies_Inherited_Attributes()
    {
        var options = new DerivedFromBase().GetOptions<ExampleOptions>();

        Assert.Equal(321, options.TimeoutMs);
    }

    [Fact]
    public void GetMemberOptions_Applies_Only_Member_Attributes()
    {
        var context = new ContainerWithAttributedMember();

        var options = context.GetMemberOptions<ExampleOptions>(nameof(ContainerWithAttributedMember.Child));

        Assert.Equal(77, options.TimeoutMs);
    }

    [Fact]
    public void GetMemberOptions_Uses_Derivation_Context_For_Derived_Attributes()
    {
        var context = new ContainerWithDerivedMember();

        var options = context.GetMemberOptions<ExampleOptions>(nameof(ContainerWithDerivedMember.Child));

        Assert.Equal(555, options.TimeoutMs);
    }

    [Fact]
    public void GetMemberOptions_Throws_For_Blank_Name()
    {
        var context = new ContainerWithAttributedMember();

        Assert.Throws<ArgumentException>(() => context.GetMemberOptions<ExampleOptions>(" "));
    }

    [Fact]
    public void GetMemberOptions_Throws_When_Member_Missing()
    {
        var context = new ContainerWithAttributedMember();

        Assert.Throws<MissingMemberException>(() => context.GetMemberOptions<ExampleOptions>("Missing"));
    }

    private sealed class ExampleOptions
    {
        public int TimeoutMs { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    private sealed class TimeoutAttribute(int value)
        : ReflectiveOptionAttribute<ExampleOptions, int>(nameof(ExampleOptions.TimeoutMs), value)
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    private sealed class DerivedTimeoutAttribute(string methodName)
        : Attribute, IDerivedOptionMiddleware<ExampleOptions>
    {
        public string DerivationMethod { get; } = methodName;

        public void Apply(ExampleOptions options, object? value)
            => OptionMemberReflectionHelpers.Apply(options, nameof(ExampleOptions.TimeoutMs), (int)value!);
    }

    [Timeout(321)]
    private class BaseWithTimeout
    {
    }

    private sealed class DerivedFromBase : BaseWithTimeout
    {
    }

    [DerivedTimeout(nameof(ComputeTimeout))]
    private sealed class DerivedTimeoutSource
    {
        private static int ComputeTimeout() => 240;
    }

    private sealed class AlternateDerivationContext
    {
        private static int ComputeTimeout() => 900;
    }

    private sealed class ContainerWithAttributedMember
    {
        [Timeout(77)]
        public ChildWithTypeAttribute Child { get; } = new();
    }

    private sealed class ContainerWithDerivedMember
    {
        [DerivedTimeout(nameof(ComputeTimeout))]
        public ChildWithTypeAttribute Child { get; } = new();

        private static int ComputeTimeout() => 555;
    }

    [Timeout(12)]
    private sealed class ChildWithTypeAttribute
    {
    }
}
