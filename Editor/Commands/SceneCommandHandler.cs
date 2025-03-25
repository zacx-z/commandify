using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Commandify
{
    public class SceneCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
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
                    return NewScene(context);
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

            if (showBuild)
            {
                for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                {
                    var scene = EditorBuildSettings.scenes[i];
                    scenes.Add($"[Build {i}] {scene.path} ({(scene.enabled ? "Enabled" : "Disabled")})");
                }
            }
            else if (showOpened)
            {
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    scenes.Add($"[Loaded] {scene.path}");
                }
            }
            else if (showActive)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                scenes.Add($"[Active] {activeScene.path}");
            }
            else
            {
                var guids = AssetDatabase.FindAssets("t:Scene");
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    scenes.Add($"[Asset] {path}");
                }
            }

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

        private string NewScene(CommandContext context)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            // Store the new scene in the result variable
            context.SetLastResult(scene);
            return "Created new scene";
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
