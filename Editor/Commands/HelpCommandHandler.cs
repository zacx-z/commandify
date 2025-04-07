using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Commandify
{
    public class HelpCommandHandler : ICommandHandler {
        private const string DocumentationPath = "Packages/com.nelasystem.commandify/Documentation";

        public async Task<string> ExecuteAsync(List<string> args, CommandContext context)
        {
            // If a specific command is provided, show detailed help for that command
            if (args.Count > 0)
            {
                string commandName = args[0].ToLower();
                var helpDoc = GetDetailedHelp(commandName);
                if (!string.IsNullOrEmpty(helpDoc))
                    return helpDoc;
                throw new ArgumentException($"No detailed help available for '{commandName}'\nTry 'help' for a list of commands");
            }

            // Otherwise show general help with all available commands
            return GetGeneralHelp();
        }

        private string GetGeneralHelp()
        {
            var helpFiles = AssetDatabase.FindAssets("t:TextAsset", new[] { DocumentationPath })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => path.EndsWith(".md"));

            var help = new List<string> { "Available Commands:" };

            foreach (var filePath in helpFiles)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                var firstLine = textAsset.text.Split('\n').FirstOrDefault();

                if (!string.IsNullOrEmpty(firstLine))
                {
                    help.Add($"  {fileName}: {firstLine.TrimStart('#', ' ')}");
                }
            }

            help.Add("");
            help.Add("Use 'help <command>' for detailed help on a specific command");
            help.Add("Type 'exit' to quit");

            return string.Join("\n", help);
        }

        private string GetDetailedHelp(string command)
        {
            // First check for a documentation file using AssetDatabase
            string docPath = $"{DocumentationPath}/{command}.md";

            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(docPath);
            if (textAsset != null)
            {
                return textAsset.text;
            }

            return string.Empty;
        }
    }
}
