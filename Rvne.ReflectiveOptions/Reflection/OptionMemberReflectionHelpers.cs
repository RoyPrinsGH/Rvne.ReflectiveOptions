using System.Reflection;

namespace Rvne.ReflectiveOptions.Reflection;

/// <summary>
/// Helper methods for reflecting on option members and validating assignments.
/// </summary>
/// <remarks>
/// In this library an "option member" is the field or property on the options type referenced by a
/// <see cref="ReflectiveOptionAttribute{TOptions,TValue}"/>. We throw when that member is missing,
/// not writable, or cannot accept the provided value (including nullability constraints) because those
/// cases are invalid option definitions rather than recoverable runtime conditions.
/// </remarks>
internal static class OptionMemberReflectionHelpers
{
    // Not entirely sure why this is not static, but reading the source
    // it seem to hold some sort of lookup cache, so I'm placing it behind a static class
    // to keep the allocations to a minimum
    private readonly static NullabilityInfoContext _nullabilityInfoContext = new();

    /// <summary>
    /// Gets nullability metadata for a field or property member.
    /// </summary>
    private static NullabilityInfo GetNullabilityInfo(MemberInfo fieldInfo) =>
        fieldInfo switch
        {
            PropertyInfo property => _nullabilityInfoContext.Create(property),
            FieldInfo field => _nullabilityInfoContext.Create(field),
            _ => throw new InvalidOperationException(
                $"Member '{fieldInfo.DeclaringType!.FullName}.{fieldInfo.Name}' is not a field or property.")
        };

    /// <summary>
    /// Ensures the provided option value type can be assigned to the option member.
    /// </summary>
    internal static void ThrowIfNotAssignable(MemberInfo optionMember, Type? optionType)
    {
        if (optionMember is PropertyInfo property && (!property.CanWrite || property.GetIndexParameters().Length != 0))
            throw new InvalidOperationException(
                $"Member '{property.DeclaringType!.FullName}.{property.Name}' is not a writable non-indexed property.");

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

            throw new InvalidOperationException(
                $"Option value type '{optionType}' is not assignable to member '{optionMember.DeclaringType!.FullName}.{optionMember.Name}' of type '{memberType}'.");
        }
    }

    /// <summary>
    /// Ensures null can be assigned to the option member.
    /// </summary>
    /// <remarks>
    /// For value types, nullability is represented by <see cref="Nullable{T}"/>. For reference types,
    /// <see cref="NullabilityInfoContext"/> reads compiler-emitted nullability metadata and we use the
    /// write state (or fall back to the read state) to determine if null is allowed.
    /// </remarks>
    private static void ThrowIfNotNullAssignable(MemberInfo optionMember)
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

    /// <summary>
    /// Finds a field or property by name on the type or its base types.
    /// </summary>
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

    /// <summary>
    /// Gets the field or property type for the option member.
    /// </summary>
    private static Type GetMemberType(MemberInfo optionMember)
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
