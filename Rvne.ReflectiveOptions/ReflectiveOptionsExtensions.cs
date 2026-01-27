using System.Reflection;
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

        IEnumerable<Attribute> sourceAttributes = source.GetType()
            .GetCustomAttributes(inherit: true)
            .Cast<Attribute>();

        return UncachedBuilder.BuildFrom<TOptions>(sourceAttributes, declaringInstance);
    }

    /// <summary>
    /// Builds options from the attributes applied to a specific member on the declaring instance.
    /// </summary>
    public static TOptions GetMemberOptions<TOptions>(this object declaringInstance, string memberName)
        where TOptions : new()
    {
        ArgumentNullException.ThrowIfNull(declaringInstance);

        if (string.IsNullOrWhiteSpace(memberName))
            throw new ArgumentException("Member name must be provided.", nameof(memberName));

        IEnumerable<Attribute> memberAttributes
            = OptionMemberReflectionHelpers.FindMemberByName(declaringInstance.GetType(), memberName)
                .GetCustomAttributes(inherit: true)
                .Cast<Attribute>();

        return UncachedBuilder.BuildFrom<TOptions>(memberAttributes, declaringInstance);
    }

    /// <summary>
    /// Builds options from the attributes applied to a specific member on the declaring instance.
    /// </summary>
    public static TOptions GetMemberOptions<TOptions>(this object declaringInstance, MethodInfo methodInfo)
        where TOptions : new()
    {
        ArgumentNullException.ThrowIfNull(declaringInstance);
        ArgumentNullException.ThrowIfNull(methodInfo);

        IEnumerable<Attribute> memberAttributes = methodInfo
            .GetCustomAttributes(inherit: true)
            .Cast<Attribute>();

        return UncachedBuilder.BuildFrom<TOptions>(memberAttributes, declaringInstance);
    }
}
