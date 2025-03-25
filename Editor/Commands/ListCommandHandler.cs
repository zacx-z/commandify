using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Commandify
{
    public class ListCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            bool showPath = false;
            bool showComponents = false;
            string filterPattern = null;
            string selector = null;

            // Parse arguments
            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];
                arg = context.ResolveStringReference(arg);

                switch (arg)
                {
                    case "--path":
                        showPath = true;
                        break;
                    case "--components":
                        showComponents = true;
                        break;
                    case "--filter":
                        if (++i < args.Count)
                            filterPattern = context.ResolveStringReference(args[i]);
                        break;
                    default:
                        if (selector == null)
                            selector = arg;
                        break;
                }
            }

            if (selector == null)
                throw new ArgumentException("Selector required");

            // Get objects using selector
            var objects = context.ResolveObjectReference(selector);

            // Filter objects if pattern is specified
            if (!string.IsNullOrEmpty(filterPattern))
            {
                var regex = new Regex(WildcardToRegex(filterPattern), RegexOptions.IgnoreCase);
                objects = objects.Where(obj => regex.IsMatch(obj.name));
            }

            var results = new List<string>();
            foreach (var obj in objects)
            {
                if (obj is GameObject go)
                {
                    string info = showPath ? GetObjectPath(go) : go.name;
                    if (showComponents)
                    {
                        var components = go.GetComponents<Component>()
                            .Where(c => c != null)
                            .Select(c => c.GetType().Name);
                        info += $" [{string.Join(", ", components)}]";
                    }
                    results.Add(info);
                }
                else
                {
                    results.Add(obj.name);
                }
            }

            if (!results.Any())
                return "No objects found";

            // Store the found objects in the result variable
            context.SetLastResult(objects);
            return string.Join("\n", results.OrderBy(r => r));
        }

        private string GetObjectPath(GameObject obj)
        {
            string path = obj.name;
            var parent = obj.transform.parent;
            while (parent != null)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }
            return path;
        }

        private string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".")
                + "$";
        }
    }
}
