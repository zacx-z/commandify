using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;

namespace Commandify
{
    public class MacroCommandHandler : ICommandHandler
    {
        public const string macrosDirectory = "Packages/com.nelasystem.commandify/Macros";
        private static readonly Regex argPattern = new Regex(@"(?:([a-zA-Z0-9_]+)=)?(.+)", RegexOptions.Compiled);

        public async Task<string> ExecuteAsync(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                return "Error: Please specify a macro name";

            if (args[0] == "--help")
                return "Usage:\n  <macro_name> [[<name>=]<arg> ...]\n\nExecutes a macro from the macros directory with the given arguments.\nArguments can be positional ($1, $2, etc.) or named ($name).\n\nExample:\n  create-cube pos=(1,0,1) size=(1.8,0.5,2)";

            if (args[0] == "--list")
                return ListMacros();

            string macroName = args[0];
            string macroPath = GetMacroPath(macroName);

            if (!File.Exists(macroPath))
                return $"Error: Macro '{macroName}' not found";

            // Parse and assign arguments
            var macroArgs = args.Skip(1).ToList();
            AssignMacroArguments(macroArgs, context);

            // Read and execute the macro file
            string[] commands = File.ReadAllLines(macroPath);
            string result = "";

            foreach (string command in commands)
            {
                if (string.IsNullOrWhiteSpace(command))
                    continue;

                result = await CommandProcessor.Instance.ExecuteCommandAsync(command);
            }

            return result;
        }

        private void AssignMacroArguments(List<string> args, CommandContext context)
        {
            // Clear any existing numbered arguments
            for (int i = 1; i <= 9; i++)
            {
                if (context.HasVariable($"${i}"))
                {
                    context.SetVariable($"${i}", null);
                }
            }

            // Assign new arguments
            for (int i = 0; i < args.Count && i < 9; i++)
            {
                string arg = args[i];
                Match match = argPattern.Match(arg);
                
                if (match.Success)
                {
                    string name = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    
                    // Always set the positional argument
                    context.SetVariable($"${i+1}", value);
                    
                    // If a name was provided, also set the named variable
                    if (!string.IsNullOrEmpty(name))
                    {
                        context.SetVariable($"${name}", value);
                    }
                }
            }
        }

        public static string GetMacroPath(string macroName)
        {
            // If the macro name already has .macro extension, use it as is
            if (macroName.EndsWith(".macro"))
                return Path.Combine(macrosDirectory, macroName).Replace("\\", "/");
            
            // Otherwise, add the .macro extension
            return Path.Combine(macrosDirectory, $"{macroName}.macro").Replace("\\", "/");
        }

        private string ListMacros()
        {
            if (!Directory.Exists(macrosDirectory))
                return "No macros directory found";

            string[] macroFiles = Directory.GetFiles(macrosDirectory, "*.macro");
            
            if (macroFiles.Length == 0)
                return "No macros found";

            var result = "Available macros:\n";
            foreach (var macroFile in macroFiles)
            {
                string macroName = Path.GetFileNameWithoutExtension(macroFile);
                result += $"  {macroName}\n";
            }
            
            return result;
        }
        
        public static string GetMacroHelp(string macroPath)
        {
            if (!File.Exists(macroPath))
                return string.Empty;
                
            string[] lines = File.ReadAllLines(macroPath);
            List<string> commentLines = new List<string>();
            
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("#"))
                {
                    // Remove the # and any single space after it
                    string commentText = trimmedLine.StartsWith("# ") ? 
                        trimmedLine.Substring(2) : 
                        trimmedLine.Substring(1);
                        
                    commentLines.Add(commentText);
                }
                else if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    // Stop when we hit a non-comment, non-empty line
                    break;
                }
            }
            
            if (commentLines.Count == 0)
                return string.Empty;
                
            return string.Join("\n", commentLines);
        }
    }
}
