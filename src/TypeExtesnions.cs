using System.Runtime.CompilerServices;

namespace ClassicDiagnostics.Avalonia;

internal static class TypeExtesnions
{
    private readonly static ConditionalWeakTable<Type, string> s_getTypeNameCache = new();

    public static string GetTypeName(this Type type)
    {
        if (!s_getTypeNameCache.TryGetValue(type, out var name))
        {
            name = type.Name;
            if (Nullable.GetUnderlyingType(type) is Type nullable)
            {
                name = nullable.Name + "?";
            }
            else if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var arguments = type.GetGenericArguments();
                name = definition.Name.Substring(0, definition.Name.IndexOf('`'));
                name = $"{name}<{string.Join(",", arguments.Select(GetTypeName))}>";
            }
            s_getTypeNameCache.Add(type, name);
        }
        return name;
    }
}