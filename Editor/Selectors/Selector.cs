using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

namespace Commandify
{
    public class Selector
    {
        private readonly string selectorString;
        private readonly Dictionary<string, object> variables;

        public Selector(string selectorString, Dictionary<string, object> variables)
        {
            this.selectorString = selectorString ?? throw new ArgumentNullException(nameof(selectorString));
            this.variables = variables ?? throw new ArgumentNullException(nameof(variables));
        }

        public IEnumerable<UnityEngine.Object> Evaluate()
        {
            var primarySelector = ParsePrimarySelector(selectorString, out var rangeSpecifier);
            var objects = EvaluatePrimarySelector(primarySelector);
            return ApplyRangeSpecifier(objects, rangeSpecifier);
        }

        private string ParsePrimarySelector(string input, out string rangeSpecifier)
        {
            rangeSpecifier = null;
            var parts = input.Split('#');
            if (parts.Length > 1)
            {
                rangeSpecifier = parts[1];
                return parts[0];
            }
            return input;
        }

        private IEnumerable<UnityEngine.Object> EvaluatePrimarySelector(string selector)
        {
            if (string.IsNullOrEmpty(selector))
                return Enumerable.Empty<UnityEngine.Object>();

            // Handle variable reference
            if (selector.StartsWith("$"))
            {
                string varName = selector.Substring(1);
                if (variables.TryGetValue(varName, out object value))
                {
                    if (value is IEnumerable<UnityEngine.Object> objects)
                        return objects;
                    if (value is UnityEngine.Object obj)
                        return new[] { obj };
                }
                return Enumerable.Empty<UnityEngine.Object>();
            }

            // Handle hierarchy path
            if (selector.StartsWith("^"))
            {
                return FindInHierarchy(selector.Substring(1));
            }

            // Handle tag
            if (selector.StartsWith("@#"))
            {
                string tag = selector.Substring(2);
                return GameObject.FindGameObjectsWithTag(tag).Cast<UnityEngine.Object>();
            }

            // Handle QuickSearch
            if (selector.StartsWith("@@"))
            {
                return QuickSearch(selector.Substring(2));
            }

            // Handle component type filter
            if (selector.Contains(":"))
            {
                var parts = selector.Split(':');
                var baseObjects = EvaluatePrimarySelector(parts[0]);
                return FilterByComponent(baseObjects, parts[1]);
            }

            // Handle intersection
            if (selector.Contains("&"))
            {
                var parts = selector.Split('&');
                var left = EvaluatePrimarySelector(parts[0]);
                var right = EvaluatePrimarySelector(parts[1]);
                return left.Intersect(right);
            }

            // Handle union
            if (selector.Contains("|"))
            {
                var parts = selector.Split('|');
                var left = EvaluatePrimarySelector(parts[0]);
                var right = EvaluatePrimarySelector(parts[1]);
                return left.Union(right);
            }

            // Default to asset path
            return FindAssets(selector);
        }

        private IEnumerable<UnityEngine.Object> FindInHierarchy(string path)
        {
            var parts = path.Split('/');
            var current = new List<GameObject>();

            // Start with root objects
            if (parts[0] == "**") {
                var matches = GetLoadedScenes().SelectMany(scene => scene.GetRootGameObjects())
                    .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                    .Select((t => t.gameObject));
                current.AddRange(matches);
            } else {
                var rootRegex = WildcardToRegex(parts[0]);
                var matches = GetLoadedScenes().SelectMany(scene => scene.GetRootGameObjects())
                    .Where(go => rootRegex.IsMatch(go.name));
                current.AddRange(matches);
            }

            // Navigate through hierarchy
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i] == "**")
                {
                    current = current.SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                        .Select(t => t.gameObject)
                        .ToList();
                }
                else
                {
                    var regex = WildcardToRegex(parts[i]);
                    current = current.SelectMany(GetTransformChildren)
                        .Where(t => regex.IsMatch(t.name))
                        .Select(t => t.gameObject)
                        .ToList();
                }
            }

            return current.Distinct();

            IEnumerable<Scene> GetLoadedScenes() {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    yield return SceneManager.GetSceneAt(i);
                }
            }

            IEnumerable<Transform> GetTransformChildren(GameObject go) {
                foreach (var child in go.transform) {
                    yield return (Transform)child;
                }
            }
        }

        private IEnumerable<UnityEngine.Object> FindAssets(string path)
        {
            if (System.IO.File.Exists(path))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                return obj != null ? new[] { obj } : Enumerable.Empty<UnityEngine.Object>();
            }

            var guids = AssetDatabase.FindAssets(path);
            return guids.Select(guid => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                AssetDatabase.GUIDToAssetPath(guid)));
        }

        private IEnumerable<UnityEngine.Object> QuickSearch(string query)
        {
            // Note: This is a simplified implementation
            // In a full implementation, you would integrate with Unity's Quick Search API
            return FindAssets(query);
        }

        private IEnumerable<UnityEngine.Object> FilterByComponent(IEnumerable<UnityEngine.Object> objects, string componentType)
        {
            var type = TypeCache.GetTypesDerivedFrom<Component>()
                .FirstOrDefault(t => t.Name.Equals(componentType, StringComparison.OrdinalIgnoreCase));

            if (type == null)
                return Enumerable.Empty<UnityEngine.Object>();

            return objects.OfType<GameObject>()
                .Select(go => go.GetComponent(type))
                .Where(c => c != null)
                .Cast<UnityEngine.Object>();
        }

        private IEnumerable<UnityEngine.Object> ApplyRangeSpecifier(IEnumerable<UnityEngine.Object> objects, string rangeSpec)
        {
            if (string.IsNullOrEmpty(rangeSpec))
                return objects;

            var list = objects.ToList();
            var ranges = rangeSpec.Split(',');
            var result = new HashSet<UnityEngine.Object>();

            foreach (var range in ranges)
            {
                var parts = range.Split(new[] { ".." }, StringSplitOptions.None);
                if (parts.Length == 1)
                {
                    if (int.TryParse(parts[0], out int index) && index >= 0 && index < list.Count)
                        result.Add(list[index]);
                }
                else if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                    {
                        start = Math.Max(0, start);
                        end = Math.Min(list.Count - 1, end);
                        for (int i = start; i <= end; i++)
                            result.Add(list[i]);
                    }
                }
            }

            return result;
        }

        private static Regex WildcardToRegex(string pattern)
        {
            return GetRegex("^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".")
                + "$");
        }

        private static readonly Dictionary<string, Regex> regexCache = new Dictionary<string, Regex>();

        private static Regex GetRegex(string pattern)
        {
            if (!regexCache.TryGetValue(pattern, out Regex regex))
            {
                regex = new Regex(pattern, RegexOptions.Compiled);
                regexCache[pattern] = regex;
            }

            return regex;
        }
    }
}
