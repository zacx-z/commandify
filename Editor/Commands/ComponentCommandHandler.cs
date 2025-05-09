using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Commandify
{
    public class ComponentCommandHandler : ICommandHandler
    {
        public async Task<string> ExecuteAsync(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No component subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "list":
                    return await ListComponents(subArgs, context);
                case "add":
                    return await AddComponent(subArgs, context);
                case "search":
                    return SearchComponents(subArgs, context);
                case "remove":
                    return await RemoveComponents(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown component subcommand: {subCommand}");
            }
        }

        private ObjectFormatter.OutputFormat format = ObjectFormatter.OutputFormat.Default;

        private async Task<string> ListComponents(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Object selector required");

            // Parse format option
            for (int i = 0; i < args.Count; i++)
            {
                string arg = context.ResolveStringReference(args[i]);
                if (arg == "--format" && ++i < args.Count)
                {
                    string formatStr = context.ResolveStringReference(args[i]).ToLower();
                    args.RemoveRange(i - 1, 2);
                    i -= 2;

                    switch (formatStr)
                    {
                        case "path":
                            format = ObjectFormatter.OutputFormat.Path;
                            break;
                        case "instance-id":
                        case "instanceid":
                            format = ObjectFormatter.OutputFormat.InstanceId;
                            break;
                        default:
                            throw new ArgumentException("Format must be either 'path' or 'instance-id'");
                    }
                }
            }

            var objects = await context.ResolveObjectReference(args[0]);
            var components = new List<string>();

            foreach (var obj in objects)
            {
                if (obj is GameObject go)
                {
                    var objComponents = go.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => $"{ObjectFormatter.FormatObject(go, format)}: {c.GetType().Name}");
                    components.AddRange(objComponents);
                }
            }

            if (!components.Any())
                return "No components found";

            // Store components in result variable
            context.SetLastResult(components);
            return string.Join("\n", components);
        }

        private async Task<string> AddComponent(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Object selector and component type required");

            var objects = await context.ResolveObjectReference(args[0]);
            string componentType = args[1];
            componentType = context.ResolveStringReference(componentType);

            var type = TypeUtility.FindType(componentType);
            if (type == null)
                throw new ArgumentException($"Component type not found: {componentType}");

            var addedComponents = new List<Component>();
            foreach (var obj in objects)
            {
                if (obj is GameObject go)
                {
                    Undo.RecordObject(go, "Add Component");
                    var component = Undo.AddComponent(go, type);
                    if (component != null)
                        addedComponents.Add(component);
                }
            }

            // Store added components in result variable
            context.SetLastResult(addedComponents);
            return $"Added {addedComponents.Count} {componentType} component(s)";
        }

        private string SearchComponents(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Search pattern required");

            string pattern = null;
            Type baseType = typeof(Component);

            // Parse arguments
            for (int i = 0; i < args.Count; i++)
            {
                string arg = context.ResolveStringReference(args[i]);
                if (arg == "--base" && ++i < args.Count)
                {
                    string baseTypeName = context.ResolveStringReference(args[i]);
                    Type resolvedType = TypeUtility.FindType(baseTypeName);
                    if (resolvedType == null)
                        throw new ArgumentException($"Base type not found: {baseTypeName}");
                    baseType = resolvedType;
                }
                else if (pattern == null)
                {
                    pattern = arg;
                }
                else
                {
                    throw new ArgumentException($"Unexpected argument: {arg}");
                }
            }

            if (pattern == null)
                throw new ArgumentException("Search pattern required");

            var matchingTypes = TypeUtility.GetDerivedTypes(baseType, pattern);

            if (!matchingTypes.Any())
                return "No components found matching the pattern.";

            return string.Join("\n", matchingTypes.Select(t => t.FullName));
        }

        private async Task<string> RemoveComponents(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Object selector and component name(s) required");

            string selector = args[0];
            var componentNames = args[1].Split(',').Select(n => n.Trim()).ToList();

            var objects = (await context.ResolveObjectReference(selector)).OfType<GameObject>().ToList();
            if (!objects.Any())
                return $"No objects found matching selector: {selector}";

            foreach (var obj in objects)
            {
                foreach (var componentName in componentNames)
                {
                    var components = obj.GetComponents<Component>()
                        .Where(c => c != null && c.GetType().Name.Equals(componentName, StringComparison.OrdinalIgnoreCase));

                    foreach (var component in components)
                    {
                        if (component is Transform)
                            continue; // Skip Transform component as it's required

                        Undo.DestroyObjectImmediate(component);
                    }
                }
            }

            return $"Removed {componentNames.Count} component(s) from {objects.Count} object(s)";
        }
    }
}
