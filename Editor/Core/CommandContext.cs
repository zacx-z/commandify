using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Commandify
{
    public class CommandContext
    {
        private Dictionary<string, object> variables;

        public CommandContext()
        {
            variables = new Dictionary<string, object>();
            variables["~"] = null; // Initialize the last result variable
        }

        public object GetVariable(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Variable name cannot be empty");

            // Remove $ prefix if present
            name = name.StartsWith("$") ? name.Substring(1) : name;

            if (!variables.TryGetValue(name, out object value))
                throw new ArgumentException($"Variable ${name} not found");

            return value;
        }

        public T GetVariable<T>(string name)
        {
            var value = GetVariable(name);
            if (value is T typedValue)
                return typedValue;
            throw new ArgumentException($"Variable ${name} is not of type {typeof(T).Name}");
        }

        public bool TryGetVariable<T>(string name, out T value)
        {
            value = default;
            try
            {
                value = GetVariable<T>(name);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetVariable(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Variable name cannot be empty");

            // Remove $ prefix if present
            name = name.StartsWith("$") ? name.Substring(1) : name;
            variables[name] = value;
        }

        public bool HasVariable(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // Remove $ prefix if present
            name = name.StartsWith("$") ? name.Substring(1) : name;
            return variables.ContainsKey(name);
        }

        public void ClearVariables()
        {
            var lastResult = GetLastResult();
            variables.Clear();
            variables["~"] = lastResult; // Preserve the last result
        }

        public void SetLastResult(object result)
        {
            variables["~"] = result;
        }

        public object GetLastResult()
        {
            return variables["~"];
        }

        public T GetLastResult<T>()
        {
            var value = GetLastResult();
            if (value is T typedValue)
                return typedValue;
            throw new ArgumentException($"Last result is not of type {typeof(T).Name}");
        }

        public IEnumerable<UnityEngine.Object> ResolveObjectReference(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Reference cannot be empty");

            // If it's a variable reference
            if (reference.StartsWith("$"))
            {
                var value = GetVariable(reference);
                if (value is IEnumerable<UnityEngine.Object> objects)
                    return objects;
                if (value is UnityEngine.Object obj)
                    return new[] { obj };
                throw new ArgumentException($"Variable {reference} does not contain Unity objects");
            }

            // Otherwise, treat as a selector
            var selector = new Selector(reference, variables);
            return selector.Evaluate();
        }

        public string ResolveStringReference(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Reference cannot be empty");

            // If it's a variable reference
            if (reference.StartsWith("$"))
            {
                var value = GetVariable(reference);
                return value?.ToString() ?? "null";
            }

            return reference;
        }

        public IReadOnlyDictionary<string, object> GetAllVariables()
        {
            return variables;
        }

        public object ResolveReference(string reference)
        {
            if (!reference.StartsWith("$"))
                return reference;

            string varName = reference.Substring(1);
            var value = GetVariable(varName);
            if (value == null)
                throw new ArgumentException($"Variable not found: {reference}");

            return value;
        }
    }
}
