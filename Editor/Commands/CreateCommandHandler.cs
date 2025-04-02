using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Commandify
{
    public class CreateCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                return @"Usage: create <name> [--parent path/to/parent] [--with Component1,Component2,...] [--prefab prefab-selector]
       create <source-selector> <name> [--parent path/to/parent] [--with Component1,Component2,...] [--prefab prefab-selector]";

            string name = null;
            string parentPath = null;
            GameObject sourceObject = null;
            List<string> components = new List<string>();
            GameObject targetPrefab = null;

            // Parse arguments
            for (int i = 0; i < args.Count; i++)
            {
                string arg = context.ResolveStringReference(args[i]);
                if (arg.StartsWith("--"))
                {
                    if (arg == "--parent" && i + 1 < args.Count)
                    {
                        parentPath = context.ResolveStringReference(args[++i]);
                    }
                    else if (arg == "--with" && i + 1 < args.Count)
                    {
                        components = context.ResolveStringReference(args[++i])
                            .Split(',')
                            .Select(c => c.Trim())
                            .Where(c => !string.IsNullOrEmpty(c))
                            .ToList();
                    }
                    else if (arg == "--prefab" && i + 1 < args.Count)
                    {
                        var prefabSelector = args[++i];
                        var selectedObjects = context.ResolveObjectReference(prefabSelector).ToArray();
                        if (selectedObjects.Length == 0)
                            throw new ArgumentException($"No objects found matching prefab selector: {prefabSelector}");
                        targetPrefab = selectedObjects[0] as GameObject;
                        if (targetPrefab == null)
                            throw new ArgumentException($"Selected object is not a GameObject: {prefabSelector}");
                    }
                    else if (arg == "--source" && i + 1 < args.Count)
                    {
                        var sourceSelector = args[++i];
                        var selectedObjects = context.ResolveObjectReference(sourceSelector).ToArray();
                        if (selectedObjects.Length == 0)
                            throw new ArgumentException($"No objects found matching source selector: {sourceSelector}");
                        sourceObject = selectedObjects[0] as GameObject;
                        if (sourceObject == null)
                            throw new ArgumentException($"Selected object is not a GameObject: {sourceSelector}");
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown option: {arg}");
                    }
                }
                else
                {
                    if (name == null)
                    {
                        name = arg;
                    }
                    else
                    {
                        throw new ArgumentException("Too many arguments");
                    }
                }
            }

            // If no name was specified, use source object name or default
            if (name == null)
            {
                name = sourceObject != null ? sourceObject.name : "GameObject";
            }

            GameObject obj;
            Transform parentTransform = null;

            // Handle prefab context if specified
            if (targetPrefab != null)
            {
                if (!PrefabUtility.IsPartOfPrefabAsset(targetPrefab))
                    throw new ArgumentException($"Selected object is not a prefab: {targetPrefab.name}");

                // Get the prefab stage or open it
                var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage == null || prefabStage.prefabAssetPath != AssetDatabase.GetAssetPath(targetPrefab))
                {
                    prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(targetPrefab));
                    if (prefabStage == null)
                        throw new Exception($"Failed to open prefab stage for: {targetPrefab.name}");
                }

                // Find parent in prefab if specified
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parentTransform = FindTransformInHierarchy(prefabStage.prefabContentsRoot.transform, parentPath);
                    if (parentTransform == null)
                        throw new ArgumentException($"Parent path not found in prefab: {parentPath}");
                }
                else
                {
                    parentTransform = prefabStage.prefabContentsRoot.transform;
                }

                if (sourceObject != null)
                {
                    // Instantiate from source
                    if (PrefabUtility.IsPartOfPrefabAsset(sourceObject))
                    {
                        obj = (GameObject)PrefabUtility.InstantiatePrefab(sourceObject);
                        obj.name = name;
                    }
                    else
                    {
                        obj = UnityEngine.Object.Instantiate(sourceObject);
                        obj.name = name;
                    }
                }
                else
                {
                    // Create new
                    obj = new GameObject(name);
                }

                obj.transform.SetParent(parentTransform, false);
                Undo.RegisterCreatedObjectUndo(obj, "Create GameObject in Prefab");
            }
            else
            {
                // Create in scene
                if (sourceObject != null)
                {
                    // Instantiate from source
                    if (PrefabUtility.IsPartOfPrefabAsset(sourceObject))
                    {
                        obj = (GameObject)PrefabUtility.InstantiatePrefab(sourceObject);
                        obj.name = name;
                    }
                    else
                    {
                        obj = UnityEngine.Object.Instantiate(sourceObject);
                        obj.name = name;
                    }
                }
                else
                {
                    // Create new
                    obj = new GameObject(name);
                }

                Undo.RegisterCreatedObjectUndo(obj, "Create GameObject");

                // Set parent if specified
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parentTransform = FindTransformInScene(parentPath);
                    if (parentTransform == null)
                        throw new ArgumentException($"Parent path not found in scene: {parentPath}");
                }

                if (parentTransform != null)
                {
                    Undo.SetTransformParent(obj.transform, parentTransform, "Set GameObject Parent");
                }
            }

            // Add components
            foreach (var componentName in components)
            {
                var componentType = TypeUtility.FindType(componentName);
                if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
                    throw new ArgumentException($"Invalid component type: {componentName}");
                Undo.AddComponent(obj, componentType);
            }

            Selection.activeGameObject = obj;
            context.SetLastResult(obj);
            return $"Created {obj.name}";
        }

        private Transform FindTransformInScene(string path)
        {
            var parts = path.Split('/');
            Transform current = null;

            foreach (var part in parts)
            {
                if (current == null)
                {
                    var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                    current = roots.FirstOrDefault(r => r.name == part)?.transform;
                    if (current == null)
                        return null;
                }
                else
                {
                    current = current.Find(part);
                    if (current == null)
                        return null;
                }
            }

            return current;
        }

        private Transform FindTransformInHierarchy(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;

            var parts = path.Split('/');
            Transform current = root;

            foreach (var part in parts)
            {
                current = current.Find(part);
                if (current == null)
                    return null;
            }

            return current;
        }
    }
}
