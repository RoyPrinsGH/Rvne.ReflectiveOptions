using System.Reflection;

internal static class NullabilityHelpers
{
    // Not entirely sure why this is not static, but reading the source
    // it seem to hold some sort of lookup cache, so I'm placing it behind a static class
    // to keep the allocations to a minimum
    private readonly static NullabilityInfoContext _nullabilityInfoContext = new();

    /// <summary>
    /// Gets nullability metadata for a field or property member.
    /// </summary>
    internal static NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo) =>
        memberInfo switch
        {
            PropertyInfo property => _nullabilityInfoContext.Create(property),
            FieldInfo field => _nullabilityInfoContext.Create(field),
            _ => throw new InvalidOperationException(
                $"Member '{memberInfo.DeclaringType!.FullName}.{memberInfo.Name}' is not a field or property.")
        };
}