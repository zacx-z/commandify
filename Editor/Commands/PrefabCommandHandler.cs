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
            if (args.Count == 0)
                throw new ArgumentException("Prefab path or reference required");

            GameObject prefab;
            string prefabPath = args[0];

            // Handle variable reference
            if (prefabPath.StartsWith("$"))
            {
                var value = context.ResolveReference(prefabPath);
                if (value is GameObject go)
                    prefab = go;
                else if (value is string path)
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                else
                    throw new ArgumentException($"Variable {prefabPath} does not contain a prefab or valid path");
            }
            else
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            if (prefab == null)
                throw new ArgumentException($"Prefab not found: {prefabPath}");

            // Get parent if specified
            Transform parent = null;
            if (args.Count > 1)
            {
                var parentObjects = context.ResolveObjectReference(args[1]);
                var firstParent = parentObjects.FirstOrDefault();
                if (firstParent != null)
                {
                    parent = firstParent is GameObject go ? go.transform :
                            firstParent is Component comp ? comp.transform : null;
                }
            }

            // Instantiate the prefab
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
                
                if (parent != null)
                {
                    instance.transform.SetParent(parent, false);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                }
            }

            // Store the instance in the result
            context.SetLastResult(instance);
            return $"Instantiated prefab: {instance.name}";
        }

        private string CreatePrefab(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Object selector and prefab path required");

            var objects = context.ResolveObjectReference(args[0]);
            string prefabPath = args[1];

            // Handle variable reference in path
            if (prefabPath.StartsWith("$"))
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
