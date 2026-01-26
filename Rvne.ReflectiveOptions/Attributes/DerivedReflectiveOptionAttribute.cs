using Rvne.ReflectiveOptions.Interfaces;
using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions.Attributes;

public abstract class DerivedReflectiveOptionAttribute<TOptions, TDerivationResult>(string memberName, string derivationMethod)
    : Attribute, IDerivedOptionMiddleware<TOptions>
{
    public string DerivationMethod { get; } = derivationMethod;
    public void Apply(TOptions options, object? valueToAssign)
        => OptionMemberReflectionHelpers.Apply(options, memberName, valueToAssign);
}