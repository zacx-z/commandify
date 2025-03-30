using UnityEngine;
using System.Text;

namespace Commandify
{
    public static class ObjectFormatter
    {
        public static string GetObjectPath(GameObject obj)
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
                return format == ListCommandHandler.OutputFormat.Path ? GetObjectPath(go) : go.name;
            }
            else
            {
                return obj.name;
            }
        }
    }
}
