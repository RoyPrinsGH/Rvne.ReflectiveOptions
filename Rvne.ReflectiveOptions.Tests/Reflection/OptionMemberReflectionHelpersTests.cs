using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions.Tests.Reflection;

public sealed class OptionMemberReflectionHelpersTests
{
    [Fact]
    public void Apply_Sets_Property_Value()
    {
        var options = new ExampleOptions();

        OptionMemberReflectionHelpers.Apply(options, nameof(ExampleOptions.TimeoutMs), 123);

        Assert.Equal(123, options.TimeoutMs);
    }

    [Fact]
    public void Apply_Sets_Field_Value()
    {
        var options = new ExampleOptions();

        OptionMemberReflectionHelpers.Apply(options, nameof(ExampleOptions.RetryCount), 3);

        Assert.Equal(3, options.RetryCount);
    }

    [Fact]
    public void Apply_Throws_For_Missing_Member()
    {
        var options = new ExampleOptions();

        Assert.Throws<MissingMemberException>(() =>
            OptionMemberReflectionHelpers.Apply(options, "MissingMember", 1));
    }

    [Fact]
    public void Apply_Throws_For_ReadOnly_Property()
    {
        var options = new ExampleOptions();

        Assert.Throws<InvalidOperationException>(() =>
            OptionMemberReflectionHelpers.Apply(options, nameof(ExampleOptions.ReadOnlyTimeout), 10));
    }

    [Fact]
    public void Apply_Throws_For_Indexer_Property()
    {
        var options = new ExampleOptions();

        Assert.Throws<InvalidOperationException>(() =>
            OptionMemberReflectionHelpers.Apply(options, "Item", 5));
    }

    [Fact]
    public void Apply_Throws_For_NonAssignable_Value()
    {
        var options = new ExampleOptions();

        Assert.Throws<InvalidOperationException>(() =>
            OptionMemberReflectionHelpers.Apply(options, nameof(ExampleOptions.TimeoutMs), "not-an-int"));
    }

    [Fact]
    public void ThrowIfNotAssignable_Rejects_Null_For_NonNullable_ValueType()
    {
        var member = typeof(ExampleOptions).GetProperty(nameof(ExampleOptions.TimeoutMs))!;

        Assert.Throws<InvalidOperationException>(() =>
            OptionMemberReflectionHelpers.ThrowIfNotAssignable(member, null));
    }

    [Fact]
    public void ThrowIfNotAssignable_Allows_Null_For_Nullable_ValueType()
    {
        var member = typeof(ExampleOptions).GetProperty(nameof(ExampleOptions.OptionalTimeout))!;

        OptionMemberReflectionHelpers.ThrowIfNotAssignable(member, null);
    }

    [Fact]
    public void ThrowIfNotAssignable_Rejects_Null_For_NonNullable_ReferenceType()
    {
        var member = typeof(ExampleOptions).GetProperty(nameof(ExampleOptions.Name))!;

        Assert.Throws<InvalidOperationException>(() =>
            OptionMemberReflectionHelpers.ThrowIfNotAssignable(member, null));
    }

    [Fact]
    public void ThrowIfNotAssignable_Allows_Null_For_Nullable_ReferenceType()
    {
        var member = typeof(ExampleOptions).GetProperty(nameof(ExampleOptions.Description))!;

        OptionMemberReflectionHelpers.ThrowIfNotAssignable(member, null);
    }

    [Fact]
    public void ThrowIfNotAssignable_Allows_Assignable_Derived_Type()
    {
        var member = typeof(ExampleOptions).GetProperty(nameof(ExampleOptions.Pet))!;

        OptionMemberReflectionHelpers.ThrowIfNotAssignable(member, typeof(Dog));
    }

    [Fact]
    public void FindMemberByName_Finds_Members_On_Base_Types()
    {
        var member = OptionMemberReflectionHelpers.FindMemberByName(typeof(DerivedOptions), nameof(BaseOptions.TimeoutMs));

        Assert.Equal(nameof(BaseOptions.TimeoutMs), member.Name);
    }

    private sealed class ExampleOptions
    {
        public int TimeoutMs { get; set; }
        public int? OptionalTimeout { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int RetryCount = -1;
        public int ReadOnlyTimeout { get; } = 5;
        public int this[int index]
        {
            get => 0;
            set { }
        }
        public Animal? Pet { get; set; }
    }

    private class BaseOptions
    {
        public int TimeoutMs { get; set; }
    }

    private sealed class DerivedOptions : BaseOptions
    {
    }

    private abstract class Animal
    {
    }

    private sealed class Dog : Animal
    {
    }
}
