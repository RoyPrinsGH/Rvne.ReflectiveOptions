using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions;

/// <summary>
/// Extension methods for deriving options from attributes.
/// </summary>
public static class ReflectiveOptionsExtensions
{
    /// <summary>
    /// Builds options from the attributes on the source type, using the source as the declaring instance.
    /// </summary>
    public static TOptions GetOptions<TOptions>(this object source)
        where TOptions : new()
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.GetOptions<TOptions>(source);
    }

    /// <summary>
    /// Builds options from the attributes on the source type using the provided declaring instance.
    /// </summary>
    public static TOptions GetOptions<TOptions>(this object source, object declaringInstance)
        where TOptions : new()
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(declaringInstance);

        IEnumerable<Attribute> sourceAttributes
            = source.GetType()
                .GetCustomAttributes(inherit: true)
                .Cast<Attribute>();

        return UncachedBuilder.BuildFrom<TOptions>(sourceAttributes, declaringInstance);
    }

    /// <summary>
    /// Builds options from the attributes applied to a specific member on the declaring instance.
    /// </summary>
    public static TOptions GetMemberOptions<TOptions>(this object derivationContext, string memberName)
        where TOptions : new()
    {
        ArgumentNullException.ThrowIfNull(derivationContext);

        if (string.IsNullOrWhiteSpace(memberName))
            throw new ArgumentException("Member name must be provided.", nameof(memberName));

        IEnumerable<Attribute> memberAttributes
            = OptionMemberReflectionHelpers.FindMemberByName(derivationContext.GetType(), memberName)
                .GetCustomAttributes(inherit: true)
                .Cast<Attribute>();

        return UncachedBuilder.BuildFrom<TOptions>(memberAttributes, derivationContext);
    }
}
