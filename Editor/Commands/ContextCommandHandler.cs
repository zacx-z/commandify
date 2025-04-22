using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using UnityEditor.SceneManagement;
namespace Commandify
{
    public class ContextCommandHandler : ICommandHandler
    {
        // Available context variables
        private static readonly List<string> availableContexts = new List<string>
        {
            "parent",
            "selection"
        };
        public async Task<string> ExecuteAsync(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Context variable name required");
            string contextName = args[0].ToLower();
            // Validate context variable name
            if (!availableContexts.Contains(contextName))
                throw new ArgumentException($"Unknown context variable: {contextName}. Available context variables: {string.Join(", ", availableContexts)}");
            // Case: context <varname> --clear
            if (args.Count > 1 && args[1] == "--clear")
            {
                if (contextName == "parent")
                {
                    // Clear the default parent using Unity's built-in functionality
                    EditorUtility.ClearDefaultParentObject();
                    context.SetLastResult(null);
                    return $"Cleared context variable: {contextName}";
                }
                else if (contextName == "selection")
                {
                    // Clear the selection
                    Selection.objects = new UnityEngine.Object[0];
                    context.SetLastResult(Selection.objects);
                    return $"Cleared context variable: {contextName}";
                }
            }
            // Case: context <varname> <value>
            else if (args.Count > 1)
            {
                string valueStr = args[1];
                // Handle setting the context variable
                if (contextName == "parent")
                {
                    // For parent, we need to resolve the object reference
                    var objects = await context.ResolveObjectReference(valueStr);
                    var parentObj = objects.FirstOrDefault() as GameObject;
                    if (parentObj == null)
                        throw new ArgumentException("Parent context must be a GameObject");
                    // Set the default parent using Unity's built-in functionality
                    EditorUtility.SetDefaultParentObject(parentObj);
                    // Set last result to the parent GameObject
                    context.SetLastResult(parentObj);
                    return $"Set context {contextName} = {parentObj.name}";
                }
                else if (contextName == "selection")
                {
                    // For selection, we resolve object references and set the Selection.objects directly
                    var objects = await context.ResolveObjectReference(valueStr);
                    var objectsList = objects.ToList();
                    if (objectsList.Count == 0)
                        throw new ArgumentException("No objects found for selection context");
                    // Set the selection using Unity's built-in functionality
                    Selection.objects = objectsList.ToArray();
                    // Set last result to the selection objects
                    context.SetLastResult(objectsList);
                    return $"Set context {contextName} = {objectsList.Count} object(s)";
                }
                return $"Unknown context variable: {contextName}";
            }
            // Case: context <varname> (show current value)
            else
            {
                if (contextName == "parent")
                {
                    // Try to get the current default parent using reflection to access SceneHierarchy
                    Transform parentObject = GetDefaultParentObject();
                    if (parentObject != null)
                    {
                        context.SetLastResult(parentObject.gameObject);
                        return $"Context {contextName} = {parentObject.gameObject.name}";
                    }
                    else
                    {
                        context.SetLastResult(null);
                        return $"Context {contextName} is not set";
                    }
                }
                else if (contextName == "selection")
                {
                    // Get the current selection directly from Unity
                    var selectedObjects = Selection.objects;
                    if (selectedObjects == null || selectedObjects.Length == 0)
                        return $"Context {contextName} is not set (no objects selected)";
                    
                    // Set last result to the selection objects
                    context.SetLastResult(selectedObjects);
                    
                    // Build a list of all selected object names
                    var objectNames = new List<string>();
                    foreach (var obj in selectedObjects)
                    {
                        objectNames.Add(obj.name);
                    }
                    
                    // Format the response with both count and names
                    return $"Context {contextName} = {selectedObjects.Length} object(s):\n{string.Join("\n", objectNames)}";
                }
            }
            return $"Unknown context variable: {contextName}";
        }
        // Helper method to get the current default parent object using reflection
        public static Transform GetDefaultParentObject()
        {
            try
            {
                // Get the current prefab stage or active scene GUID
                var prefabStageType = Type.GetType("UnityEditor.SceneManagement.PrefabStageUtility, UnityEditor") ??
                                     Type.GetType("UnityEditor.Experimental.SceneManagement.PrefabStageUtility, UnityEditor");
                var getCurrentPrefabStageMethod = prefabStageType?.GetMethod("GetCurrentPrefabStage",
                                                                        BindingFlags.Public | BindingFlags.Static);
                var prefabStage = getCurrentPrefabStageMethod?.Invoke(null, null);
                string activeSceneGUID;
                if (prefabStage != null)
                {
                    var sceneProperty = prefabStage.GetType().GetProperty("scene");
                    var scene = sceneProperty.GetValue(prefabStage);
                    var guidProperty = scene.GetType().GetProperty("guid");
                    activeSceneGUID = (string)guidProperty.GetValue(scene);
                }
                else
                {
                    // Access the guid property of the active scene using reflection
                    var activeScene = EditorSceneManager.GetActiveScene();
                    var sceneType = typeof(UnityEngine.SceneManagement.Scene);
                    var guidProperty = sceneType.GetProperty("guid", BindingFlags.NonPublic | BindingFlags.Instance) ??
                                      sceneType.GetProperty("guid", BindingFlags.Public | BindingFlags.Instance);
                    if (guidProperty != null)
                    {
                        activeSceneGUID = (string)guidProperty.GetValue(activeScene);
                    }
                    else
                    {
                        // Fallback: try to get the guid field
                        var guidField = sceneType.GetField("guid", BindingFlags.NonPublic | BindingFlags.Instance) ??
                                       sceneType.GetField("guid", BindingFlags.Public | BindingFlags.Instance);
                        if (guidField != null)
                        {
                            activeSceneGUID = (string)guidField.GetValue(activeScene);
                        }
                        else
                        {
                            Debug.LogError("Could not access Scene.guid via reflection");
                            return null;
                        }
                    }
                }
                // Get the SceneHierarchy type using reflection
                var sceneHierarchyType = Type.GetType("UnityEditor.SceneHierarchy, UnityEditor") ??
                                        Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.SceneHierarchy");
                if (sceneHierarchyType == null)
                    return null;
                // Get the GetDefaultParentForSession method
                var getDefaultParentMethod = sceneHierarchyType.GetMethod("GetDefaultParentForSession",
                                                                    BindingFlags.NonPublic | BindingFlags.Static);
                if (getDefaultParentMethod == null)
                    return null;
                // Call the method to get the instance ID
                int id = (int)getDefaultParentMethod.Invoke(null, new object[] { activeSceneGUID });
                if (id != 0)
                {
                    var objectFromInstanceID = EditorUtility.InstanceIDToObject(id) as GameObject;
                    return objectFromInstanceID?.transform;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting default parent: {ex.Message}");
            }
            return null;
        }
    }
}