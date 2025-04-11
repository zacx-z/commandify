using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Commandify
{
    public class RemoveCommandHandler : ICommandHandler
    {
        public async Task<string> ExecuteAsync(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                return "Usage: remove <selector>\nRemoves objects matching the selector from the scene or prefab.";

            string selector = args[0];
            var objects = (await context.ResolveObjectReference(selector)).ToList();

            if (!objects.Any())
                return $"No objects found matching selector: {selector}";

            int count = 0;
            foreach (var obj in objects)
            {
                if (obj is GameObject go)
                {
                    // Record the operation for undo
                    Undo.DestroyObjectImmediate(go);
                    count++;
                }
            }

            return $"Removed {count} object(s)";
        }
    }
}
