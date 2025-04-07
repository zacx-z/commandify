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
                case "show":
                    return ShowTransformInfo(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown transform subcommand: {subCommand}");
            }
        }

        private string TranslateObjects(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            var objects = context.ResolveObjectReference(args[0]).OfType<GameObject>();

            context.SetLastResult(objects.ToArray());
            // If only selector provided, show current positions
            if (args.Count == 1)
            {
                var positionInfo = objects.Select(o => (o.name, o.transform.position));
                return string.Join("\n", positionInfo.Select(info => $"{info.name}: Position ({info.position.x}, {info.position.y}, {info.position.z})"));
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

            return $"Translated {count} object(s) by ({x}, {y}, {z})";
        }

        private string RotateObjects(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            var objects = context.ResolveObjectReference(args[0]).OfType<GameObject>();

            context.SetLastResult(objects.ToArray());

            // If only selector provided, show current rotations
            if (args.Count == 1)
            {
                IEnumerable<(string name, Vector3 rotation)> rotations = objects.Select(o => (o.name, o.transform.rotation.eulerAngles));
                return string.Join("\n", rotations.Select(info => $"{info.name}: Rotation ({info.rotation.x}, {info.rotation.y}, {info.rotation.z})"));
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

            return $"Rotated {count} object(s) by ({x}, {y}, {z}) degrees";
        }

        private string ScaleObjects(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            var objects = context.ResolveObjectReference(args[0]).OfType<GameObject>();

            context.SetLastResult(objects.ToArray());

            // If only selector provided, show current scales
            if (args.Count == 1)
            {
                IEnumerable<(string name, Vector3 scale)> scales = objects.Select(o => (o.name, o.transform.localScale));
                return string.Join("\n", scales.Select(info => $"{info.name}: Scale ({info.scale.x}, {info.scale.y}, {info.scale.z})"));
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

            context.SetLastResult(children.ToArray());
            return $"Parented {count} object(s) to {parent.name}";
        }

        private string ShowTransformInfo(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Selector required");

            var objects = context.ResolveObjectReference(args[0]).OfType<GameObject>();
            if (!objects.Any())
                return "No objects found";

            context.SetLastResult(objects.ToArray());
            var result = new List<string>();

            foreach (var obj in objects)
            {
                var transform = obj.transform;
                var parent = transform.parent;
                string parentInfo = parent != null ? 
                    $"Parent: {parent.gameObject.name} (Instance ID: {parent.gameObject.GetInstanceID()})" :
                    "Parent: none";

                result.Add($"GameObject: {obj.name} (Instance ID: {obj.GetInstanceID()})");
                result.Add($"Position: ({transform.position.x}, {transform.position.y}, {transform.position.z})");
                result.Add($"Rotation: ({transform.rotation.eulerAngles.x}, {transform.rotation.eulerAngles.y}, {transform.rotation.eulerAngles.z})");
                result.Add($"Scale: ({transform.localScale.x}, {transform.localScale.y}, {transform.localScale.z})");
                result.Add(parentInfo);
                result.Add(""); // Empty line between objects
            }

            return string.Join("\n", result.Take(result.Count - 1)); // Remove last empty line
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
