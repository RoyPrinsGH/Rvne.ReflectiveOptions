namespace Rvne.ReflectiveOptions.Tests;

using System;
using System.IO;
using Rvne.ReflectiveOptions;
using Xunit;

public sealed class ReflectiveOptionsTests
{
    [Fact]
    public void CalculateOptions_Applies_CompileTime_Attributes()
    {
        // BasicLayoutElement is decorated with compile-time attributes that directly implement
        // IOptionAttribute<LayoutOptions>. CalculateOptions should apply them without any
        // derivation step, producing a preconfigured LayoutOptions instance.
        var options = new BasicLayoutElement().GetOptions<LayoutOptions>();

        Assert.Equal(5, options.Gap);
        Assert.Equal("alpha", options.LayoutName);
    }

    [Fact]
    public void CalculateOptions_Applies_DerivedOptions_From_Instance_Method()
    {
        // The DerivedGap attribute points at a private instance method on the element.
        // Reflection should invoke that method on the source instance and use its result.
        var options = new DerivedGapElement().GetOptions<LayoutOptions>();

        Assert.Equal(42, options.Gap);
    }

    [Fact]
    public void CalculateOptions_Applies_DerivedOptions_From_Static_Method()
    {
        // The derived gap method is static here. The builder should still find it and
        // invoke it without an instance, then assign the returned value to the option.
        var options = new DerivedStaticGapElement().GetOptions<LayoutOptions>();

        Assert.Equal(13, options.Gap);
    }

    [Fact]
    public void CalculateMemberOptions_Uses_DerivationContext_When_Provided()
    {
        // The nested element is decorated with a derived columns attribute whose method
        // exists on the parent context. Using CalculateMemberOptions should route the
        // reflective lookup to that context type.
        var parent = new LayoutContext();
        var options = parent.GetMemberOptions<LayoutOptions>(nameof(LayoutContext.Nested));

        Assert.Equal(77, options.Columns);
    }

    [Fact]
    public void CalculateMemberOptions_Applies_Attributes_From_Context_Member()
    {
        // A component can be decorated at the member level in a parent container. When the
        // container is used as the derivation context, those member attributes should apply.
        var container = new ComponentContainer();

        var options = container.GetMemberOptions<LayoutOptions>(nameof(ComponentContainer.DemoButton));

        Assert.Equal(10, options.Gap);
    }

    [Fact]
    public void CalculateMemberOptions_Applies_Attributes_From_ValueType_Member()
    {
        // Member-level attributes are applied by name, so value type members work too.
        var container = new StructContainer();

        var options = container.GetMemberOptions<LayoutOptions>(nameof(StructContainer.StructButton));

        Assert.Equal(22, options.Gap);
    }

    [Fact]
    public void CalculateOptions_Uses_Source_As_Context()
    {
        // CalculateOptions always uses the source instance as the context for reflective
        // method lookup.
        var source = new DerivedGapElement();

        var options = source.GetOptions<LayoutOptions>();

        Assert.Equal(42, options.Gap);
    }

    [Fact]
    public void CalculateOptions_Uses_Explicit_DerivationContext()
    {
        // The derivation method exists on the explicit context, even if the source has a
        // matching method. The explicit context should win.
        var source = new ExplicitContextElement();
        IColumnsProvider derivationContext = new ColumnsProvider();

        var options = source.GetOptions<LayoutOptions>(derivationContext);

        Assert.Equal(88, options.Columns);
        Assert.Equal(3, options.Gap);
    }

    [Fact]
    public void CalculateOptions_Throws_When_Source_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ReflectiveOptionsExtensions.GetOptions<LayoutOptions>(null!));
    }

    [Fact]
    public void CalculateOptions_Throws_When_DerivationContext_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ExplicitContextElement().GetOptions<LayoutOptions>(null!));
    }

    [Fact]
    public void CalculateMemberOptions_Throws_When_DerivationContext_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ReflectiveOptionsExtensions.GetMemberOptions<LayoutOptions>(null!, nameof(LayoutContext.Nested)));
    }

    [Fact]
    public void CalculateMemberOptions_Throws_When_MemberName_Missing()
    {
        var parent = new LayoutContext();

        Assert.Throws<ArgumentException>(() => parent.GetMemberOptions<LayoutOptions>(string.Empty));
    }

    [Fact]
    public void CalculateMemberOptions_Throws_When_Member_Missing()
    {
        var parent = new LayoutContext();

        Assert.Throws<MissingMemberException>(() => parent.GetMemberOptions<LayoutOptions>("Missing"));
    }

    [Fact]
    public void CalculateMemberOptions_Throws_When_Member_Not_Readable()
    {
        var parent = new WriteOnlyContainer();

        Assert.Throws<InvalidOperationException>(() => parent.GetMemberOptions<LayoutOptions>(nameof(WriteOnlyContainer.WriteOnly)));
    }

    [Fact]
    public void CalculateMemberOptions_Throws_When_Member_Is_Indexer()
    {
        var parent = new IndexedContainer();

        Assert.Throws<InvalidOperationException>(() => parent.GetMemberOptions<LayoutOptions>("Item"));
    }

    [Fact]
    public void CalculateMemberOptions_Applies_Attributes_From_Context_Field()
    {
        var container = new FieldContainer();

        var options = container.GetMemberOptions<LayoutOptions>(nameof(FieldContainer.FieldButton));

        Assert.Equal(17, options.Gap);
    }

    [Fact]
    public void CalculateOptions_Uses_Inherited_Attributes()
    {
        // The base class provides attributes (including a derived attribute that calls a
        // protected method). The derived class should inherit and apply those attributes,
        // then combine them with its own compile-time gap override.
        var options = new InheritedLayoutElement().GetOptions<LayoutOptions>();

        Assert.Equal("base", options.LayoutName);
        Assert.Equal(9, options.Columns);
        Assert.Equal(9, options.Gap);
    }

    [Fact]
    public void CalculateOptions_Ignores_Attributes_For_Other_Options_Types()
    {
        // This element includes an attribute targeting TypographyOptions. Since we're
        // calculating LayoutOptions, that attribute should be ignored entirely.
        var options = new IrrelevantAttributeElement().GetOptions<LayoutOptions>();

        Assert.Equal(1, options.Gap);
    }

    [Fact]
    public void CalculateOptions_Handles_Derived_Attribute_Through_Intermediate_Base_Class()
    {
        // The attribute inherits from an intermediate base class which in turn inherits
        // DerivedOptionAttribute. The builder should walk base types to detect the generic
        // DerivedOptionAttribute and still apply the derivation.
        var options = new IndirectDerivedGapElement().GetOptions<LayoutOptions>();

        Assert.Equal(100, options.Gap);
    }

    [Fact]
    public void CalculateOptions_Allows_Null_For_ReferenceType_Result()
    {
        // The derivation method returns null for a reference type (string?). This is a
        // valid result and should flow through without an exception.
        var options = new NullLayoutNameElement().GetOptions<LayoutOptions>();

        Assert.True(options.WasNullLayoutName);
    }

    [Fact]
    public void CalculateOptions_Throws_When_Null_Returned_For_NonNullable_ReferenceType_Result()
    {
        // The derivation method claims a non-nullable reference type but returns null.
        Assert.Throws<InvalidOperationException>(() => new NonNullableLayoutNameElement().GetOptions<LayoutOptions>());
    }

    [Fact]
    public void CalculateOptions_Allows_Null_For_Nullable_ValueType_Result()
    {
        // The derivation method returns null for a Nullable<int>. This should be accepted
        // and stored as null without throwing.
        var options = new NullablePaddingElement().GetOptions<LayoutOptions>();

        Assert.True(options.WasNullPadding);
        Assert.Null(options.Padding);
    }

    [Fact]
    public void CalculateOptions_Accepts_Derived_Result_Types()
    {
        // The attribute expects a Stream, but the method returns a MemoryStream. Because
        // MemoryStream is assignable to Stream, the result should be accepted.
        var options = new AssetStreamElement().GetOptions<LayoutOptions>();

        Assert.IsType<MemoryStream>(options.AssetStream);
    }

    [Fact]
    public void CalculateOptions_Throws_When_Derivation_Method_Missing()
    {
        // The attribute points at a method name that doesn't exist on the context type,
        // so reflection should fail with a MissingMethodException.
        Assert.Throws<MissingMethodException>(() => new MissingMethodElement().GetOptions<LayoutOptions>());
    }

    [Fact]
    public void CalculateOptions_Throws_When_Derivation_Method_Has_Parameters()
    {
        // A derivation method exists, but it has parameters. The builder requires a
        // parameterless method, so this should throw.
        Assert.Throws<MissingMethodException>(() => new ParameterMethodElement().GetOptions<LayoutOptions>());
    }

    [Fact]
    public void CalculateOptions_Throws_When_Derivation_Method_Returns_Void()
    {
        // A derivation method that returns void is invalid because the builder needs
        // a value to apply. Expect an InvalidOperationException.
        Assert.Throws<InvalidOperationException>(() => new VoidMethodElement().GetOptions<LayoutOptions>());
    }

    [Fact]
    public void CalculateOptions_Throws_When_Null_Returned_For_NonNullable_ValueType()
    {
        // The attribute expects an int, but the method returns null. Null is not valid
        // for non-nullable value types, so this should throw.
        Assert.Throws<InvalidOperationException>(() => new NullForNonNullableGapElement().GetOptions<LayoutOptions>());
    }

    [Fact]
    public void CalculateOptions_Throws_When_Derivation_Result_Type_Mismatch()
    {
        // The attribute expects an int, but the method returns a string. The result type
        // is not assignable, so the builder should throw.
        Assert.Throws<InvalidOperationException>(() => new TypeMismatchElement().GetOptions<LayoutOptions>());
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    private sealed class GapAttribute(int value)
        : ReflectiveOptionAttribute<LayoutOptions, int>(nameof(LayoutOptions.Gap), value)
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class LayoutNameAttribute(string value) : Attribute, IOptionAttribute<LayoutOptions>
    {
        public void Apply(LayoutOptions options) => options.LayoutName = value;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class DerivedGapAttribute(string methodName) : DerivedOptionAttribute<LayoutOptions, int>(methodName)
    {
        public override void Apply(int derivationResult, LayoutOptions options) => options.Gap = derivationResult;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    private sealed class DerivedColumnsAttribute(string methodName) : DerivedOptionAttribute<LayoutOptions, int>(methodName)
    {
        public override void Apply(int derivationResult, LayoutOptions options) => options.Columns = derivationResult;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class DerivedPaddingAttribute(string methodName) : DerivedOptionAttribute<LayoutOptions, int?>(methodName)
    {
        public override void Apply(int? derivationResult, LayoutOptions options)
        {
            options.Padding = derivationResult;
            options.WasNullPadding = derivationResult is null;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class DerivedLayoutNameAttribute(string methodName) : DerivedOptionAttribute<LayoutOptions, string?>(methodName)
    {
        public override void Apply(string? derivationResult, LayoutOptions options) =>
            options.WasNullLayoutName = derivationResult is null;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class DerivedLayoutNameNonNullAttribute(string methodName) : DerivedOptionAttribute<LayoutOptions, string>(methodName)
    {
        public override void Apply(string derivationResult, LayoutOptions options) =>
            options.LayoutName = derivationResult;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class DerivedAssetStreamAttribute(string methodName) : DerivedOptionAttribute<LayoutOptions, Stream>(methodName)
    {
        public override void Apply(Stream derivationResult, LayoutOptions options) => options.AssetStream = derivationResult;
    }

    private abstract class BaseDerivedGapAttribute(string methodName) : DerivedOptionAttribute<LayoutOptions, int>(methodName)
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class IndirectDerivedGapAttribute(string methodName) : BaseDerivedGapAttribute(methodName)
    {
        public override void Apply(int derivationResult, LayoutOptions options) => options.Gap = derivationResult;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    private sealed class IrrelevantDerivedAttribute(string methodName) : DerivedOptionAttribute<TypographyOptions, int>(methodName)
    {
        public override void Apply(int derivationResult, TypographyOptions options) => options.FontSize = derivationResult;
    }

    private sealed class LayoutOptions
    {
        public int Gap { get; set; }
        public string? LayoutName { get; set; }
        public int Columns { get; set; }
        public int? Padding { get; set; }
        public bool WasNullLayoutName { get; set; }
        public bool WasNullPadding { get; set; }
        public Stream? AssetStream { get; set; }
        public int Priority { get; set; }
    }

    private sealed class TypographyOptions
    {
        public int FontSize { get; set; }
    }

    [Gap(5)]
    [LayoutName("alpha")]
    private sealed class BasicLayoutElement
    {
    }

    [DerivedGap(nameof(CalculateGap))]
    private sealed class DerivedGapElement
    {
        private int CalculateGap() => 42;
    }

    [DerivedGap(nameof(CalculateStaticGap))]
    private sealed class DerivedStaticGapElement
    {
        private static int CalculateStaticGap() => 13;
    }

    private sealed class LayoutContext
    {
        private int CalculateColumns() => 77;

        [DerivedColumns(nameof(CalculateColumns))]
        public NestedElement Nested { get; } = new();

        public sealed class NestedElement
        {
        }
    }

    [LayoutName("base")]
    [DerivedColumns(nameof(GetBaseColumns))]
    private class InheritedLayoutBase
    {
        protected int GetBaseColumns() => 9;
    }

    [Gap(9)]
    private sealed class InheritedLayoutElement : InheritedLayoutBase
    {
    }

    private sealed class ComponentContainer
    {
        [Gap(10)]
        public MyButton DemoButton { get; set; } = new();
    }

    private sealed class MyButton
    {
    }

    private sealed class StructContainer
    {
        [Gap(22)]
        public StructButton StructButton { get; set; }
    }

    private struct StructButton
    {
    }

    [Gap(1)]
    [IrrelevantDerived(nameof(IrrelevantMethod))]
    private sealed class IrrelevantAttributeElement
    {
        private static int IrrelevantMethod() => 0;
    }

    [IndirectDerivedGap(nameof(GetIndirectGap))]
    private sealed class IndirectDerivedGapElement
    {
        private int GetIndirectGap() => 100;
    }

    [Gap(3)]
    [DerivedColumns(nameof(IColumnsProvider.GetColumns))]
    private sealed class ExplicitContextElement
    {
    }

    private interface IColumnsProvider
    {
        int GetColumns();
    }

    private sealed class ColumnsProvider : IColumnsProvider
    {
        public int GetColumns() => 88;
    }

    private sealed class WriteOnlyContainer
    {
        public MyButton WriteOnly
        {
            set
            {
            }
        }
    }

    private sealed class IndexedContainer
    {
        public MyButton this[int index] => new();
    }

    private sealed class FieldContainer
    {
        [Gap(17)]
        public MyButton FieldButton = new();
    }

    [DerivedLayoutName(nameof(GetNullLayoutName))]
    private sealed class NullLayoutNameElement
    {
        private string? GetNullLayoutName() => null;
    }

    [DerivedLayoutNameNonNull(nameof(GetLayoutName))]
    private sealed class NonNullableLayoutNameElement
    {
        private string GetLayoutName() => null!;
    }

    [DerivedPadding(nameof(GetPadding))]
    private sealed class NullablePaddingElement
    {
        private int? GetPadding() => null;
    }

    [DerivedAssetStream(nameof(GetStream))]
    private sealed class AssetStreamElement
    {
        private Stream GetStream() => new MemoryStream();
    }

    [DerivedGap("Missing")]
    private sealed class MissingMethodElement
    {
    }

    [DerivedGap(nameof(GetWithParameter))]
    private sealed class ParameterMethodElement
    {
        private int GetWithParameter(int value) => value;
    }

    [DerivedGap(nameof(GetVoid))]
    private sealed class VoidMethodElement
    {
        private void GetVoid()
        {
        }
    }

    [DerivedGap(nameof(GetNullableGap))]
    private sealed class NullForNonNullableGapElement
    {
        private int? GetNullableGap() => null;
    }

    [DerivedGap(nameof(GetWrongType))]
    private sealed class TypeMismatchElement
    {
        private string GetWrongType() => "nope";
    }
}
