using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Commandify
{
    /// <summary>
    /// Utility class for type-related operations, particularly for finding and resolving Unity component types.
    /// </summary>
    public static class TypeUtility
    {
        /// <summary>
        /// Finds a component type by name, searching in the current assembly, UnityEngine namespace, and all loaded assemblies.
        /// </summary>
        /// <param name="typeName">The name of the type to find. Can be a simple name (e.g., "Rigidbody") or fully qualified (e.g., "UnityEngine.Rigidbody").</param>
        /// <returns>The found Type, or null if not found or not a Component type.</returns>
        public static Type FindType(string typeName)
        {
            // Try exact name first
            var type = Type.GetType(typeName);
            if (type != null && typeof(Component).IsAssignableFrom(type))
                return type;

            // Try Unity namespace
            type = Type.GetType($"UnityEngine.{typeName}");
            if (type != null && typeof(Component).IsAssignableFrom(type))
                return type;

            // Try all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName) ?? 
                       assembly.GetType($"UnityEngine.{typeName}");
                
                if (type != null && typeof(Component).IsAssignableFrom(type))
                    return type;
            }

            return null;
        }

        /// <summary>
        /// Gets all types derived from the specified base type that match the given pattern.
        /// </summary>
        /// <param name="baseType">The base type to search for derived types. Must be Component or a derived type.</param>
        /// <param name="pattern">Optional wildcard pattern to filter types by name.</param>
        /// <returns>An enumerable of matching types, ordered by full name.</returns>
        public static IEnumerable<Type> GetDerivedTypes(Type baseType, string pattern = null)
        {
            if (!typeof(Component).IsAssignableFrom(baseType))
                throw new ArgumentException($"Base type must inherit from Component: {baseType.FullName}");

            var types = TypeCache.GetTypesDerivedFrom(baseType);
            
            if (string.IsNullOrEmpty(pattern))
                return types.OrderBy(t => t.FullName);

            return types.Where(t => WildcardMatch(t.FullName, pattern))
                       .OrderBy(t => t.FullName);
        }

        /// <summary>
        /// Performs a case-insensitive wildcard match.
        /// </summary>
        private static bool WildcardMatch(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;

            if (string.IsNullOrEmpty(text))
                return false;

            // Convert the pattern to a regex pattern
            pattern = pattern.Replace(".", "\\.")
                           .Replace("*", ".*")
                           .Replace("?", ".");

            return System.Text.RegularExpressions.Regex.IsMatch(
                text,
                $"^{pattern}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
