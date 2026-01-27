using Rvne.ReflectiveOptions.Attributes;

namespace Rvne.ReflectiveOptions.Tests.Attributes;

public sealed class ReflectiveOptionAttributeTests
{
    [Fact]
    public void ReflectiveOptionAttribute_Assigns_Property_Value()
    {
        var options = new PropertyAttributedSource().GetOptions<ExampleOptions>();

        Assert.Equal(50, options.TimeoutMs);
    }

    [Fact]
    public void ReflectiveOptionAttribute_Assigns_Field_Value()
    {
        var options = new FieldAttributedSource().GetOptions<ExampleOptions>();

        Assert.Equal(4, options.RetryCount);
    }

    private sealed class ExampleOptions
    {
        public int TimeoutMs { get; set; }
        public int RetryCount = -1;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class TimeoutAttribute(int value)
        : ReflectiveOptionAttribute<ExampleOptions, int>(nameof(ExampleOptions.TimeoutMs), value)
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class RetriesAttribute(int value)
        : ReflectiveOptionAttribute<ExampleOptions, int>(nameof(ExampleOptions.RetryCount), value)
    {
    }

    [Timeout(50)]
    private sealed class PropertyAttributedSource
    {
    }

    [Retries(4)]
    private sealed class FieldAttributedSource
    {
    }
}
