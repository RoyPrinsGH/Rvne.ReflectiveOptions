namespace Rvne.ReflectiveOptions.Attributes;

public interface IStaticValueProvider<TMember>
{
    TMember Value { get; }
}