using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Commandify
{
    public class AssetCommandHandler : ICommandHandler
    {
        public async Task<string> ExecuteAsync(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No asset subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "search":
                    return SearchAssets(subArgs, context);
                case "create":
                    return CreateAsset(subArgs, context);
                case "move":
                    return MoveAsset(subArgs, context);
                case "create-types":
                    return ListCreateTypes();
                case "thumbnail":
                    return await GetThumbnails(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown asset subcommand: {subCommand}");
            }
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

        private async Task<string> GetThumbnails(List<string> args, CommandContext context)
        {
            if (args.Count < 1)
                throw new ArgumentException("No selector specified for thumbnail command");

            var objects = (await context.ResolveObjectReference(args[0])).ToList();
            if (!objects.Any())
                return "No objects found matching selector";

            var thumbnails = new List<(string name, string data)>();
            foreach (var obj in objects)
            {
                var texture = AssetPreview.GetAssetPreview(obj);
                if (texture == null) {
                    while (AssetPreview.IsLoadingAssetPreview(obj.GetInstanceID()))
                    {
                        await MainThreadUtility.Delay(100); // Wait for 100ms before checking again
                    }
                    texture = AssetPreview.GetAssetPreview(obj);
                }
                if (texture != null)
                {
                    byte[] bytes = texture.EncodeToPNG();
                    if (bytes != null && bytes.Length > 0)
                    {
                        string base64 = Convert.ToBase64String(bytes);
                        thumbnails.Add((obj.name, $"data:image/png;base64,{base64}"));
                    }
                }
                else
                    thumbnails.Add((obj.name, "No thumbnail available"));
            }

            if (!thumbnails.Any())
                return "No thumbnails available for selected objects";

            // Store the thumbnails in the result variable
            context.SetLastResult(thumbnails);

            return string.Join("\n", thumbnails.Select(t => $"{t.name}:\n{t.data}"));
        }

        private string SearchAssets(List<string> args, CommandContext context)
        {
            string[] folders = null;
            string format = null;
            string query = null;

            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];

                switch (arg)
                {
                    case "--folder":
                    case "--folders":
                        if (++i < args.Count)
                        {
                            string folderArg = context.ResolveStringReference(args[i]);
                            if (folderArg.StartsWith("[") && folderArg.EndsWith("]"))
                            {
                                // Parse array format [folder1, folder2, ...]
                                folders = folderArg.Trim('[', ']')
                                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(f => f.Trim())
                                    .ToArray();
                            }
                            else
                            {
                                folders = new[] { folderArg };
                            }

                            // Ensure all folders start with Assets/
                            folders = folders.Select(f => 
                            {
                                f = f.Replace('\\', '/');
                                return f.StartsWith("Assets/") ? f : "Assets/" + f.TrimStart('/');
                            }).ToArray();
                        }
                        break;
                    case "--format":
                        if (++i < args.Count)
                            format = context.ResolveStringReference(args[i]);
                        break;
                    default:
                        if (query == null)
                            query = context.ResolveStringReference(arg);
                        break;
                }
            }

            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("No search query specified");

            // If no folders specified, search in entire Assets folder
            if (folders == null || folders.Length == 0)
                folders = new[] { "Assets" };

            var guids = AssetDatabase.FindAssets(query, folders);
            var assets = new List<string>();

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(assetPath))
                    assets.Add(assetPath);
            }

            if (!assets.Any())
                return "No assets found";

            // Store the assets in the result variable
            var loadedAssets = assets.Select(p => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p))
                                   .Where(a => a != null).ToList();
            context.SetLastResult(loadedAssets);

            ObjectFormatter.OutputFormat outputFormat = ObjectFormatter.OutputFormat.Default;
            if (format?.ToLower() == "path")
                outputFormat = ObjectFormatter.OutputFormat.Path;
            else if (format?.ToLower() == "instance-id" || format?.ToLower() == "instanceid")
                outputFormat = ObjectFormatter.OutputFormat.InstanceId;

            var formattedAssets = loadedAssets.Select(a => ObjectFormatter.FormatObject(a, outputFormat));
            return string.Join("\n", formattedAssets);
        }
    }
}
