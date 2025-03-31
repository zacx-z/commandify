using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;

namespace Commandify
{
    public class ListCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            bool showComponents = false;
            string filterPattern = null;
            IEnumerable<Object> objects = null;
            var format = ObjectFormatter.OutputFormat.Default;

            // Parse arguments
            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];

                switch (context.ResolveStringReference(arg))
                {
                    case "--format":
                        if (++i < args.Count)
                        {
                            string formatStr = context.ResolveStringReference(args[i]).ToLower();
                            switch (formatStr)
                            {
                                case "instance-id":
                                case "instanceid":
                                    format = ObjectFormatter.OutputFormat.InstanceId;
                                    break;
                                case "path":
                                    format = ObjectFormatter.OutputFormat.Path;
                                    break;
                                default:
                                    format = ObjectFormatter.OutputFormat.Default;
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
                        objects = context.ResolveObjectReference(arg);
                        break;
                }
            }

            if (objects == null) {
                throw new ArgumentException("Selector required");
            }

            // Filter objects if pattern is specified
            if (!string.IsNullOrEmpty(filterPattern))
            {
                var regex = new Regex(WildcardToRegex(filterPattern), RegexOptions.IgnoreCase);
                objects = objects.Where(obj => regex.IsMatch(obj.name));
            }

            var results = new List<string>();
            foreach (var obj in objects)
            {
                string info = ObjectFormatter.FormatObject(obj, format);
                if (showComponents && obj is GameObject go)
                {
                    var components = go.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => c.GetType().Name);
                    info += $" [{string.Join(", ", components)}]";
                }
                results.Add(info);
            }

            if (!results.Any())
                return null;

            // Store the found objects in the result variable
            context.SetLastResult(objects);
            return string.Join("\n", results.OrderBy(r => r));
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
