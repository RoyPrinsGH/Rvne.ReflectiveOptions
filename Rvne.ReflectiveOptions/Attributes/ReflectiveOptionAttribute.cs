using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions.Attributes;

public abstract class ReflectiveOptionAttribute<TOptions, TMember>(string memberName, TMember memberType)
    : Attribute, IStaticValueProvider<TMember>, IOptionMiddleware<TOptions, TMember>
{
    public TMember Value { get; } = memberType;
    public void Apply(TOptions options, TMember value)
        => OptionMemberReflectionHelpers.Apply(options, memberName, value);
}