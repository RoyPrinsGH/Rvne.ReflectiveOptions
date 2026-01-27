using System.Reflection;
using Rvne.ReflectiveOptions.Interfaces;

namespace Rvne.ReflectiveOptions.Reflection;

/// <summary>
/// Resolves option middleware attributes and applies their effects to an options instance.
/// </summary>
internal static class OptionAttributeResolutionHelpers
{
    /// <summary>
    /// Applies the attribute to the options instance when it implements a supported middleware interface.
    /// </summary>
    internal static TOptions TryApplyAttribute<TOptions>(Attribute attribute, TOptions options, object declaringInstance)
    {
        // Only known middleware attributes are applied; unrelated attributes are intentionally ignored.
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

    /// <summary>
    /// Invokes the named derivation method and returns its value.
    /// </summary>
    private static object? CallDerivationMethod(string methodName, object declaringInstance)
    {
        Type declaringType = declaringInstance.GetType();

        // Look for a parameterless method on the instance type; we support both instance and static methods.
        MethodInfo derivationMethod = declaringType.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, types: Type.EmptyTypes)
            ?? throw new MissingMethodException(declaringType.FullName, methodName);

        if (derivationMethod.ReturnType == typeof(void))
        {
            throw new InvalidOperationException(
                $"Derivation method '{declaringType.FullName}.{methodName}' must be parameterless and return a value.");
        }

        // Static methods don't need an instance; instance methods use the declaring object.
        return derivationMethod.IsStatic
            ? derivationMethod.Invoke(null, null)
            : derivationMethod.Invoke(declaringInstance, null);
    }
}
