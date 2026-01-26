using System.Reflection;
using Rvne.ReflectiveOptions.Reflection;

namespace Rvne.ReflectiveOptions;

public abstract class ReflectiveOptionAttribute<TOptions, TValue>(string memberName, TValue value) : Attribute, IOptionAttribute<TOptions>
{
    public string MemberName { get; } = string.IsNullOrWhiteSpace(memberName)
        ? throw new ArgumentException("Member name must be provided.", nameof(memberName))
        : memberName;

    public TValue Value { get; } = value;

    public void Apply(TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        MemberInfo member = GetMemberInfo(typeof(TOptions), MemberName);

        if (member is PropertyInfo property)
        {
            if (!property.CanWrite || property.GetIndexParameters().Length != 0)
            {
                throw new InvalidOperationException(
                    $"Member '{property.DeclaringType!.FullName}.{MemberName}' is not a writable non-indexed property.");
            }

            ValidateAssignable(property, property.PropertyType, Value);
            property.SetValue(options, Value);
            return;
        }

        if (member is FieldInfo field)
        {
            ValidateAssignable(field, field.FieldType, Value);
            field.SetValue(options, Value);
            return;
        }

        throw new InvalidOperationException($"Member '{typeof(TOptions).FullName}.{MemberName}' is not a field or property.");
    }
}