using System.Reflection;

namespace Rvne.ReflectiveOptions
{

    public interface IOptionAttribute<TOptions>
    {
        void Apply(TOptions options);
    }

    public abstract class DerivedOptionAttribute<TOptions, TDerivationResult>(string derivationMethodName) : Attribute
    {
        public string DerivationMethodName { get; } = derivationMethodName;
        public abstract void Apply(TDerivationResult derivationResult, TOptions options);
    }

    public static class ReflectiveOptionsExtensions
    {
        public static TOptions CalculateOptions<TOptions>(this object source, object? derivationContext = null)
            where TOptions : new()
        {
            derivationContext ??= source;
            return UncachedBuilder.BuildFrom<TOptions>(source, derivationContext);
        }
    }

    internal static class UncachedBuilder
    {
        public static TOptions BuildFrom<TOptions>(object source, object derivationContext) where TOptions : new()
        {
            var options = new TOptions();

            foreach (Attribute attr in source.GetType().GetCustomAttributes(inherit: true).Cast<Attribute>())
            {
                // Check if it's a compile-time only application
                if (attr is IOptionAttribute<TOptions> optionAttribute)
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

            if (derivationMethod.ReturnType == typeof(void)
                || derivationMethod.GetParameters().Length != 0)
            {
                throw new InvalidOperationException($"Derivation method '{contextType.FullName}.{methodName}' must be parameterless and return a value.");
            }

            var opaqueDerivationResult = derivationMethod.IsStatic
                ? derivationMethod.Invoke(null, null)
                : derivationMethod.Invoke(context, null);

            if (opaqueDerivationResult is null
                && resultType.IsValueType
                && Nullable.GetUnderlyingType(resultType) is null)
            {
                throw new InvalidOperationException($"Derivation method '{contextType.FullName}.{methodName}' returned null for non-nullable '{resultType}'.");
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
}