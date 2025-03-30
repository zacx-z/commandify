using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Commandify
{
    public class SelectCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            bool additive = false;
            bool selectChildren = false;
            List<UnityEngine.Object> objects = null;

            // Parse arguments
            for (int i = 0; i < args.Count; i++)
            {
                string arg = context.ResolveStringReference(args[i]);
                switch (arg)
                {
                    case "--add":
                        additive = true;
                        break;
                    case "--children":
                        selectChildren = true;
                        break;
                    default:
                        if (objects == null)
                            objects = context.ResolveObjectReference(args[i]).ToList();
                        break;
                }
            }

            if (objects == null)
                throw new ArgumentException("Selector required");

            // Include children if requested
            if (selectChildren)
            {
                var withChildren = new List<UnityEngine.Object>(objects);
                foreach (var obj in objects.OfType<GameObject>())
                {
                    withChildren.AddRange(obj.GetComponentsInChildren<Transform>()
                        .Select(t => t.gameObject));
                }
                objects = withChildren.Distinct().ToList();
            }

            // Convert to GameObjects where possible
            var gameObjects = objects.Select(obj =>
            {
                if (obj is GameObject go)
                    return go;
                if (obj is Component comp)
                    return comp.gameObject;
                return null;
            }).Where(go => go != null).ToList();

            // Update selection
            if (!additive)
                Selection.objects = null;

            if (gameObjects.Any())
            {
                if (additive)
                {
                    var currentSelection = Selection.objects.ToList();
                    currentSelection.AddRange(gameObjects);
                    Selection.objects = currentSelection.Distinct().ToArray();
                }
                else
                {
                    Selection.objects = gameObjects.ToArray();
                }
            }

            // Store selected objects in result variable
            context.SetLastResult(Selection.objects);

            return $"Selected {Selection.objects.Length} object(s)";
        }
    }
}
