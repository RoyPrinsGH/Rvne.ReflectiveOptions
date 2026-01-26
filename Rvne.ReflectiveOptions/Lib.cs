using System.Reflection;
using Rvne.ReflectiveOptions.Attributes;

namespace Rvne.ReflectiveOptions;

public static class ReflectiveOptionsExtensions
{
    public static TOptions GetOptions<TOptions>(this object source)
        where TOptions : new()
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.GetOptions<TOptions>(source);
    }

    public static TOptions GetOptions<TOptions>(this object source, object derivationContext)
        where TOptions : new()
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(derivationContext);

        IEnumerable<Attribute> sourceAttributes = source.GetType().GetCustomAttributes(inherit: true).Cast<Attribute>();
        return UncachedBuilder.BuildFrom<TOptions>(sourceAttributes, derivationContext);
    }

    public static TOptions GetMemberOptions<TOptions>(this object derivationContext, string memberName)
        where TOptions : new()
    {
        ArgumentNullException.ThrowIfNull(derivationContext);

        if (string.IsNullOrWhiteSpace(memberName))
            throw new ArgumentException("Member name must be provided.", nameof(memberName));

        IEnumerable<Attribute> memberAttributes = GetMemberInfo(derivationContext, memberName)
            .GetCustomAttributes(inherit: true)
            .Cast<Attribute>();

        return UncachedBuilder.BuildFrom<TOptions>(memberAttributes, derivationContext);
    }

    private static MemberInfo GetMemberInfo(object derivationContext, string memberName)
    {
        for (var t = derivationContext.GetType(); t is not null && t != typeof(object); t = t.BaseType)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            var property = t.GetProperty(memberName, flags);
            if (property is not null)
            {
                if (!property.CanRead || property.GetIndexParameters().Length != 0)
                    throw new InvalidOperationException($"Member '{t.FullName}.{memberName}' is not a readable non-indexed property.");

                return property;
            }

            var field = t.GetField(memberName, flags);
            if (field is not null)
            {
                return field;
            }
        }

        throw new MissingMemberException(derivationContext.GetType().FullName, memberName);
    }
}

internal static class UncachedBuilder
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public static TOptions BuildFrom<TOptions>(IEnumerable<Attribute> customAttributes, object derivationContext)
        where TOptions : new()
    {
        var options = new TOptions();

        foreach (Attribute attr in customAttributes)
        {
            // Check if it's a compile-time only application
            if (attr is IOptionMiddleware<TOptions> optionAttribute)
            {
                optionAttribute.Apply(options);

                // We explicitly don't support both a compile-time only application and a derivation at the same time.
                continue;
            }

            // Check if it's a derivation that needs to be computed
            var deriveMethodInfo = GetDeriveMethodInfoFor<TOptions>(attr);
            if (deriveMethodInfo is null)
                continue;

            (string methodName, Type resultType) = deriveMethodInfo.Value;

            object? opaqueDerivationResult = CallDerivationMethod(methodName, resultType, derivationContext);
            ApplyOpaqueGenericDerivation(attr, resultType, opaqueDerivationResult!, options);
        }

        return options;
    }

    private static object? CallDerivationMethod(string methodName, Type resultType, object context)
    {
        Type contextType = context.GetType();

        var derivationMethod = contextType.GetMethod(
            methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            binder: null, types: Type.EmptyTypes, modifiers: null)
            ?? throw new MissingMethodException(contextType.FullName, methodName);

        if (derivationMethod.ReturnType == typeof(void))
        {
            throw new InvalidOperationException($"Derivation method '{contextType.FullName}.{methodName}' must be parameterless and return a value.");
        }

        var opaqueDerivationResult = derivationMethod.IsStatic
            ? derivationMethod.Invoke(null, null)
            : derivationMethod.Invoke(context, null);

        if (opaqueDerivationResult is null)
        {
            if (resultType.IsValueType && Nullable.GetUnderlyingType(resultType) is null)
            {
                throw new InvalidOperationException($"Derivation method '{contextType.FullName}.{methodName}' returned null for non-nullable '{resultType}'.");
            }

            if (!resultType.IsValueType)
            {
                NullabilityInfo nullability = NullabilityContext.Create(derivationMethod.ReturnParameter);
                if (nullability.ReadState == NullabilityState.NotNull)
                {
                    throw new InvalidOperationException($"Derivation method '{contextType.FullName}.{methodName}' returned null for non-nullable '{resultType}'.");
                }
            }
        }

        if (opaqueDerivationResult is not null
            && !resultType.IsInstanceOfType(opaqueDerivationResult))
        {
            throw new InvalidOperationException($"Derivation result type mismatch. Expected '{resultType}', got '{opaqueDerivationResult.GetType()}'.");
        }

        return opaqueDerivationResult;
    }

    private static void ApplyOpaqueGenericDerivation<TOptions>(
        Attribute opaqueGenericDerivedOptionAttribute, Type opaqueGenericParameter, object opaqueDerivationResult, TOptions options)
    {
        var applyMi = opaqueGenericDerivedOptionAttribute.GetType().GetMethod(nameof(DerivedOptionAttribute<,>.Apply),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, types: [opaqueGenericParameter, typeof(TOptions)], modifiers: null)
            ?? throw new InvalidOperationException($"Apply({opaqueGenericParameter.Name}, {typeof(TOptions).Name}) not found on {opaqueGenericDerivedOptionAttribute.GetType().FullName}.");

        applyMi.Invoke(opaqueGenericDerivedOptionAttribute, [opaqueDerivationResult, options]);
    }

    private static (string methodName, Type resultType)? GetDeriveMethodInfoFor<TOptions>(Attribute attr)
    {
        for (var t = attr.GetType(); t is not null && t != typeof(object); t = t.BaseType)
        {
            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(DerivedOptionAttribute<,>)
                && t.GetGenericArguments()[0] == typeof(TOptions))
            {
                return (
                    (string)t.GetProperty(nameof(DerivedOptionAttribute<,>.DerivationMethodName))!.GetValue(attr)!,
                    t.GetGenericArguments()[1]
                );
            }
        }
        return null;
    }
}
