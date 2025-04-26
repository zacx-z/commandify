using System;
using System.Collections.Generic;
using UnityEngine;

namespace Commandify
{
    /// <summary>
    /// Manages command execution logs
    /// </summary>
    public class CommandLogger
    {
        private static CommandLogger instance;
        public static CommandLogger Instance => instance ??= new CommandLogger();

        /// <summary>
        /// Maximum number of log entries to keep
        /// </summary>
        private const int MAX_LOG_ENTRIES = 1000;

        /// <summary>
        /// List of all log entries
        /// </summary>
        private readonly List<CommandLogEntry> logEntries = new List<CommandLogEntry>();

        /// <summary>
        /// Stack of currently executing commands (for nested commands)
        /// </summary>
        private readonly Stack<CommandLogEntry> commandStack = new Stack<CommandLogEntry>();

        /// <summary>
        /// Event raised when a new log entry is added
        /// </summary>
        public event Action<CommandLogEntry> OnLogEntryAdded;

        /// <summary>
        /// Event raised when a log entry is updated
        /// </summary>
        public event Action<CommandLogEntry> OnLogEntryUpdated;

        /// <summary>
        /// Event raised when the log is cleared
        /// </summary>
        public event Action OnLogCleared;

        private CommandLogger()
        {
            // Private constructor to enforce singleton pattern
        }

        /// <summary>
        /// Gets all log entries
        /// </summary>
        /// <returns>All log entries</returns>
        public IReadOnlyList<CommandLogEntry> GetLogEntries()
        {
            return logEntries.AsReadOnly();
        }

        /// <summary>
        /// Gets only top-level log entries (not children of macros)
        /// </summary>
        /// <returns>Top-level log entries</returns>
        public IEnumerable<CommandLogEntry> GetTopLevelEntries()
        {
            return logEntries.FindAll(entry => entry.Parent == null);
        }

        /// <summary>
        /// Begins logging a command execution
        /// </summary>
        /// <param name="command">The command being executed</param>
        /// <param name="isMacro">Whether this is a macro command</param>
        /// <returns>The log entry for the command</returns>
        public CommandLogEntry BeginCommand(string command, bool isMacro = false)
        {
            CommandLogEntry parent = commandStack.Count > 0 ? commandStack.Peek() : null;
            CommandLogEntry entry = new CommandLogEntry(command, isMacro, parent);

            // If this is a top-level command, add it to the main log entries list
            if (parent == null)
            {
                logEntries.Add(entry);

                // Trim log if it gets too large
                if (logEntries.Count > MAX_LOG_ENTRIES)
                {
                    logEntries.RemoveAt(0);
                }
            }

            // Push this command onto the stack
            commandStack.Push(entry);

            // Notify listeners
            OnLogEntryAdded?.Invoke(entry);

            return entry;
        }

        /// <summary>
        /// Ends the current command execution and sets its output
        /// </summary>
        /// <param name="output">The command output</param>
        /// <param name="error">The command error output</param>
        public void EndCommand(string output = null, string error = null)
        {
            if (commandStack.Count == 0)
            {
                Debug.LogWarning("[CommandLogger] Attempted to end a command when no command was in progress");
                return;
            }

            CommandLogEntry entry = commandStack.Pop();

            if (!string.IsNullOrEmpty(output))
            {
                entry.SetOutput(output);
            }

            if (!string.IsNullOrEmpty(error))
            {
                entry.SetError(error);
            }

            // Notify listeners
            OnLogEntryUpdated?.Invoke(entry);
        }

        /// <summary>
        /// Clears all log entries
        /// </summary>
        public void Clear()
        {
            logEntries.Clear();
            commandStack.Clear();

            // Notify listeners
            OnLogCleared?.Invoke();
        }
    }
}
