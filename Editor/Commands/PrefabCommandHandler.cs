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
                case "instantiate":
                    return InstantiatePrefab(subArgs, context);
                case "create":
                    return CreatePrefab(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown prefab subcommand: {subCommand}");
            }
        }

        private string InstantiatePrefab(List<string> args, CommandContext context)
        {
            if (args.Count != 2)
                throw new ArgumentException("Usage: prefab instantiate <selector> <hierarchy-path>");

            string selector = args[0];
            string hierarchyPath = args[1];

            var selectedObjects = context.ResolveObjectReference(selector).ToArray();
            if (selectedObjects.Length == 0)
                throw new ArgumentException($"No objects found matching selector: {selector}");

            var firstObject = selectedObjects[0];
            if (!PrefabUtility.IsPartOfPrefabAsset(firstObject))
                throw new ArgumentException($"Selected object is not a prefab: {firstObject.name}");

            // Create parent hierarchy if needed
            var parentPath = System.IO.Path.GetDirectoryName(hierarchyPath)?.Replace('\\', '/');
            var objectName = System.IO.Path.GetFileName(hierarchyPath);
            
            Transform parent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentObject = GameObject.Find(parentPath);
                if (parentObject == null)
                    throw new ArgumentException($"Parent path not found: {parentPath}");
                parent = parentObject.transform;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(firstObject);
            if (instance == null)
                throw new Exception($"Failed to instantiate prefab: {firstObject.name}");

            instance.name = objectName;
            if (parent != null)
                instance.transform.SetParent(parent, false);

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
            Selection.activeObject = instance;

            return $"Instantiated prefab {firstObject.name} at {hierarchyPath}";
        }

        private string CreatePrefab(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Object selector and prefab path required");

            var objects = context.ResolveObjectReference(args[0]);
            string prefabPath = args[1];

            // Handle variable reference in path
            prefabPath = context.ResolveStringReference(prefabPath);

            // Ensure path has .prefab extension
            if (!prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                prefabPath += ".prefab";

            var createdPrefabs = new List<GameObject>();
            foreach (var obj in objects)
            {
                if (obj is GameObject go)
                {
                    // Create unique path for multiple objects
                    string uniquePath = objects.Count() > 1 ? 
                        System.IO.Path.ChangeExtension(prefabPath, null) + "_" + go.name + ".prefab" : 
                        prefabPath;

                    // Create the prefab
                    bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(uniquePath) != null;
                    GameObject prefab;
                    
                    if (exists)
                    {
                        // Update existing prefab
                        prefab = PrefabUtility.SaveAsPrefabAsset(go, uniquePath);
                    }
                    else
                    {
                        // Create new prefab
                        prefab = PrefabUtility.SaveAsPrefabAsset(go, uniquePath);
                    }

                    if (prefab != null)
                        createdPrefabs.Add(prefab);
                }
            }

            if (!createdPrefabs.Any())
                return "No prefabs created";

            // Store created prefabs in result
            context.SetLastResult(createdPrefabs);
            return $"Created {createdPrefabs.Count} prefab(s)";
        }
    }
}
