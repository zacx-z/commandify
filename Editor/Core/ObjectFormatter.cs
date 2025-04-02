using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;

namespace Commandify
{
    public static class ObjectFormatter
    {
        public enum OutputFormat
        {
            Default,
            InstanceId,
            Path
        }

        public static string GetObjectHierarchyPath(GameObject obj)
        {
            var path = new StringBuilder(obj.name);
            var current = obj.transform.parent;
            while (current != null)
            {
                path.Insert(0, current.name + "/");
                current = current.parent;
            }
            return path.ToString();
        }

        public static string FormatObject(Object obj, OutputFormat format)
        {
            if (obj == null)
                return "null";

            if (format == OutputFormat.InstanceId)
            {
                return $"@&{obj.GetInstanceID()}";
            }
            else if (obj is GameObject go)
            {
                if (format == OutputFormat.Path)
                {
                    string assetPath = AssetDatabase.GetAssetPath(go);
                    if (!string.IsNullOrEmpty(assetPath))
                        return assetPath;
                    return "^" + GetObjectHierarchyPath(go);
                }
                return $"{go.name} (GameObject)";
            }
            else
            {
                if (format == OutputFormat.Path)
                {
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    return string.IsNullOrEmpty(assetPath) ? obj.name : assetPath;
                }
                return $"{obj.name} ({obj.GetType().Name})";
            }
        }
    }
}
