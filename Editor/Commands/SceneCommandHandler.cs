using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Commandify
{
    public class SceneCommandHandler : ICommandHandler
    {
        public async Task<string> ExecuteAsync(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No scene subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "list":
                    return ListScenes(subArgs);
                case "open":
                    return OpenScene(subArgs, context);
                case "new":
                    return NewScene(subArgs, context);
                case "save":
                    return SaveScene(context);
                case "unload":
                    return UnloadScene(subArgs, context);
                case "activate":
                    return ActivateScene(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown scene subcommand: {subCommand}");
            }
        }

        private string ListScenes(List<string> args)
        {
            var scenes = new List<string>();
            bool showBuild = args.Contains("--build");
            bool showOpened = args.Contains("--opened");
            bool showActive = args.Contains("--active");
            bool showAll = !showBuild && !showOpened && !showActive;

            var buildScenes = EditorBuildSettings.scenes;
            var openedScenes = Enumerable.Range(0, EditorSceneManager.sceneCount)
                .Select(i => EditorSceneManager.GetSceneAt(i))
                .ToList();
            var activeScene = EditorSceneManager.GetActiveScene();

            IEnumerable<string> scenePaths
                = showBuild ? buildScenes.Select(s => s.path)
                : showOpened ? openedScenes.Select(s => s.path)
                : showActive ? new[] { activeScene.path }
                : AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath);

            int maxBuildIndex = buildScenes.Length - 1;
            int padding = maxBuildIndex.ToString().Length;

            foreach (var path in scenePaths)
            {
                var prefixes = new List<string>();
                var buildIndex = Array.FindIndex(buildScenes, s => s.path == path);
                if (buildIndex != -1)
                {
                    prefixes.Add($"Build {buildIndex.ToString().PadLeft(padding)} ({(buildScenes[buildIndex].enabled ? "Enabled" : "Disabled")})");
                }

                var openedScene = openedScenes.FirstOrDefault(s => s.path == path);
                if (openedScene.IsValid())
                {
                    prefixes.Add(openedScene.isLoaded ? "Loaded" : "Unloaded");
                    if (openedScene == activeScene)
                    {
                        prefixes.Add("Active");
                    }
                }

                if (prefixes.Any())
                {
                    scenes.Add($"[{string.Join(", ", prefixes)}] {path}");
                }
                else
                {
                    scenes.Add(path);
                }
            }

            scenes.Sort();

            return scenes.Any() ? string.Join("\n", scenes) : "No scenes found";
        }

        private string OpenScene(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Scene path or index required");

            string scenePathOrIndex = args[0];
            bool additive = args.Contains("--additive");

            // Handle variable reference
            scenePathOrIndex = context.ResolveStringReference(scenePathOrIndex);

            string scenePath;
            if (int.TryParse(scenePathOrIndex, out int buildIndex))
            {
                if (buildIndex < 0 || buildIndex >= EditorBuildSettings.scenes.Length)
                    throw new ArgumentException($"Invalid build index: {buildIndex}");
                scenePath = EditorBuildSettings.scenes[buildIndex].path;
            }
            else
            {
                scenePath = scenePathOrIndex;
                if (!scenePath.EndsWith(".unity"))
                    scenePath += ".unity";
                if (!File.Exists(scenePath))
                    throw new ArgumentException($"Scene not found: {scenePath}");
            }

            var mode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(scenePath, mode);

            // Store the opened scene in the result variable
            context.SetLastResult(scene);

            return $"Opened scene: {scene.path}";
        }

        private string NewScene(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Scene path required");

            string path = args[0];
            string templateName = args.Count > 1 ? args[1] : null;

            // Handle variable reference for path
            path = context.ResolveStringReference(path);
            if (!path.EndsWith(".unity"))
                path += ".unity";

            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            // Create new scene
            Scene scene;
            NewSceneSetup setup = NewSceneSetup.DefaultGameObjects;
            NewSceneMode mode = NewSceneMode.Single;

            if (!string.IsNullOrEmpty(templateName))
            {
                // Try to find the template scene
                string templatePath = AssetDatabase.FindAssets($"t:Scene {templateName}")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Equals(templateName, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(templatePath))
                    throw new ArgumentException($"Scene template not found: {templateName}");

                // Create empty scene first
                var emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
                
                // Open template in additive mode
                var templateScene = EditorSceneManager.OpenScene(templatePath, OpenSceneMode.Additive);
                
                // Copy all objects from template to new scene
                foreach (var rootObj in templateScene.GetRootGameObjects())
                {
                    UnityEngine.Object.Instantiate(rootObj);
                }

                // Close template scene
                EditorSceneManager.CloseScene(templateScene, true);

                // Use the empty scene we created
                scene = emptyScene;
            }
            else
            {
                scene = EditorSceneManager.NewScene(setup, mode);
            }
            
            // Save the scene to the specified path
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new Exception($"Failed to save scene to path: {path}");

            // Store the new scene in the result variable
            context.SetLastResult(scene);
            return $"Created and saved new scene at: {path}";
        }

        private string SaveScene(CommandContext context)
        {
            if (EditorSceneManager.SaveOpenScenes())
            {
                var scene = EditorSceneManager.GetActiveScene();
                // Store the saved scene in the result variable
                context.SetLastResult(scene);
                return $"Saved scene: {scene.path}";
            }
            throw new Exception("Failed to save scene(s)");
        }

        private string UnloadScene(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Scene name or index required");

            string sceneNameOrIndex = args[0];

            // Handle variable reference
            sceneNameOrIndex = context.ResolveStringReference(sceneNameOrIndex);

            var scene = GetSceneByNameOrIndex(sceneNameOrIndex);
            if (!scene.IsValid())
                throw new ArgumentException($"Scene not found: {sceneNameOrIndex}");

            if (EditorSceneManager.sceneCount == 1)
                throw new ArgumentException("Cannot unload the last scene");

            EditorSceneManager.CloseScene(scene, true);
            return $"Unloaded scene: {scene.path}";
        }

        private string ActivateScene(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Scene name or index required");

            string sceneNameOrIndex = args[0];

            // Handle variable reference
            sceneNameOrIndex = context.ResolveStringReference(sceneNameOrIndex);

            var scene = GetSceneByNameOrIndex(sceneNameOrIndex);
            if (!scene.IsValid())
                throw new ArgumentException($"Scene not found: {sceneNameOrIndex}");

            EditorSceneManager.SetActiveScene(scene);
            // Store the activated scene in the result variable
            context.SetLastResult(scene);
            return $"Activated scene: {scene.path}";
        }

        private UnityEngine.SceneManagement.Scene GetSceneByNameOrIndex(string nameOrIndex)
        {
            if (int.TryParse(nameOrIndex, out int index))
            {
                if (index >= 0 && index < EditorSceneManager.sceneCount)
                    return EditorSceneManager.GetSceneAt(index);
            }
            else
            {
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    if (scene.name == nameOrIndex || scene.path == nameOrIndex)
                        return scene;
                }
            }
            return new UnityEngine.SceneManagement.Scene();
        }
    }
}
