using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEditor.Search;

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
            var results = new List<UnityEngine.Object>();
            var selectors = selectorString.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var selector in selectors)
            {
                var baseSelector = ParseBaseSelector(selector.Trim(), out var rangeSpecifier);
                var objects = EvaluateBaseSelector(baseSelector);
                results.AddRange(ApplyRangeSpecifier(objects, rangeSpecifier));
            }

            return results.Distinct();
        }

        private string ParseBaseSelector(string input, out string rangeSpecifier)
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

        private IEnumerable<UnityEngine.Object> EvaluateBaseSelector(string selector)
        {
            if (string.IsNullOrEmpty(selector))
                return Enumerable.Empty<UnityEngine.Object>();

            var lexer = new Lexer(selector);
            return ParseExpression(lexer);
        }

        private enum TokenType
        {
            Variable,        // $varname
            InstanceId,      // @&id
            Tag,            // @#tag
            QuickSearch,    // @@query
            HierarchyPath,  // ^path
            ComponentType,  // :type
            Union,         // |
            Intersection,  // &
            Text,         // any other text
            EOF           // end of input
        }

        private class Token
        {
            public TokenType Type { get; }
            public string Value { get; }

            public Token(TokenType type, string value)
            {
                Type = type;
                Value = value;
            }
        }

        private class Lexer
        {
            private readonly string input;
            private int position;
            private Token? currentToken;

            public Lexer(string input)
            {
                this.input = input;
                this.position = 0;
                this.currentToken = null;
            }

            public Token Peek()
            {
                if (currentToken == null)
                    currentToken = ReadNextToken();
                return currentToken;
            }

            public Token Consume()
            {
                var token = Peek();
                currentToken = null;
                return token;
            }

            private Token ReadNextToken()
            {
                SkipWhitespace();

                if (position >= input.Length)
                    return new Token(TokenType.EOF, "");

                char current = input[position];

                // Handle special prefixes
                if (current == '$')
                {
                    position++;
                    return new Token(TokenType.Variable, ReadIdentifier());
                }

                if (current == '@')
                {
                    position++;
                    if (position < input.Length)
                    {
                        char next = input[position];
                        if (next == '&')
                        {
                            position++;
                            return new Token(TokenType.InstanceId, ReadIdentifier());
                        }
                        if (next == '#')
                        {
                            position++;
                            return new Token(TokenType.Tag, ReadIdentifier());
                        }
                        if (next == '@')
                        {
                            position++;
                            return new Token(TokenType.QuickSearch, ReadUntilDelimiter());
                        }
                    }
                    // Invalid @ prefix, treat as text
                    position--;
                    return new Token(TokenType.Text, ReadUntilDelimiter());
                }

                if (current == '^')
                {
                    position++;
                    return new Token(TokenType.HierarchyPath, ReadUntilDelimiter());
                }

                if (current == ':')
                {
                    position++;
                    return new Token(TokenType.ComponentType, ReadIdentifier());
                }

                if (current == '|')
                {
                    position++;
                    return new Token(TokenType.Union, "|");
                }

                if (current == '&')
                {
                    position++;
                    return new Token(TokenType.Intersection, "&");
                }

                // Handle regular text
                return new Token(TokenType.Text, ReadUntilDelimiter());
            }

            private void SkipWhitespace()
            {
                while (position < input.Length && char.IsWhiteSpace(input[position]))
                    position++;
            }

            private string ReadIdentifier()
            {
                var start = position;
                while (position < input.Length && (char.IsLetterOrDigit(input[position]) || input[position] == '_'))
                    position++;
                return input.Substring(start, position - start);
            }

            private string ReadUntilDelimiter()
            {
                var start = position;
                while (position < input.Length && !IsDelimiter(input[position]) && !char.IsWhiteSpace(input[position]))
                    position++;
                return input.Substring(start, position - start);
            }

            private bool IsDelimiter(char c)
            {
                return c == ':' || c == '|' || c == '&';
            }
        }

        private IEnumerable<UnityEngine.Object> ParseExpression(Lexer lexer)
        {
            var result = ParseTerm(lexer);

            while (lexer.Peek().Type == TokenType.Union)
            {
                lexer.Consume(); // consume the union operator
                var right = ParseTerm(lexer);
                result = result.Union(right);
            }

            return result;
        }

        private IEnumerable<UnityEngine.Object> ParseTerm(Lexer lexer)
        {
            var result = ParseFactor(lexer);

            while (lexer.Peek().Type == TokenType.Intersection)
            {
                lexer.Consume(); // consume the intersection operator
                var right = ParseFactor(lexer);
                result = result.Intersect(right);
            }

            return result;
        }

        private IEnumerable<UnityEngine.Object> ParseFactor(Lexer lexer)
        {
            var token = lexer.Consume();
            IEnumerable<UnityEngine.Object> result;

            switch (token.Type)
            {
                case TokenType.Variable:
                    if (variables.TryGetValue(token.Value, out object value))
                    {
                        if (value is IEnumerable<UnityEngine.Object> objects)
                            result = objects;
                        else if (value is UnityEngine.Object obj)
                            result = new[] { obj };
                        else
                            result = Enumerable.Empty<UnityEngine.Object>();
                    }
                    else
                        result = Enumerable.Empty<UnityEngine.Object>();
                    break;

                case TokenType.InstanceId:
                    if (int.TryParse(token.Value, out int instanceId))
                    {
                        var obj = EditorUtility.InstanceIDToObject(instanceId);
                        result = obj != null ? new[] { obj } : Enumerable.Empty<UnityEngine.Object>();
                    }
                    else
                        result = Enumerable.Empty<UnityEngine.Object>();
                    break;

                case TokenType.Tag:
                    result = GameObject.FindGameObjectsWithTag(token.Value).Cast<UnityEngine.Object>();
                    break;

                case TokenType.QuickSearch:
                    result = QuickSearch(token.Value).Select(item => item.ToObject()).Where(obj => obj != null);
                    break;

                case TokenType.HierarchyPath:
                    result = FindInHierarchy(token.Value);
                    break;

                case TokenType.Text:
                    result = FindAssets(token.Value);
                    break;

                default:
                    result = Enumerable.Empty<UnityEngine.Object>();
                    break;
            }

            // Handle component type filters
            while (lexer.Peek().Type == TokenType.ComponentType)
            {
                var componentToken = lexer.Consume();
                result = SelectComponent(result, componentToken.Value);
            }

            return result;
        }

        private IEnumerable<UnityEngine.Object> FindInHierarchy(string path)
        {
            var parts = path.Split('/');
            var current = new List<GameObject>();

            // Start with root objects
            if (parts[0] == "**")
            {
                var matches = GetLoadedScenes().SelectMany(scene => scene.GetRootGameObjects())
                    .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                    .Select((t => t.gameObject));
                current.AddRange(matches);
            }
            else
            {
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

            IEnumerable<Scene> GetLoadedScenes()
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    yield return SceneManager.GetSceneAt(i);
                }
            }

            IEnumerable<Transform> GetTransformChildren(GameObject go)
            {
                foreach (var child in go.transform)
                {
                    yield return (Transform)child;
                }
            }
        }

        private IEnumerable<UnityEngine.Object> FindAssets(string path)
        {
            string fullPath = path.StartsWith("Assets/") ? path : "Assets/" + path;

            if (!fullPath.Contains("*") && !fullPath.Contains("?"))
            {
                if (System.IO.File.Exists(fullPath))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath);
                    return obj != null ? new[] { obj } : Enumerable.Empty<UnityEngine.Object>();
                }
                return Enumerable.Empty<UnityEngine.Object>();
            }

            var files = SearchFiles(fullPath);
            return files.Select(f => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(f))
                .Where(obj => obj != null);
        }

        private IEnumerable<string> SearchFiles(string pattern)
        {
            pattern = pattern.Replace('\\', '/') ?? "";

            var parts = pattern.Split("/");

            var current = new List<string>() { "." };
            bool parentDoubleStars = false;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (i < parts.Length - 1)
                {
                    if (part == "**")
                    {
                        parentDoubleStars = true;
                    }
                    else
                    {
                        current = current
                            .SelectMany(basePath => Directory.GetDirectories(basePath, part,
                                parentDoubleStars ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                            .ToList();
                        parentDoubleStars = false;
                    }
                }
                else
                {
                    if (part == "**")
                    {
                        current = current
                            .SelectMany(basePath => Directory.GetFiles(basePath, "*", SearchOption.AllDirectories))
                            .ToList();
                    }
                    else
                    {
                        current = current
                            .SelectMany(basePath => Directory.GetFiles(basePath, part,
                                parentDoubleStars ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                            .ToList();
                    }
                }
            }

            current = current.Select(path => path.Substring(2)).ToList();

            return current;
        }

        private List<SearchItem> QuickSearch(string query)
        {
            var providers = new[] {
                "asset",
                "scene",
                "find",
                "menu",
                "packages",
                "log",
            };

            var context = SearchService.CreateContext(providers);
            context.searchText = query;
            context.wantsMore = true;

            var items = SearchService.GetItems(context);
            return items;
        }

        private IEnumerable<UnityEngine.Object> SelectComponent(IEnumerable<UnityEngine.Object> objects, string componentType)
        {
            var type = TypeCache.GetTypesDerivedFrom<Component>()
                .FirstOrDefault(t => t.Name.Equals(componentType, StringComparison.OrdinalIgnoreCase));

            if (type == null)
                return Enumerable.Empty<UnityEngine.Object>();

            return objects.OfType<GameObject>()
                .Select(go => go.GetComponent(type))
                .Where(c => c != null);
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
