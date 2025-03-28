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
        private enum OutputFormat
        {
            Default,
            InstanceId,
            Path,
            Full
        }

        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            bool showComponents = false;
            string filterPattern = null;
            string selector = null;
            var format = OutputFormat.Default;

            // Parse arguments
            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];
                arg = context.ResolveStringReference(arg);

                switch (arg)
                {
                    case "--format":
                        if (++i < args.Count)
                        {
                            string formatStr = context.ResolveStringReference(args[i]).ToLower();
                            switch (formatStr)
                            {
                                case "instance-id":
                                case "instanceid":
                                    format = OutputFormat.InstanceId;
                                    break;
                                case "path":
                                    format = OutputFormat.Path;
                                    break;
                                case "full":
                                    format = OutputFormat.Full;
                                    break;
                                default:
                                    format = OutputFormat.Default;
                                    break;
                            }
                        }
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
                string info;
                if (format == OutputFormat.InstanceId)
                {
                    info = $"@&{obj.GetInstanceID()}";
                }
                else if (obj is GameObject go)
                {
                    info = format == OutputFormat.Path || format == OutputFormat.Full ? GetObjectPath(go) : go.name;
                    if ((showComponents || format == OutputFormat.Full) && go != null)
                    {
                        var components = go.GetComponents<Component>()
                            .Where(c => c != null)
                            .Select(c => c.GetType().Name);
                        info += $" [{string.Join(", ", components)}]";
                    }
                }
                else
                {
                    info = obj.name;
                }
                results.Add(info);
            }

            if (!results.Any())
                return null;

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
