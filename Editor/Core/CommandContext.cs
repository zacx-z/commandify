using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            if (!IsIdentifier(name)) return null;

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

        private async Task<object> ResolveCommandSubstitution(string reference)
        {
            if (!reference.StartsWith("$(") || !reference.EndsWith(")"))
                return null;

            string command = reference.Substring(2, reference.Length - 3);
            await CommandProcessor.Instance.ExecuteCommandAsync(command);
            return ResolveReference("$~");
        }

        public async Task<IEnumerable<UnityEngine.Object>> ResolveObjectReference(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Reference cannot be empty");

            // If it's a variable reference
            if (IsVariable(reference))
            {
                var value = await ResolveCommandSubstitution(reference) ?? GetVariable(reference);

                if (value is IEnumerable<UnityEngine.Object> objects)
                    return objects;
                if (value is UnityEngine.Object obj)
                    return new[] { obj };
                if (value is string sel)
                    return new Selector(sel, variables).Evaluate();
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
                var value = ResolveCommandSubstitution(reference) ?? GetVariable(reference);
                return value?.ToString() ?? "null";
            }

            return reference;
        }

        public object ResolveReference(string reference)
        {
            if (!reference.StartsWith("$"))
                return reference;

            var value = ResolveCommandSubstitution(reference) ?? GetVariable(reference);
            if (value == null)
                throw new ArgumentException($"Variable not found: {reference}");

            return value;
        }

        public IReadOnlyDictionary<string, object> GetAllVariables()
        {
            return variables;
        }

        private bool IsIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // Single character case
            if (name.Length == 1)
                return true;

            // Check remaining characters (can be letter, digit, or underscore)
            for (int i = 0; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                    return false;
            }

            return true;
        }

        public bool IsVariable(string value)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("$"))
                return false;

            return IsIdentifier(value.Substring(1));
        }
    }
}
