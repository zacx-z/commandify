using System;
using System.Collections.Generic;

namespace Commandify
{
    /// <summary>
    /// Represents a log entry for a command execution
    /// </summary>
    public class CommandLogEntry
    {
        /// <summary>
        /// The command that was executed
        /// </summary>
        public string Command { get; }
        
        /// <summary>
        /// The output of the command
        /// </summary>
        public string Output { get; private set; }
        
        /// <summary>
        /// The error output of the command
        /// </summary>
        public string Error { get; private set; }
        
        /// <summary>
        /// The timestamp when the command was executed
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Child commands (for macros)
        /// </summary>
        public List<CommandLogEntry> Children { get; }
        
        /// <summary>
        /// Parent command (null for top-level commands)
        /// </summary>
        public CommandLogEntry Parent { get; }
        
        /// <summary>
        /// Whether this command is a macro
        /// </summary>
        public bool IsMacro { get; }

        /// <summary>
        /// Creates a new command log entry
        /// </summary>
        /// <param name="command">The command that was executed</param>
        /// <param name="isMacro">Whether this command is a macro</param>
        /// <param name="parent">Parent command (null for top-level commands)</param>
        public CommandLogEntry(string command, bool isMacro = false, CommandLogEntry parent = null)
        {
            Command = command;
            Timestamp = DateTime.Now;
            Children = new List<CommandLogEntry>();
            Parent = parent;
            IsMacro = isMacro;
            
            // Add this entry as a child of the parent if it exists
            parent?.Children.Add(this);
        }
        
        /// <summary>
        /// Sets the output of the command
        /// </summary>
        /// <param name="output">The command output</param>
        public void SetOutput(string output)
        {
            Output = output;
        }
        
        /// <summary>
        /// Sets the error output of the command
        /// </summary>
        /// <param name="error">The error output</param>
        public void SetError(string error)
        {
            Error = error;
        }
    }
}
