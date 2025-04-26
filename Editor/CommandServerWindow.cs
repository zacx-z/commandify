using UnityEngine;
using UnityEditor;

namespace Commandify
{
    public class CommandServerWindow : EditorWindow
    {
        private CommandServer server => CommandServer.Instance;
        private string status => server.IsRunning ? "Running" : "Stopped";
        private int port;

        [MenuItem("Window/Commandify/Server Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<CommandServerWindow>();
            window.titleContent = new GUIContent("Commandify Server");
            window.Show();
        }

        private void OnEnable()
        {
            port = server.Port;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Server Status", status, EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(server.IsRunning))
            {
                int newPort = EditorGUILayout.IntField("Port", port);
                if (newPort != port && !server.IsRunning)
                {
                    port = newPort;
                    server.Port = port;
                }
            }

            EditorGUILayout.Space(20);
            if (!server.IsRunning)
            {
                if (GUILayout.Button("Start Server"))
                {
                    server.Start();
                }
            }
            else
            {
                if (GUILayout.Button("Stop Server"))
                {
                    server.Stop();
                }
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Open Command Log"))
            {
                CommandLogWindow.ShowWindow();
            }
        }

        private void OnDisable()
        {
            // Optionally stop the server when window is closed
            // server.Stop();
        }
    }
}
