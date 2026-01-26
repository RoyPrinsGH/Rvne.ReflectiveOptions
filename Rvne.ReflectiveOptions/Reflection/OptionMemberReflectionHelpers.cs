using System.Reflection;

namespace Rvne.ReflectiveOptions.Reflection;

// TODO: Summary + explanation what we mean by "option member" and why we throw when we throw w.r.t. the spec of the library
internal static class OptionMemberReflectionHelpers
{
    // Not entirely sure why this is not static, but reading the source
    // it seem to hold some sort of lookup cache, so I'm placing it behind a static class
    // to keep the allocations to a minimum
    private readonly static NullabilityInfoContext _nullabilityInfoContext = new();

    // TODO: Summary
    internal static NullabilityInfo GetNullabilityInfo(MemberInfo fieldInfo) =>
        fieldInfo switch
        {
            PropertyInfo property => _nullabilityInfoContext.Create(property),
            FieldInfo field => _nullabilityInfoContext.Create(field),
            // TODO: Fill this exception
            _ => throw new InvalidOperationException("")
        };

    // TODO: Summary
    internal static void ThrowIfNotAssignable(MemberInfo optionMember, Type? optionType)
    {
        if (optionType is null)
        {
            ThrowIfNotNullAssignable(optionMember);
        }
        else
        {
            Type memberType = GetMemberType(optionMember);

            if (memberType.IsAssignableFrom(optionType)
                || Nullable.GetUnderlyingType(memberType) is Type baseType
                    && baseType.IsAssignableFrom(optionType))
            {
                // Example cases that reach here:
                // - Trying to assign Dog to Animal
                // - Trying to assign Dog to Dog?
                return;
            }

            // TODO: Specify is not about a mismatch, it's about assignability 
            // and add more info
            throw new InvalidOperationException(
                $"Option value type mismatch.");
        }
    }

    // TODO: Summary + explanation what happens here w.r.t. technical details about nullability repr
    internal static void ThrowIfNotNullAssignable(MemberInfo optionMember)
    {
        Type memberType = GetMemberType(optionMember);

        if (memberType.IsValueType)
        {
            if (Nullable.GetUnderlyingType(memberType) is null)
                throw new InvalidOperationException($"Option value cannot be null for non-nullable '{memberType}'.");
        }
        else
        {
            NullabilityInfo nullability = GetNullabilityInfo(optionMember);

            var nullState = nullability.WriteState == NullabilityState.Unknown
                ? nullability.ReadState
                : nullability.WriteState;

            if (nullState == NullabilityState.NotNull)
                throw new InvalidOperationException($"Option value cannot be null for non-nullable '{memberType}'.");
        }
    }

    // TODO: Summary
    internal static MemberInfo FindMemberByName(Type optionType, string optionMemberName)
    {
        for (
            var visitingType = optionType;
            visitingType is not null && visitingType != typeof(object);
            visitingType = visitingType.BaseType
        )
        {
            const BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly;

            // Manually checking for properties & fields instead of using .GetMember()
            // avoids issues with other types of members / multiple returns
            if (visitingType.GetProperty(optionMemberName, flags) is PropertyInfo property)
                return property;

            if (visitingType.GetField(optionMemberName, flags) is FieldInfo field)
                return field;
        }

        throw new MissingMemberException(optionType.FullName, optionMemberName);
    }

    // TODO: Summary
    internal static Type GetMemberType(MemberInfo optionMember)
    {
        return optionMember switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new InvalidOperationException(
                $"Member '{optionMember.DeclaringType!.FullName}.{optionMember.Name}' is not a field or property.")
        };
    }
}