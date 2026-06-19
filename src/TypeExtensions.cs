using System.Runtime.CompilerServices;

namespace ClassicDiagnostics.Avalonia;

internal static class TypeExtensions
{
    private readonly static ConditionalWeakTable<Type, string> TypeNameCache = new();

    /// <summary>
    /// Gets a human-friendly name for a type, including generic type arguments and nullable annotations.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetTypeName(this Type type)
    {
        if (TypeNameCache.TryGetValue(type, out var name)) return name;

        name = type.Name;
        if (Nullable.GetUnderlyingType(type) is { } nullable)
        {
            name = nullable.Name + "?";
        }
        else if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();
            name = definition.Name[..definition.Name.IndexOf('`')];
            name = $"{name}<{string.Join(",", arguments.Select(GetTypeName))}>";
        }

        TypeNameCache.Add(type, name);
        return name;
    }
}
