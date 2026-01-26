namespace Rvne.ReflectiveOptions.Attributes;

public interface IDerivedValueProvider<TDerivationResult>
{
    string DerivationMethod { get; }
}