namespace Rvne.ReflectiveOptions.Interfaces;

public interface IStaticOptionMiddleware<TOptions>
{
    void Apply(TOptions options);
}