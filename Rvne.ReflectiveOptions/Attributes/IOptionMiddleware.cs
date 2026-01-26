namespace Rvne.ReflectiveOptions.Attributes;

public interface IOptionMiddleware<TOptions, TMember>
{
    void Apply(TOptions options, TMember value);
}