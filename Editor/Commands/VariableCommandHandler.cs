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
            if (args.Count < 2)
                throw new ArgumentException("Variable name and value required");

            string name = args[0];
            name = context.ResolveStringReference(name);

            string valueStr = args[1];
            object value;

            // Handle variable reference in value
            if (context.IsVariable(valueStr))
            {
                value = context.ResolveReference(valueStr);
            }
            else
            {
                value = context.ResolveObjectReference(valueStr).ToArray();
            }

            context.SetVariable(name, value);
            return $"Set ${name} = {FormatValue(value)}";
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
