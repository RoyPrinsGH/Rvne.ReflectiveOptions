using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions;

// TODO: Summary
internal static class UncachedBuilder
{
    internal static TOptions BuildFrom<TOptions>(IEnumerable<Attribute> customAttributes, object derivationContext)
        where TOptions : new() => customAttributes.Aggregate(new TOptions(),
            (opt, attr) => OptionAttributeResolutionHelpers.TryApplyAttribute(attr, opt, derivationContext));
}
