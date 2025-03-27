using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Commandify
{
    public class TransformCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No transform subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "translate":
                    return TranslateObjects(subArgs, context);
                case "rotate":
                    return RotateObjects(subArgs, context);
                case "scale":
                    return ScaleObjects(subArgs, context);
                case "parent":
                    return ParentObjects(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown transform subcommand: {subCommand}");
            }
        }

        private string TranslateObjects(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            var objects = context.ResolveObjectReference(args[0]).OfType<GameObject>();

            // If only selector provided, show current positions
            if (args.Count == 1)
            {
                var positions = objects.Select(o => o.transform.position);
                context.SetLastResult(objects);
                return string.Join("\n", positions.Select((pos, i) => $"Object {i + 1}: Position ({pos.x}, {pos.y}, {pos.z})"));
            }

            if (args.Count < 4)
                throw new ArgumentException("Three float values (x, y, z) required");

            // Handle potential variable references for coordinates
            float x = ParseCoordinate(args[1], context);
            float y = ParseCoordinate(args[2], context);
            float z = ParseCoordinate(args[3], context);

            Vector3 translation = new Vector3(x, y, z);
            int count = 0;

            var transforms = objects.Select(o => o.transform).ToArray();
            Undo.RecordObjects(transforms, "Transform Translate");
            
            foreach (var transform in transforms)
            {
                transform.position += translation;
                count++;
            }

            // Store the transformed objects in the result variable
            context.SetLastResult(transforms.Select(t => t.gameObject));
            return $"Translated {count} object(s) by ({x}, {y}, {z})";
        }

        private string RotateObjects(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            var objects = context.ResolveObjectReference(args[0]).OfType<GameObject>();

            // If only selector provided, show current rotations
            if (args.Count == 1)
            {
                var rotations = objects.Select(o => o.transform.rotation.eulerAngles);
                context.SetLastResult(objects);
                return string.Join("\n", rotations.Select((rot, i) => $"Object {i + 1}: Rotation ({rot.x}, {rot.y}, {rot.z})"));
            }

            if (args.Count < 4)
                throw new ArgumentException("Three float values (x, y, z) required");

            float x = ParseCoordinate(args[1], context);
            float y = ParseCoordinate(args[2], context);
            float z = ParseCoordinate(args[3], context);

            Vector3 rotation = new Vector3(x, y, z);
            int count = 0;

            var transforms = objects.Select(o => o.transform).ToArray();
            Undo.RecordObjects(transforms, "Transform Rotate");

            foreach (var transform in transforms)
            {
                transform.Rotate(rotation, Space.Self);
                count++;
            }

            context.SetLastResult(transforms.Select(t => t.gameObject));
            return $"Rotated {count} object(s) by ({x}, {y}, {z}) degrees";
        }

        private string ScaleObjects(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            var objects = context.ResolveObjectReference(args[0]).OfType<GameObject>();

            // If only selector provided, show current scales
            if (args.Count == 1)
            {
                var scales = objects.Select(o => o.transform.localScale);
                context.SetLastResult(objects);
                return string.Join("\n", scales.Select((scale, i) => $"Object {i + 1}: Scale ({scale.x}, {scale.y}, {scale.z})"));
            }

            if (args.Count < 4)
                throw new ArgumentException("Three float values (x, y, z) required");

            float x = ParseCoordinate(args[1], context);
            float y = ParseCoordinate(args[2], context);
            float z = ParseCoordinate(args[3], context);

            Vector3 scale = new Vector3(x, y, z);
            int count = 0;

            var transforms = objects.Select(o => o.transform).ToArray();
            Undo.RecordObjects(transforms, "Transform Scale");

            foreach (var transform in transforms)
            {
                transform.localScale = Vector3.Scale(transform.localScale, scale);
                count++;
            }

            context.SetLastResult(transforms.Select(t => t.gameObject));
            return $"Scaled {count} object(s) by ({x}, {y}, {z})";
        }

        private string ParentObjects(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Child selector and parent selector required");

            var children = context.ResolveObjectReference(args[0]).OfType<GameObject>();
            var parents = context.ResolveObjectReference(args[1]).OfType<GameObject>();

            if (!parents.Any())
                throw new ArgumentException("No parent objects found");
            if (parents.Count() > 1)
                throw new ArgumentException("Multiple parent objects found, expected one");

            var parent = parents.First();
            int count = 0;

            var transforms = children.Select(o => o.transform).ToArray();
            Undo.RecordObjects(transforms, "Transform Parent");

            foreach (var transform in transforms)
            {
                transform.SetParent(parent.transform, true);
                count++;
            }

            context.SetLastResult(transforms.Select(t => t.gameObject));
            return $"Parented {count} object(s) to {parent.name}";
        }

        private float ParseCoordinate(string value, CommandContext context)
        {
            // If it's a variable reference, resolve it
            if (value.StartsWith("$"))
            {
                var resolvedValue = context.ResolveStringReference(value);
                if (!float.TryParse(resolvedValue, out float result))
                    throw new ArgumentException($"Variable {value} does not contain a valid number");
                return result;
            }

            // Otherwise parse directly
            if (!float.TryParse(value, out float coordinate))
                throw new ArgumentException($"Invalid coordinate value: {value}");
            return coordinate;
        }
    }
}
