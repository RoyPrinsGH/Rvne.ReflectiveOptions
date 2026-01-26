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

        MemberInfo member = OptionMemberReflectionHelpers.FindMemberByName(typeof(TOptions), MemberName);

        OptionMemberReflectionHelpers.ThrowIfNotAssignable(member, Value?.GetType());

        if (member is PropertyInfo property)
        {
            property.SetValue(options, Value);
        }
        else if (member is FieldInfo field)
        {
            field.SetValue(options, Value);
        }
        else
        {
            throw new InvalidOperationException($"Member '{typeof(TOptions).FullName}.{MemberName}' is not a field or property.");
        }

    }
}