namespace Rvne.ReflectiveOptions.Attributes;

// TODO: Explain these and their relation to the generators

public enum GenerationTarget
{
    Class = 0,
    Member = 1,
    All = 2
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateReflectiveOptionsAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InheritedAttribute(bool inherited) : Attribute
{
    public bool Inherited { get; } = inherited;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AllowMultipleAttribute(bool allowMultiple) : Attribute
{
    public bool AllowMultiple { get; } = allowMultiple;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ApplicableOnAttribute(GenerationTarget target) : Attribute
{
    public GenerationTarget Target { get; } = target;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AliasAttribute(string alias) : Attribute
{
    public string Alias { get; } = alias;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class IgnoreAttribute : Attribute;
