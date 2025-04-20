using UnityEngine;
using System;
using System.Linq;

namespace Commandify
{
    /// <summary>
    /// Utility class for vector-related operations, particularly for parsing vector formats from command line.
    /// </summary>
    public static class VectorUtility
    {
        /// <summary>
        /// Parses a string into a Vector3, supporting formats like "(x,y,z)" or "x,y,z".
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="context">Optional command context for variable resolution</param>
        /// <returns>A Vector3 parsed from the string</returns>
        public static Vector3 ParseVector3(string value, CommandContext context = null)
        {
            // If context provided and it's a variable reference, resolve it first
            if (context != null && value.StartsWith("$"))
            {
                value = context.ResolveStringReference(value);
            }

            // Remove parentheses if present
            value = value.Trim();
            if (value.StartsWith("(") && value.EndsWith(")"))
            {
                value = value.Substring(1, value.Length - 2);
            }

            // Split by comma
            string[] components = value.Split(',');
            if (components.Length != 3)
            {
                throw new ArgumentException($"Vector format must have exactly 3 components: {value}");
            }

            // Parse each component
            float x, y, z;
            if (!float.TryParse(components[0].Trim(), out x))
                throw new ArgumentException($"Invalid x coordinate in vector: {components[0]}");
            if (!float.TryParse(components[1].Trim(), out y))
                throw new ArgumentException($"Invalid y coordinate in vector: {components[1]}");
            if (!float.TryParse(components[2].Trim(), out z))
                throw new ArgumentException($"Invalid z coordinate in vector: {components[2]}");

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Parses a string into a Vector2, supporting formats like "(x,y)" or "x,y".
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="context">Optional command context for variable resolution</param>
        /// <returns>A Vector2 parsed from the string</returns>
        public static Vector2 ParseVector2(string value, CommandContext context = null)
        {
            // If context provided and it's a variable reference, resolve it first
            if (context != null && value.StartsWith("$"))
            {
                value = context.ResolveStringReference(value);
            }

            // Remove parentheses if present
            value = value.Trim();
            if (value.StartsWith("(") && value.EndsWith(")"))
            {
                value = value.Substring(1, value.Length - 2);
            }

            // Split by comma
            string[] components = value.Split(',');
            if (components.Length != 2)
            {
                throw new ArgumentException($"Vector2 format must have exactly 2 components: {value}");
            }

            // Parse each component
            float x, y;
            if (!float.TryParse(components[0].Trim(), out x))
                throw new ArgumentException($"Invalid x coordinate in vector: {components[0]}");
            if (!float.TryParse(components[1].Trim(), out y))
                throw new ArgumentException($"Invalid y coordinate in vector: {components[1]}");

            return new Vector2(x, y);
        }

        /// <summary>
        /// Parses a string into a Vector4, supporting formats like "(x,y,z,w)" or "x,y,z,w".
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="context">Optional command context for variable resolution</param>
        /// <returns>A Vector4 parsed from the string</returns>
        public static Vector4 ParseVector4(string value, CommandContext context = null)
        {
            // If context provided and it's a variable reference, resolve it first
            if (context != null && value.StartsWith("$"))
            {
                value = context.ResolveStringReference(value);
            }

            // Remove parentheses if present
            value = value.Trim();
            if (value.StartsWith("(") && value.EndsWith(")"))
            {
                value = value.Substring(1, value.Length - 2);
            }

            // Split by comma
            string[] components = value.Split(',');
            if (components.Length != 4)
            {
                throw new ArgumentException($"Vector4 format must have exactly 4 components: {value}");
            }

            // Parse each component
            float x, y, z, w;
            if (!float.TryParse(components[0].Trim(), out x))
                throw new ArgumentException($"Invalid x coordinate in vector: {components[0]}");
            if (!float.TryParse(components[1].Trim(), out y))
                throw new ArgumentException($"Invalid y coordinate in vector: {components[1]}");
            if (!float.TryParse(components[2].Trim(), out z))
                throw new ArgumentException($"Invalid z coordinate in vector: {components[2]}");
            if (!float.TryParse(components[3].Trim(), out w))
                throw new ArgumentException($"Invalid w coordinate in vector: {components[3]}");

            return new Vector4(x, y, z, w);
        }

        /// <summary>
        /// Parses a string into an array of floats, supporting formats like "(x,y,z)" or "x,y,z".
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="expectedComponents">The expected number of components, or 0 for any number</param>
        /// <param name="context">Optional command context for variable resolution</param>
        /// <returns>An array of floats parsed from the string</returns>
        public static float[] ParseVector(string value, int expectedComponents = 0, CommandContext context = null)
        {
            // If context provided and it's a variable reference, resolve it first
            if (context != null && value.StartsWith("$"))
            {
                value = context.ResolveStringReference(value);
            }

            // Remove parentheses if present
            value = value.Trim();
            if (value.StartsWith("(") && value.EndsWith(")"))
            {
                value = value.Substring(1, value.Length - 2);
            }

            // Split by comma and parse
            var components = value.Split(',')
                .Select(s => 
                {
                    if (!float.TryParse(s.Trim(), out float result))
                        throw new ArgumentException($"Invalid component in vector: {s}");
                    return result;
                })
                .ToArray();

            // Validate component count if expected
            if (expectedComponents > 0 && components.Length != expectedComponents)
                throw new ArgumentException($"Expected {expectedComponents} components, got {components.Length}");

            return components;
        }
    }
}
