using Rvne.ReflectiveOptions.Attributes;

namespace Rvne.ReflectiveOptions.Tests.Attributes;

public sealed class DerivedReflectiveOptionAttributeTests
{
    [Fact]
    public void DerivedReflectiveOptionAttribute_Assigns_Derived_Value()
    {
        var source = new DerivedPayloadSource();

        var options = source.GetOptions<ExampleOptions>();

        Assert.Equal("payload", options.Payload);
    }

    private sealed class ExampleOptions
    {
        public string? Payload { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class DerivedPayloadAttribute(string methodName)
        : DerivedReflectiveOptionAttribute<ExampleOptions, object>(nameof(ExampleOptions.Payload), methodName)
    {
    }

    [DerivedPayload(nameof(ComputePayload))]
    private sealed class DerivedPayloadSource
    {
        private static string ComputePayload() => "payload";
    }
}
