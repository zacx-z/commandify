using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Commandify
{
    public class CommandProcessor
    {
        private static CommandProcessor instance;
        public static CommandProcessor Instance => instance ??= new CommandProcessor();

        private Dictionary<string, ICommandHandler> commandHandlers;
        private CommandContext context;

        private CommandProcessor()
        {
            InitializeHandlers();
            context = new CommandContext();
        }

        private void InitializeHandlers()
        {
            commandHandlers = new Dictionary<string, ICommandHandler>
            {
                { "scene", new SceneCommandHandler() },
                { "asset", new AssetCommandHandler() },
                { "prefab", new PrefabCommandHandler() },
                { "list", new ListCommandHandler() },
                { "select", new SelectCommandHandler() },
                { "property", new PropertyCommandHandler() },
                { "component", new ComponentCommandHandler() },
                { "transform", new TransformCommandHandler() },
                { "set", new VariableCommandHandler() },
                { "package", new PackageCommandHandler() },
                { "settings", new SettingsCommandHandler() }
            };
        }

        public string ProcessCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return "Error: Empty command";

            try
            {
                var tokens = TokenizeCommand(commandLine);
                if (tokens.Count == 0)
                    return "Error: Invalid command format";

                string command = tokens[0];
                if (!commandHandlers.TryGetValue(command, out var handler))
                    return $"Error: Unknown command '{command}'";

                var args = tokens.Skip(1).ToList();
                string result = handler.Execute(args, context);

                return result;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private List<string> TokenizeCommand(string commandLine)
        {
            var tokens = new List<string>();
            var currentToken = new List<char>();
            bool inQuotes = false;
            bool escaped = false;

            foreach (char c in commandLine)
            {
                if (escaped)
                {
                    currentToken.Add(c);
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (currentToken.Count > 0)
                    {
                        tokens.Add(new string(currentToken.ToArray()));
                        currentToken.Clear();
                    }
                }
                else
                {
                    currentToken.Add(c);
                }
            }

            if (currentToken.Count > 0)
            {
                tokens.Add(new string(currentToken.ToArray()));
            }

            return tokens;
        }
    }
}
