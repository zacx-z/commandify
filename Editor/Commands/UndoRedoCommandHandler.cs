using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Commandify
{
    public class UndoRedoCommandHandler : ICommandHandler
    {
        private readonly string command;

        public UndoRedoCommandHandler(string command)
        {
            this.command = command;
        }

        public async Task<string> ExecuteAsync(List<string> args, CommandContext context)
        {
            if (args.Count > 0 && args[0] == "--help")
                return "Usage:\n  undo     Undo last operation\n  redo     Redo last undone operation";

            switch (command)
            {
                case "undo":
                    Undo.PerformUndo();
                    return "Performed undo operation";

                case "redo":
                    Undo.PerformRedo();
                    return "Performed redo operation";

                default:
                    throw new ArgumentException($"Invalid command handler configuration for '{command}'");
            }
        }
    }
}
