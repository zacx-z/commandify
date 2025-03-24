using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Commandify
{
    public class PropertyCommandHandler : ICommandHandler
    {
        public string Execute(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No property subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "list":
                    return ListProperties(subArgs, context);
                case "get":
                    return GetProperty(subArgs, context);
                case "set":
                    return SetProperty(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown property subcommand: {subCommand}");
            }
        }

        private string ListProperties(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Object selector required");

            var objects = context.ResolveObjectReference(args[0]).ToList();
            if (!objects.Any())
                throw new ArgumentException("No objects found");

            var properties = new List<string>();
            foreach (var obj in objects)
            {
                var serializedObject = new SerializedObject(obj);
                var property = serializedObject.GetIterator();
                var depth = 0;

                if (property.Next(true))
                {
                    do
                    {
                        var indent = new string(' ', depth * 2);
                        properties.Add($"{indent}{property.propertyPath} ({property.propertyType})");
                        depth = property.depth;
                    }
                    while (property.Next(false));
                }
            }

            // Store the properties in the result variable
            context.SetLastResult(properties);
            return string.Join("\n", properties);
        }

        private string GetProperty(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Object selector and property path required");

            var objects = context.ResolveObjectReference(args[0]).ToList();
            if (!objects.Any())
                throw new ArgumentException("No objects found");

            string propertyPath = args[1];
            if (propertyPath.StartsWith("$"))
                propertyPath = context.ResolveStringReference(propertyPath);

            var values = new List<string>();
            foreach (var obj in objects)
            {
                var serializedObject = new SerializedObject(obj);
                var property = serializedObject.FindProperty(propertyPath);

                if (property == null)
                    throw new ArgumentException($"Property not found: {propertyPath}");

                values.Add(GetPropertyValue(property));
            }

            // Store the property values in the result variable
            context.SetLastResult(values);
            return string.Join("\n", values);
        }

        private string SetProperty(List<string> args, CommandContext context)
        {
            if (args.Count < 3)
                throw new ArgumentException("Object selector, property path, and value required");

            var objects = context.ResolveObjectReference(args[0]).ToList();
            if (!objects.Any())
                throw new ArgumentException("No objects found");

            string propertyPath = args[1];
            if (propertyPath.StartsWith("$"))
                propertyPath = context.ResolveStringReference(propertyPath);

            string value = args[2];
            if (value.StartsWith("$"))
                value = context.ResolveStringReference(value);

            int count = 0;
            foreach (var obj in objects)
            {
                var serializedObject = new SerializedObject(obj);
                var property = serializedObject.FindProperty(propertyPath);

                if (property == null)
                    throw new ArgumentException($"Property not found: {propertyPath}");

                Undo.RecordObject(obj, "Set Property");

                if (SetPropertyValue(property, value))
                {
                    serializedObject.ApplyModifiedProperties();
                    count++;
                }
            }

            // Store the modified objects in the result variable
            context.SetLastResult(objects);
            return $"Set property on {count} object(s)";
        }

        private string GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString();
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Color:
                    return ColorUtility.ToHtmlStringRGBA(property.colorValue);
                case SerializedPropertyType.Vector2:
                    return property.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return property.vector3Value.ToString();
                case SerializedPropertyType.Vector4:
                    return property.vector4Value.ToString();
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue.eulerAngles.ToString();
                case SerializedPropertyType.Enum:
                    return property.enumNames[property.enumValueIndex];
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue?.name ?? "null";
                default:
                    return $"[{property.propertyType}]";
            }
        }

        private bool SetPropertyValue(SerializedProperty property, string value)
        {
            try
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        property.intValue = int.Parse(value);
                        break;
                    case SerializedPropertyType.Boolean:
                        property.boolValue = bool.Parse(value);
                        break;
                    case SerializedPropertyType.Float:
                        property.floatValue = float.Parse(value);
                        break;
                    case SerializedPropertyType.String:
                        property.stringValue = value;
                        break;
                    case SerializedPropertyType.Color:
                        if (ColorUtility.TryParseHtmlString(value, out Color color))
                            property.colorValue = color;
                        break;
                    case SerializedPropertyType.Vector2:
                        var v2 = ParseVector(value, 2);
                        property.vector2Value = new Vector2(v2[0], v2[1]);
                        break;
                    case SerializedPropertyType.Vector3:
                        var v3 = ParseVector(value, 3);
                        property.vector3Value = new Vector3(v3[0], v3[1], v3[2]);
                        break;
                    case SerializedPropertyType.Vector4:
                        var v4 = ParseVector(value, 4);
                        property.vector4Value = new Vector4(v4[0], v4[1], v4[2], v4[3]);
                        break;
                    case SerializedPropertyType.Quaternion:
                        var euler = ParseVector(value, 3);
                        property.quaternionValue = Quaternion.Euler(euler[0], euler[1], euler[2]);
                        break;
                    case SerializedPropertyType.Enum:
                        int enumIndex = Array.IndexOf(property.enumNames, value);
                        if (enumIndex >= 0)
                            property.enumValueIndex = enumIndex;
                        break;
                    default:
                        return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to set property value: {ex.Message}");
            }
        }

        private float[] ParseVector(string value, int expectedComponents)
        {
            var components = value.Trim('(', ')', ' ')
                .Split(',')
                .Select(s => float.Parse(s.Trim()))
                .ToArray();

            if (components.Length != expectedComponents)
                throw new ArgumentException($"Expected {expectedComponents} components, got {components.Length}");

            return components;
        }
    }
}
