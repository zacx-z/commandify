using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Commandify
{
    public class PackageCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No package subcommand specified");

            string subCommand = args[0].ToLower();
            var subArgs = args.Skip(1).ToList();

            switch (subCommand)
            {
                case "list":
                    return ListPackages(context);
                case "install":
                    if (subArgs.Count == 0)
                        throw new ArgumentException("Package identifier required");
                    return InstallPackage(subArgs[0], context);
                default:
                    throw new ArgumentException($"Unknown package subcommand: {subCommand}");
            }
        }

        private string ListPackages(CommandContext context)
        {
            var listRequest = Client.List(true); // Include dependencies
            WaitForRequest(listRequest);

            if (listRequest.Status == StatusCode.Failure)
                throw new Exception($"Failed to list packages: {listRequest.Error?.message}");

            var packages = listRequest.Result
                .OrderBy(p => p.name)
                .Select(p => FormatPackageInfo(p));

            // Store packages in result variable
            context.SetLastResult(listRequest.Result);
            return string.Join("\n", packages);
        }

        private string InstallPackage(string packageId, CommandContext context)
        {
            // Handle variable reference
            if (packageId.StartsWith("$"))
                packageId = context.ResolveStringReference(packageId);

            if (string.IsNullOrEmpty(packageId))
                throw new ArgumentException("Invalid package identifier");

            // Check if it's a registry package (no path or URL)
            if (!packageId.Contains("/") && !packageId.Contains("\\") && !packageId.Contains(":"))
            {
                // If no version is specified, append @latest
                if (!packageId.Contains("@"))
                    packageId += "@latest";
            }

            var addRequest = Client.Add(packageId);
            WaitForRequest(addRequest);

            if (addRequest.Status == StatusCode.Failure)
                throw new Exception($"Failed to install package: {addRequest.Error?.message}");

            var package = addRequest.Result;
            // Store installed package in result variable
            context.SetLastResult(package);
            return $"Installed package: {FormatPackageInfo(package)}";
        }

        private string FormatPackageInfo(UnityEditor.PackageManager.PackageInfo package)
        {
            string source = package.source switch
            {
                PackageSource.Registry => "[Registry]",
                PackageSource.Local => "[Local]",
                PackageSource.Embedded => "[Embedded]",
                PackageSource.Git => "[Git]",
                PackageSource.BuiltIn => "[Built-in]",
                _ => "[Unknown]"
            };

            return $"{source} {package.name}@{package.version}";
        }

        private void WaitForRequest(Request request)
        {
            while (!request.IsCompleted)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Package Manager",
                    "Processing package operation...",
                    0.5f))
                {
                    EditorUtility.ClearProgressBar();
                    throw new OperationCanceledException("Package operation cancelled");
                }
                Thread.Sleep(100);
            }
            EditorUtility.ClearProgressBar();
        }
    }
}
