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
            if (args.Count < 1)
                throw new ArgumentException("Usage: prefab instantiate [selector] <hierarchy-path> [--prefab prefab-selector]");

            UnityEngine.Object prefabObject = null;
            string hierarchyPath = null;
            UnityEngine.Object parentPrefabObject = null;

            // Parse arguments
            for (int i = 0; i < args.Count; i++)
            {
                string arg = context.ResolveStringReference(args[i]);
                if (arg.StartsWith("--"))
                {
                    if (arg == "--prefab" && i + 1 < args.Count)
                    {
                        var selectedObjects = context.ResolveObjectReference(args[++i]).ToArray();
                        if (selectedObjects.Length == 0)
                            throw new ArgumentException($"No objects found matching selector: {args[i]}");
                        parentPrefabObject = selectedObjects[0];
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown option: {arg}");
                    }
                }
                else if (hierarchyPath == null && i == args.Count - 1)
                {
                    hierarchyPath = arg;
                }
                else if (prefabObject == null)
                {
                    var selectedObjects = context.ResolveObjectReference(args[i]).ToArray();
                    if (selectedObjects.Length == 0)
                        throw new ArgumentException($"No objects found matching selector: {args[i]}");
                    prefabObject = selectedObjects[0];
                }
                else
                {
                    throw new ArgumentException("Too many arguments");
                }
            }

            if (hierarchyPath == null)
                throw new ArgumentException("No hierarchy path specified");

            if (prefabObject)
                throw new ArgumentException("No prefab selector specified");

            if (!PrefabUtility.IsPartOfPrefabAsset(prefabObject))
                throw new ArgumentException($"Selected object is not a prefab: {prefabObject.name}");

            // Create parent hierarchy if needed
            var parentPath = System.IO.Path.GetDirectoryName(hierarchyPath)?.Replace('\\', '/');
            var objectName = System.IO.Path.GetFileName(hierarchyPath);
            
            Transform parent = null;
            GameObject parentPrefabRoot = null;
            bool isParentPrefabAsset = false;

            if (parentPrefabObject != null)
            {
                // Get the prefab stage or open it
                var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage == null || prefabStage.prefabAssetPath != AssetDatabase.GetAssetPath(parentPrefabObject))
                {
                    prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(parentPrefabObject));
                    if (prefabStage == null)
                        throw new Exception($"Failed to open prefab stage for: {parentPrefabObject.name}");
                }
            }

            if (!string.IsNullOrEmpty(parentPath))
            {
                if (parentPrefabObject == null)
                {
                    var parentObject = GameObject.Find(parentPath);
                    if (parentObject == null)
                        throw new ArgumentException($"Parent object not found: {parentPath}");
                    parent = parentObject.transform;
                }
                else
                {
                    // Try to find in prefab stage if we're in prefab mode
                    var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                    if (prefabStage != null)
                    {
                        parent = prefabStage.prefabContentsRoot.transform.Find(parentPath);
                        if (parent != null)
                        {
                            isParentPrefabAsset = true;
                            parentPrefabRoot = prefabStage.prefabContentsRoot;
                        }
                    }

                    if (parent == null)
                        throw new ArgumentException($"Parent path not found: {parentPath}");
                }
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabObject);
            if (instance == null)
                throw new Exception($"Failed to instantiate prefab: {prefabObject.name}");

            instance.name = objectName;
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);

                if (isParentPrefabAsset)
                {
                    // When in prefab mode, we need to apply the changes to the prefab asset
                    UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                    if (prefabStage != null)
                    {
                        EditorUtility.SetDirty(parentPrefabRoot);
                    }
                }
                else if (PrefabUtility.IsPartOfPrefabInstance(parent))
                {
                    // If parent is a prefab instance, record the modification
                    var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(parent.gameObject);
                    if (prefabRoot != null)
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(prefabRoot);
                    }
                }
            }

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
            Selection.activeObject = instance;

            return $"Instantiated prefab {prefabObject.name} at {hierarchyPath}";
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
