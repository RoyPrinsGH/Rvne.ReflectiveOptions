using Rvne.ReflectiveOptions.Interfaces;
using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions.Attributes;

public abstract class ReflectiveOptionAttribute<TOptions, TMember>(string memberName, TMember staticValue)
    : Attribute, IStaticOptionMiddleware<TOptions>
{
    public void Apply(TOptions options)
        => OptionMemberReflectionHelpers.Apply(options, memberName, staticValue);
}