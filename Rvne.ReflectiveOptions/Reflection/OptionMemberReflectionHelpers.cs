using System.Reflection;

namespace Rvne.ReflectiveOptions.Reflection;

/// <summary>
/// Helper methods for reflecting on option members and validating assignments.
/// </summary>
/// <remarks>
/// In this library an "option member" is the field or property on the options type referenced by a
/// <see cref="Attributes.ReflectiveOptionAttribute{TOptions,TMember}"/>. We throw when that member is missing,
/// not writable, or cannot accept the provided value (including nullability constraints) because those
/// cases are invalid option definitions rather than recoverable runtime conditions.
/// </remarks>
public static class OptionMemberReflectionHelpers
{
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
            NullabilityInfo nullability = NullabilityHelpers.GetNullabilityInfo(optionMember);

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

    /// <summary>
    /// Applies a value to the named option member on the options instance.
    /// </summary>
    public static void Apply<TOptions, TValue>(TOptions options, string optionMemberName, TValue value)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(optionMemberName))
            throw new ArgumentException("Member name must be provided.", nameof(optionMemberName));

        MemberInfo member = FindMemberByName(typeof(TOptions), optionMemberName);

        ThrowIfNotAssignable(member, typeof(TValue));

        if (member is PropertyInfo property)
        {
            property.SetValue(options, value);
        }
        else if (member is FieldInfo field)
        {
            field.SetValue(options, value);
        }
        else
        {
            throw new InvalidOperationException($"Member '{typeof(TOptions).FullName}.{member.Name}' is not a field or property.");
        }
    }
}
