namespace Rvne.ReflectiveOptions.Interfaces;

public interface IDerivedOptionMiddleware<TOptions>
{
    string DerivationMethod { get; }
    void Apply(TOptions options, object? value);
}