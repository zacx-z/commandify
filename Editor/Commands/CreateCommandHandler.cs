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
                return "Usage: create [name] [--parent path/to/parent] [--with Component1,Component2,...] [--prefab prefab-selector]";

            string name = "GameObject";
            string parentPath = null;
            GameObject prefabObject = null;
            List<string> components = new List<string>();

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
                    else if (arg == "--prefab" && i + 1 < args.Count) {
                        var prefabSelector = args[++i];
                        var selectedObjects = context.ResolveObjectReference(prefabSelector).ToArray();
                        if (selectedObjects.Length == 0)
                            throw new ArgumentException($"No objects found matching prefab selector: {prefabSelector}");
                        prefabObject = selectedObjects[0] as GameObject;
                        if (prefabObject == null)
                            throw new ArgumentException($"Selected object is not a GameObject: {prefabSelector}");
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown option: {arg}");
                    }
                }
                else if (i == 0)
                {
                    name = arg;
                }
            }

            GameObject obj;
            Transform parentTransform = null;

            // Handle prefab context if specified
            if (prefabObject != null)
            {
                if (!PrefabUtility.IsPartOfPrefabAsset(prefabObject))
                    throw new ArgumentException($"Selected object is not a prefab: {prefabObject.name}");

                // Get the prefab stage or open it
                var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage == null || prefabStage.prefabAssetPath != AssetDatabase.GetAssetPath(prefabObject))
                {
                    prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(prefabObject));
                    if (prefabStage == null)
                        throw new Exception($"Failed to open prefab stage for: {prefabObject.name}");
                }

                // Find parent in prefab if specified
                if (!string.IsNullOrEmpty(parentPath))
                {
                    var parent = prefabStage.prefabContentsRoot.transform.Find(parentPath);
                    if (parent == null)
                        throw new ArgumentException($"Parent path not found in prefab: {parentPath}");
                    parentTransform = parent;
                }
                else
                {
                    parentTransform = prefabStage.prefabContentsRoot.transform;
                }

                obj = new GameObject(name);
                obj.transform.SetParent(parentTransform, false);
                Undo.RegisterCreatedObjectUndo(obj, "Create GameObject in Prefab");
            }
            else
            {
                // Create in scene
                obj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(obj, "Create GameObject");

                // Set parent if specified
                if (!string.IsNullOrEmpty(parentPath))
                {
                    var parent = GameObject.Find(parentPath);
                    if (parent == null)
                        throw new ArgumentException($"Parent object not found: {parentPath}");
                    
                    obj.transform.SetParent(parent.transform, false);
                }
            }

            // Add components
            foreach (var componentName in components)
            {
                try
                {
                    // Try to find the component type
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    Type componentType = null;

                    // First try exact name
                    componentType = assemblies.SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => typeof(Component).IsAssignableFrom(t) && 
                            (t.Name == componentName || t.Name == componentName + "Component"));

                    if (componentType == null)
                    {
                        // Try UnityEngine namespace
                        componentType = assemblies.SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => typeof(Component).IsAssignableFrom(t) && 
                                (t.FullName == "UnityEngine." + componentName || 
                                 t.FullName == "UnityEngine." + componentName + "Component"));
                    }

                    if (componentType == null)
                        throw new ArgumentException($"Component type not found: {componentName}");

                    Undo.AddComponent(obj, componentType);
                }
                catch (Exception e)
                {
                    // Clean up the created object if component addition fails
                    GameObject.DestroyImmediate(obj);
                    throw new ArgumentException($"Failed to add component {componentName}: {e.Message}");
                }
            }

            return ObjectFormatter.FormatObject(obj, ObjectFormatter.OutputFormat.Default);
        }
    }
}
