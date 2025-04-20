using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Commandify
{
    public class PropertyCommandHandler : ICommandHandler
    {
        public async Task<string> ExecuteAsync(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("No property subcommand specified");

            string subCommand = args[0];
            var subArgs = args.Skip(1).ToList();

            switch (subCommand.ToLower())
            {
                case "list":
                    return await ListProperties(subArgs, context);
                case "get":
                    return await GetProperty(subArgs, context);
                case "set":
                    return await SetProperty(subArgs, context);
                default:
                    throw new ArgumentException($"Unknown property subcommand: {subCommand}");
            }
        }

        private async Task<string> ListProperties(List<string> args, CommandContext context)
        {
            if (args.Count == 0)
                throw new ArgumentException("Object selector required");

            var objects = (await context.ResolveObjectReference(args[0])).ToList();
            if (!objects.Any())
                throw new ArgumentException($"No objects found matching selector: {args[0]}");

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

        private ObjectFormatter.OutputFormat format = ObjectFormatter.OutputFormat.Default;

        private async Task<string> GetProperty(List<string> args, CommandContext context)
        {
            if (args.Count < 2)
                throw new ArgumentException("Object selector and property path required");

            // Parse format option
            for (int i = 0; i < args.Count; i++)
            {
                string arg = context.ResolveStringReference(args[i]);
                if (arg == "--format" && ++i < args.Count)
                {
                    string formatStr = context.ResolveStringReference(args[i]).ToLower();
                    args.RemoveRange(i - 1, 2);
                    i -= 2;
                    
                    switch (formatStr)
                    {
                        case "instance-id":
                        case "instanceid":
                            format = ObjectFormatter.OutputFormat.InstanceId;
                            break;
                        case "path":
                            format = ObjectFormatter.OutputFormat.Path;
                            break;
                        default:
                            format = ObjectFormatter.OutputFormat.Default;
                            break;
                    }
                }
            }

            var objects = (await context.ResolveObjectReference(args[0])).ToList();
            if (!objects.Any())
                throw new ArgumentException($"No objects found matching selector: {args[0]}");

            string propertyPath = context.ResolveStringReference(args[1]);

            var values = new List<object>();
            Type valueType = null;
            foreach (var obj in objects)
            {
                var serializedObject = new SerializedObject(obj);
                var property = serializedObject.FindProperty(propertyPath);
                valueType = property.propertyType == SerializedPropertyType.ObjectReference
                    || property.propertyType == SerializedPropertyType.ManagedReference
                    || property.propertyType == SerializedPropertyType.ExposedReference
                    ? typeof(Object) : typeof(string);

                if (property == null)
                    throw new ArgumentException($"Property not found: {propertyPath}");

                var value = GetPropertyValue(property);
                values.Add(value);
            }

            // Store the property values in the result variable
            context.SetLastResult(valueType == typeof(string) ? values.Cast<string>().ToArray() : values.Cast<Object>().ToArray());
            
            // Format object references according to the specified format
            for (int i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value is Object unityObj)
                {
                    values[i] = ObjectFormatter.FormatObject(unityObj, format);
                }
            }

            return string.Join("\n", values);
        }

        private async Task<string> SetProperty(List<string> args, CommandContext context)
        {
            if (args.Count < 3)
                throw new ArgumentException("Object selector, property path, and value required");

            var objects = (await context.ResolveObjectReference(args[0])).ToList();
            if (!objects.Any())
                throw new ArgumentException($"No objects found matching selector: {args[0]}");

            string propertyPath = context.ResolveStringReference(args[1]);
            string value = context.ResolveStringReference(args[2]);

            int count = 0;
            foreach (var obj in objects)
            {
                var serializedObject = new SerializedObject(obj);
                var property = serializedObject.FindProperty(propertyPath);

                if (property == null)
                    throw new ArgumentException($"Property not found: {propertyPath}");

                Undo.RecordObject(obj, "Set Property");

                if (await SetPropertyValue(property, value, context))
                {
                    serializedObject.ApplyModifiedProperties();
                    count++;
                }
            }

            // Store the modified objects in the result variable
            context.SetLastResult(objects);
            return $"Set property on {count} object(s)";
        }

        private object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue;
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
                    return property.objectReferenceValue;
                default:
                    return $"[{property.propertyType}]";
            }
        }

        private async Task<bool> SetPropertyValue(SerializedProperty property, string value, CommandContext context)
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
                    case SerializedPropertyType.ObjectReference:
                        try
                        {
                            var resolvedObjects = (await context.ResolveObjectReference(value)).ToList();
                            if (!resolvedObjects.Any())
                                throw new ArgumentException($"No objects found for reference: {value}");
                            property.objectReferenceValue = resolvedObjects.First();
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException($"Failed to set object reference: {ex.Message}");
                        }
                        break;
                    case SerializedPropertyType.Generic:
                        if (property.isArray)
                        {
                            // Remove brackets and split by commas
                            var arrayStr = value.Trim('[', ']');
                            var elements = arrayStr.Split(',').Select(e => e.Trim()).ToList();
                            
                            property.arraySize = elements.Count;
                            for (int i = 0; i < elements.Count; i++)
                            {
                                var element = property.GetArrayElementAtIndex(i);
                                SetPropertyValue(element, elements[i], context);
                            }
                            break;
                        }
                        throw new NotSupportedException($"Unsupported generic property type");
                    default:
                        throw new NotSupportedException($"Unsupported property type: {property.propertyType}");
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
            return VectorUtility.ParseVector(value, expectedComponents);
        }
    }
}
