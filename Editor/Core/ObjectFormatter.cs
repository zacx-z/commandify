using UnityEngine;
using UnityEditor;
using System.Text;

namespace Commandify
{
    public static class ObjectFormatter
    {
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

        public static string FormatObject(Object obj, ListCommandHandler.OutputFormat format)
        {
            if (obj == null)
                return "null";

            if (format == ListCommandHandler.OutputFormat.InstanceId)
            {
                return $"@&{obj.GetInstanceID()}";
            }
            else if (obj is GameObject go)
            {
                if (format == ListCommandHandler.OutputFormat.Path)
                {
                    string assetPath = AssetDatabase.GetAssetPath(go);
                    if (!string.IsNullOrEmpty(assetPath))
                        return assetPath;
                    return "^" + GetObjectHierarchyPath(go);
                }
                return go.name;
            }
            else
            {
                if (format == ListCommandHandler.OutputFormat.Path)
                {
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    return string.IsNullOrEmpty(assetPath) ? obj.name : assetPath;
                }
                return obj.name;
            }
        }
    }
}
