using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Commandify
{
    public class ComponentCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No component subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "list":
                    return ListComponents(subArgs, context);
                case "add":
                    return AddComponent(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown component subcommand: {subCommand}");
            }
        }

        private string ListComponents(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Object selector required");

            var objects = context.ResolveObjectReference(args[0]);
            var components = new List<string>();

            foreach (var obj in objects)
            {
                if (obj is GameObject go)
                {
                    var objComponents = go.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => $"{go.name}: {c.GetType().Name}");
                    components.AddRange(objComponents);
                }
            }

            if (!components.Any())
                return "No components found";

            // Store components in result variable
            context.SetLastResult(components);
            return string.Join("\n", components);
        }

        private string AddComponent(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Object selector and component type required");

            var objects = context.ResolveObjectReference(args[0]);
            string componentType = args[1];
            if (componentType.StartsWith("$"))
                componentType = context.ResolveStringReference(componentType);

            var type = GetComponentType(componentType);
            if (type == null)
                throw new ArgumentException($"Component type not found: {componentType}");

            var addedComponents = new List<Component>();
            foreach (var obj in objects)
            {
                if (obj is GameObject go)
                {
                    Undo.RecordObject(go, "Add Component");
                    var component = go.AddComponent(type);
                    if (component != null)
                        addedComponents.Add(component);
                }
            }

            // Store added components in result variable
            context.SetLastResult(addedComponents);
            return $"Added {addedComponents.Count} {componentType} component(s)";
        }

        private Type GetComponentType(string typeName)
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
    }
}
