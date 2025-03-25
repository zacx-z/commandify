using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Commandify
{
    public class AssetCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No asset subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "list":
                    return ListAssets(subArgs, context);
                case "create":
                    return CreateAsset(subArgs, context);
                case "move":
                    return MoveAsset(subArgs, context);
                case "create-types":
                    return ListCreateTypes();
                default:
                    throw new ArgumentException($"Unknown asset subcommand: {subCommand}");
            }
        }

        private string ListAssets(List<string> args, CommandContext context)
        {
            bool recursive = false;
            string filterSpec = null;
            string path = null;

            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];

                switch (arg)
                {
                    case "--filter":
                        if (++i < args.Count)
                            filterSpec = context.ResolveStringReference(args[i]);
                        break;
                    case "--recursive":
                        recursive = true;
                        break;
                    default:
                        if (path == null)
                            path = context.ResolveStringReference(arg);
                        break;
                }
            }

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("No path specified");

            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path.TrimStart('/');

            var assets = new List<string>();
            string[] guids;

            if (recursive)
            {
                guids = AssetDatabase.FindAssets(filterSpec ?? "", new[] { path });
                foreach (var guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(assetPath))
                        assets.Add(assetPath);
                }
            }
            else
            {
                if (Directory.Exists(path))
                {
                    var entries = Directory.GetFileSystemEntries(path);
                    foreach (var entry in entries)
                    {
                        string relativePath = entry.Replace('\\', '/');
                        if (filterSpec == null || relativePath.Contains(filterSpec))
                            assets.Add(relativePath);
                    }
                }
                else if (File.Exists(path))
                {
                    assets.Add(path);
                }
            }

            if (!assets.Any())
                return "No assets found";

            // Store the assets in the result variable
            var loadedAssets = assets.Select(p => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p))
                                   .Where(a => a != null);
            context.SetLastResult(loadedAssets);

            return string.Join("\n", assets.OrderBy(p => p));
        }

        private string CreateAsset(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Asset type and path required");

            string assetType = args[0];
            string path = args[1];

            // Handle variable references
            assetType = context.ResolveStringReference(assetType);
            path = context.ResolveStringReference(path);

            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path.TrimStart('/');

            // Ensure directory exists
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Create the asset
            ScriptableObject asset = null;
            var type = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .FirstOrDefault(t => t.Name.Equals(assetType, StringComparison.OrdinalIgnoreCase));

            if (type == null)
                throw new ArgumentException($"Unknown asset type: {assetType}");

            asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            // Store the created asset in the result variable
            context.SetLastResult(asset);

            return $"Created {assetType} asset at {path}";
        }

        private string MoveAsset(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Source and destination paths required");

            string sourcePath = args[0];
            string destPath = args[1];

            // Handle variable references
            sourcePath = context.ResolveStringReference(sourcePath);
            destPath = context.ResolveStringReference(destPath);

            sourcePath = sourcePath.Replace('\\', '/');
            destPath = destPath.Replace('\\', '/');

            if (!sourcePath.StartsWith("Assets/"))
                sourcePath = "Assets/" + sourcePath.TrimStart('/');
            if (!destPath.StartsWith("Assets/"))
                destPath = "Assets/" + destPath.TrimStart('/');

            if (!File.Exists(sourcePath))
                throw new ArgumentException($"Source asset not found: {sourcePath}");

            // Ensure destination directory exists
            string destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            string error = AssetDatabase.MoveAsset(sourcePath, destPath);
            if (!string.IsNullOrEmpty(error))
                throw new Exception($"Failed to move asset: {error}");

            AssetDatabase.Refresh();

            // Store the moved asset in the result variable
            var movedAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destPath);
            context.SetLastResult(movedAsset);

            return $"Moved asset from {sourcePath} to {destPath}";
        }

        private string ListCreateTypes()
        {
            var types = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name)
                .Select(t => t.Name);

            return string.Join("\n", types);
        }
    }
}
