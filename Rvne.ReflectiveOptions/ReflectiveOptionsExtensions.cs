using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions;

// TODO: Summaries for everything in this file

public static class ReflectiveOptionsExtensions
{
    public static TOptions GetOptions<TOptions>(this object source)
        where TOptions : new()
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.GetOptions<TOptions>(source);
    }

    public static TOptions GetOptions<TOptions>(this object source, object derivationContext)
        where TOptions : new()
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(derivationContext);

        IEnumerable<Attribute> sourceAttributes
            = source.GetType()
                .GetCustomAttributes(inherit: true)
                .Cast<Attribute>();

        return UncachedBuilder.BuildFrom<TOptions>(sourceAttributes, derivationContext);
    }

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