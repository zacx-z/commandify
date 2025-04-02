using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Commandify
{
    public class PrefabCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No prefab subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "create":
                    return CreatePrefab(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown prefab subcommand: {subCommand}");
            }
        }

        private string CreatePrefab(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Usage: prefab create [--variant] <selector> <path>");

            bool isVariant = false;
            string selector = null;
            string path = null;

            // Parse arguments
            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];
                if (arg == "--variant")
                {
                    isVariant = true;
                }
                else if (selector == null)
                {
                    selector = arg;
                }
                else if (path == null)
                {
                    path = arg;
                }
                else
                {
                    throw new ArgumentException("Too many arguments");
                }
            }

            if (selector == null || path == null)
                throw new ArgumentException("Both selector and path must be specified");

            var selectedObjects = context.ResolveObjectReference(selector).ToArray();
            if (selectedObjects.Length == 0)
                throw new ArgumentException($"No objects found matching selector: {selector}");

            var sourceObject = selectedObjects[0] as GameObject;
            if (sourceObject == null)
                throw new ArgumentException($"Selected object is not a GameObject: {selector}");

            // Ensure path has .prefab extension
            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                path += ".prefab";

            GameObject prefab;
            if (isVariant)
            {
                var originalPrefab = PrefabUtility.GetCorrespondingObjectFromSource(sourceObject);
                if (originalPrefab == null)
                    throw new ArgumentException("Source object is not a prefab instance");
                prefab = PrefabUtility.SaveAsPrefabAsset(sourceObject, path);
            }
            else
            {
                prefab = PrefabUtility.SaveAsPrefabAsset(sourceObject, path);
            }

            if (prefab == null)
                throw new Exception("Failed to create prefab");

            context.SetLastResult(prefab);
            return $"Created prefab at {path}";
        }
    }
}
