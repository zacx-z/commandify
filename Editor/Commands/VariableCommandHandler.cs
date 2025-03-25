using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Commandify
{
    public class VariableCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No variable subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "list":
                    return ListVariables(context);
                case "get":
                    return GetVariable(subArgs, context);
                case "set":
                    return SetVariable(subArgs, context);
                case "clear":
                    return ClearVariables(context);
                default:
                    throw new ArgumentException($"Unknown variable subcommand: {subCommand}");
            }
        }

        private string ListVariables(CommandContext context)
        {
            var variables = context.GetAllVariables();
            if (!variables.Any())
                return "No variables set";

            var lines = new List<string>();
            foreach (var variable in variables)
            {
                string valueStr = FormatValue(variable.Value);
                lines.Add($"${variable.Key} = {valueStr}");
            }

            // Store the variable names in the result
            context.SetLastResult(variables.Keys.ToList());
            return string.Join("\n", lines);
        }

        private string GetVariable(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Variable name required");

            string name = args[0];
            var value = context.GetVariable(name);
            if (value == null)
                throw new ArgumentException($"Variable not found: ${name}");

            // Store the variable value in the result
            context.SetLastResult(value);
            return $"${name} = {FormatValue(value)}";
        }

        private string SetVariable(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Variable name and value required");

            string name = args[0];
            name = context.ResolveStringReference(name);

            string valueStr = args[1];
            object value;

            // Handle variable reference in value
            if (valueStr.StartsWith("$"))
            {
                value = context.ResolveReference(valueStr);
            }
            else
            {
                // Try to parse the value as a number or boolean
                if (bool.TryParse(valueStr, out bool boolValue))
                    value = boolValue;
                else if (int.TryParse(valueStr, out int intValue))
                    value = intValue;
                else if (float.TryParse(valueStr, out float floatValue))
                    value = floatValue;
                else if (valueStr.StartsWith("[") && valueStr.EndsWith("]"))
                {
                    // Parse as array
                    var elements = valueStr.Trim('[', ']')
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    value = elements;
                }
                else if (valueStr == "null")
                    value = null;
                else
                    value = valueStr;
            }

            context.SetVariable(name, value);
            return $"Set ${name} = {FormatValue(value)}";
        }

        private string ClearVariables(CommandContext context)
        {
            int count = context.GetAllVariables().Count;
            context.ClearVariables();
            return $"Cleared {count} variable(s)";
        }

        private string FormatValue(object value)
        {
            if (value == null)
                return "null";
            if (value is bool || value is int || value is float)
                return value.ToString();
            if (value is string str)
                return $"\"{str}\"";
            if (value is UnityEngine.Object obj)
                return $"{obj.GetType().Name}({obj.name})";
            if (value is IEnumerable<object> list)
                return $"[{string.Join(", ", list.Select(FormatValue))}]";
            return value.ToString();
        }
    }
}
