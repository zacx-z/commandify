using System;
using System.Threading.Tasks;
using UnityEditor;

namespace Commandify
{
    public static class MainThreadUtility
    {
        public static Task<T> ExecuteOnMainThread<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            bool executed = false;

            EditorApplication.update += OnUpdate;

            void OnUpdate()
            {
                if (executed) return;
                executed = true;
                EditorApplication.update -= OnUpdate;

                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            return tcs.Task;
        }

        public static Task Delay(int milliseconds)
        {
            var startTime = EditorApplication.timeSinceStartup;
            var endTime = startTime + milliseconds / 1000.0f;
            var tcs = new TaskCompletionSource<object>();

            EditorApplication.update += OnUpdate;

            void OnUpdate()
            {
                if (EditorApplication.timeSinceStartup > endTime)
                {
                    EditorApplication.update -= OnUpdate;
                    try
                    {
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }
            }

            return tcs.Task;
        }
    }
}
