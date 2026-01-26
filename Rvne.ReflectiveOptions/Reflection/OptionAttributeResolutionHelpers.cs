using System.Reflection;
using Rvne.ReflectiveOptions.Interfaces;

namespace Rvne.ReflectiveOptions.Reflection;

internal static class OptionAttributeResolutionHelpers
{
    // TODO: Summary + explanation in code
    internal static TOptions TryApplyAttribute<TOptions>(Attribute attribute, TOptions options, object declaringInstance)
    {
        if (attribute is IStaticOptionMiddleware<TOptions> staticOptionAttribute)
        {
            staticOptionAttribute.Apply(options);
        }
        else if (attribute is IDerivedOptionMiddleware<TOptions> derivedOptionAttribute)
        {
            object? derivationResult = CallDerivationMethod(derivedOptionAttribute.DerivationMethod, declaringInstance);
            derivedOptionAttribute.Apply(options, derivationResult);
        }

        return options;
    }

    // TODO: Summary + explanation in code
    private static object? CallDerivationMethod(string methodName, object declaringInstance)
    {
        Type declaringType = declaringInstance.GetType();

        MethodInfo derivationMethod = declaringType.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, types: Type.EmptyTypes)
            ?? throw new MissingMethodException(declaringType.FullName, methodName);

        if (derivationMethod.ReturnType == typeof(void))
        {
            throw new InvalidOperationException(
                $"Derivation method '{declaringType.FullName}.{methodName}' must be parameterless and return a value.");
        }

        return derivationMethod.IsStatic
            ? derivationMethod.Invoke(null, null)
            : derivationMethod.Invoke(declaringInstance, null);
    }
}
