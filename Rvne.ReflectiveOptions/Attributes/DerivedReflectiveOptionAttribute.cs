using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions.Attributes;

public abstract class DerivedReflectiveOptionAttribute<TOptions, TDerivationResult>(string memberName, string derivationMethod)
    : Attribute, IDerivedValueProvider<TDerivationResult>, IOptionMiddleware<TOptions, TDerivationResult>
{
    public string DerivationMethod { get; } = derivationMethod;
    public void Apply(TOptions options, TDerivationResult derivationResult)
        => OptionMemberReflectionHelpers.Apply(options, memberName, derivationResult);
}