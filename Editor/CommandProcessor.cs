using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commandify
{
    public class CommandProcessor
    {
        private static CommandProcessor instance;
        public static CommandProcessor Instance => instance ??= new CommandProcessor();

        private Dictionary<string, ICommandHandler> handlers;
        private readonly StringBuilder outputBuffer = new();
        private readonly StringBuilder errorBuffer = new();
        private CommandContext context;

        private CommandProcessor()
        {
            InitializeHandlers();
            context = new CommandContext();
        }

        private void InitializeHandlers()
        {
            handlers = new Dictionary<string, ICommandHandler>
            {
                { "help", new HelpCommandHandler() },
                { "scene", new SceneCommandHandler() },
                { "create", new CreateCommandHandler() },
                { "asset", new AssetCommandHandler() },
                { "prefab", new PrefabCommandHandler() },
                { "list", new ListCommandHandler() },
                { "select", new SelectCommandHandler() },
                { "property", new PropertyCommandHandler() },
                { "component", new ComponentCommandHandler() },
                { "transform", new TransformCommandHandler() },
                { "set", new VariableCommandHandler() },
                { "package", new PackageCommandHandler() },
                { "exec", new ExecCommandHandler() },
                { "undo", new UndoRedoCommandHandler("undo") },
                { "redo", new UndoRedoCommandHandler("redo") },
                { "remove", new RemoveCommandHandler() }
            };
        }

        public void AppendOutput(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (var line in text.Split('\n'))
            {
                outputBuffer.AppendLine($"[OUT]{line}");
            }
        }

        public void AppendError(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (var line in text.Split('\n'))
            {
                errorBuffer.AppendLine($"[ERR]{line}");
            }
        }

        public async Task<string> ProcessCommandAsync(string commandLine)
        {
            outputBuffer.Clear();
            errorBuffer.Clear();

            if (string.IsNullOrWhiteSpace(commandLine))
            {
                AppendError("Error: Empty command");
                return errorBuffer.ToString();
            }

            try
            {
                string result = await ExecuteCommandAsync(commandLine);

                if (!string.IsNullOrEmpty(result))
                {
                    AppendOutput(result);
                }

                // Combine output and error buffers
                var combinedOutput = new StringBuilder();
                combinedOutput.Append(outputBuffer);
                combinedOutput.Append(errorBuffer);
                return combinedOutput.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                AppendError($"Error: {ex.Message}");
                return errorBuffer.ToString();
            }
        }

        private List<string> TokenizeCommand(string commandLine)
        {
            var tokens = new List<string>();
            var currentToken = new List<char>();
            bool inQuotes = false;
            bool escaped = false;
            bool inSubCommand = false;
            int subCommandDepth = 0;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                char? nextChar = i + 1 < commandLine.Length ? commandLine[i + 1] : null;

                if (escaped)
                {
                    currentToken.Add(c);
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"' && !inSubCommand)
                {
                    inQuotes = !inQuotes;
                }
                else if (c == '$' && nextChar == '(' && !inQuotes)
                {
                    if (currentToken.Count > 0)
                    {
                        tokens.Add(new string(currentToken.ToArray()));
                        currentToken.Clear();
                    }
                    currentToken.Add(c);
                    currentToken.Add('(');
                    inSubCommand = true;
                    subCommandDepth = 1;
                    i++; // Skip the next '('
                }
                else if (inSubCommand)
                {
                    currentToken.Add(c);
                    if (c == '(')
                        subCommandDepth++;
                    else if (c == ')')
                    {
                        subCommandDepth--;
                        if (subCommandDepth == 0)
                        {
                            inSubCommand = false;
                            tokens.Add(new string(currentToken.ToArray()));
                            currentToken.Clear();
                        }
                    }
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

        public async Task<string> ExecuteCommandAsync(string commandLine) {
            var tokens = TokenizeCommand(commandLine);
            if (tokens.Count == 0)
            {
                AppendError("Error: Invalid command format");
                return errorBuffer.ToString();
            }

            string command = tokens[0].ToLower();
            if (!handlers.TryGetValue(command, out var handler))
            {
                AppendError($"Error: Unknown command '{command}'");
                return errorBuffer.ToString();
            }

            var args = tokens.Skip(1).ToList();
            return await handler.ExecuteAsync(args, context);
        }
    }
}
