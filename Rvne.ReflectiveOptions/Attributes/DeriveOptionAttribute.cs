namespace Rvne.ReflectiveOptions.Attributes;

public abstract class DerivedOptionAttribute<TOptions, TDerivationResult>(string derivationMethodName) : Attribute
{
    public string DerivationMethodName { get; } = derivationMethodName;
    public abstract void Apply(TDerivationResult derivationResult, TOptions options);
}