using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions;

/// <summary>
/// Builds option instances by applying attributes without caching the result.
/// </summary>
internal static class UncachedBuilder
{
    internal static TOptions BuildFrom<TOptions>(IEnumerable<Attribute> customAttributes, object derivationContext)
        where TOptions : new() => customAttributes.Aggregate(new TOptions(),
            (opt, attr) => OptionAttributeResolutionHelpers.TryApplyAttribute(attr, opt, derivationContext));
}
