using System.Runtime.CompilerServices;

namespace ClassicDiagnostics.Avalonia.Extensions;

internal static class TypeExtensions
{
    private readonly static ConditionalWeakTable<Type, string> TypeNameCache = new();
    private readonly static ConditionalWeakTable<Type, string> DetailedTypeNameCache = new();

    /// <param name="type"></param>
    extension(Type type)
    {
        /// <summary>
        /// Gets a human-friendly name for a type, including generic type arguments and nullable annotations.
        /// </summary>
        /// <returns></returns>
        public string GetTypeName()
        {
            if (TypeNameCache.TryGetValue(type, out var name)) return name;

            name = GetTypeNameCore(type, includeNamespace: false);
            TypeNameCache.Add(type, name);
            return name;
        }

        /// <summary>
        /// Gets a diagnostic-friendly type name for tooltips where namespace and assembly identity matter.
        /// </summary>
        public string GetDetailedTypeName()
        {
            if (DetailedTypeNameCache.TryGetValue(type, out var name)) return name;

            var assemblyName = type.Assembly.GetName().Name;
            name = $"{GetTypeNameCore(type, includeNamespace: true)}{Environment.NewLine}Assembly: {assemblyName}";
            DetailedTypeNameCache.Add(type, name);
            return name;
        }
    }

    private static string GetTypeNameCore(Type type, bool includeNamespace)
    {
        if (type.IsArray)
        {
            var rank = type.GetArrayRank();
            var suffix = rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";
            var elementType = type.GetElementType();
            return $"{(elementType is null ? "?" : GetTypeNameCore(elementType, includeNamespace))}{suffix}";
        }

        if (Nullable.GetUnderlyingType(type) is { } nullable)
        {
            return GetTypeNameCore(nullable, includeNamespace) + "?";
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();
            var name = GetNonGenericTypeName(definition, includeNamespace);
            return $"{name}<{string.Join(", ", arguments.Select(argument => GetTypeNameCore(argument, includeNamespace)))}>";
        }

        return GetNonGenericTypeName(type, includeNamespace);
    }

    private static string GetNonGenericTypeName(Type type, bool includeNamespace)
    {
        var name = includeNamespace ? type.FullName ?? type.Name : type.Name;
        var tickIndex = name.IndexOf('`');
        if (tickIndex >= 0)
        {
            name = name[..tickIndex];
        }

        return name.Replace('+', '.');
    }
}