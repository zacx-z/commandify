using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Commandify
{
    public class CommandLogWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Dictionary<CommandLogEntry, bool> foldoutStates = new Dictionary<CommandLogEntry, bool>();
        private Dictionary<CommandLogEntry, bool> outputFoldoutStates = new Dictionary<CommandLogEntry, bool>();
        private GUIStyle commandStyle;
        private GUIStyle macroStyle;
        private GUIStyle outputStyle;
        private GUIStyle errorStyle;
        private GUIContent clearIcon;
        private GUIContent outputIcon;
        private GUIContent errorIcon;

        [MenuItem("Window/Commandify/Command Log")]
        public static void ShowWindow()
        {
            var window = GetWindow<CommandLogWindow>();
            window.titleContent = new GUIContent("Command Log");
            window.Show();
        }

        private void OnEnable()
        {
            // Subscribe to log events
            CommandLogger.Instance.OnLogEntryAdded += OnLogEntryAdded;
            CommandLogger.Instance.OnLogEntryUpdated += OnLogEntryUpdated;
            CommandLogger.Instance.OnLogCleared += OnLogCleared;

            // Initialize styles
            InitializeStyles();
        }

        private void OnDisable()
        {
            // Unsubscribe from log events
            CommandLogger.Instance.OnLogEntryAdded -= OnLogEntryAdded;
            CommandLogger.Instance.OnLogEntryUpdated -= OnLogEntryUpdated;
            CommandLogger.Instance.OnLogCleared -= OnLogCleared;
        }

        private void InitializeStyles()
        {
            commandStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true
            };

            macroStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                richText = true,
                wordWrap = true
            };

            outputStyle = new GUIStyle(EditorStyles.textArea)
            {
                richText = true,
                wordWrap = true,
                normal = { textColor = new Color(0.0f, 0.6f, 0.0f) }
            };

            errorStyle = new GUIStyle(EditorStyles.textArea)
            {
                richText = true,
                wordWrap = true,
                normal = { textColor = new Color(0.8f, 0.0f, 0.0f) }
            };

            // Load icons
            clearIcon = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
            outputIcon = EditorGUIUtility.IconContent("console.infoicon.sml");
            errorIcon = EditorGUIUtility.IconContent("console.erroricon.sml");
        }

        private void OnGUI()
        {
            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Get top-level entries
            var topLevelEntries = CommandLogger.Instance.GetTopLevelEntries().ToList();

            // Display entries in reverse chronological order (newest first)
            for (int i = topLevelEntries.Count - 1; i >= 0; i--)
            {
                DrawLogEntry(topLevelEntries[i], 0);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(clearIcon, EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Clear Command Log", 
                    "Are you sure you want to clear the command log?", "Yes", "No"))
                {
                    CommandLogger.Instance.Clear();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLogEntry(CommandLogEntry entry, int indentLevel)
        {
            // Ensure we have a foldout state for this entry
            if (!foldoutStates.ContainsKey(entry))
            {
                foldoutStates[entry] = true;
            }

            if (!outputFoldoutStates.ContainsKey(entry))
            {
                outputFoldoutStates[entry] = false;
            }

            EditorGUILayout.BeginHorizontal();

            // Indent based on level
            GUILayout.Space(indentLevel * 20);

            // Display foldout if this is a macro or has children
            if (entry.IsMacro || entry.Children.Count > 0)
            {
                foldoutStates[entry] = EditorGUILayout.Foldout(foldoutStates[entry], "", true);

                // Display command with timestamp
                string timestamp = entry.Timestamp.ToString("HH:mm:ss");
                EditorGUILayout.LabelField($"[{timestamp}] {entry.Command}", entry.IsMacro ? macroStyle : commandStyle);
            }
            else
            {
                // For regular commands, just display the command
                string timestamp = entry.Timestamp.ToString("HH:mm:ss");
                GUILayout.Space(15); // Space for alignment with foldouts
                EditorGUILayout.LabelField($"[{timestamp}] {entry.Command}", commandStyle);
            }

            EditorGUILayout.EndHorizontal();

            // Display output/error indicators if they exist
            if (!string.IsNullOrEmpty(entry.Output) || !string.IsNullOrEmpty(entry.Error))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space((indentLevel * 20) + 15);

                // Use a foldout for output/error
                outputFoldoutStates[entry] = EditorGUILayout.Foldout(outputFoldoutStates[entry], "Output/Error", true);

                // Show indicators
                if (!string.IsNullOrEmpty(entry.Output))
                {
                    GUILayout.Label(outputIcon, GUILayout.Width(16));
                }

                if (!string.IsNullOrEmpty(entry.Error))
                {
                    GUILayout.Label(errorIcon, GUILayout.Width(16));
                }

                EditorGUILayout.EndHorizontal();

                // Show output and error if foldout is open
                if (outputFoldoutStates[entry])
                {
                    if (!string.IsNullOrEmpty(entry.Output))
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space((indentLevel * 20) + 30);
                        EditorGUILayout.LabelField("Output:", EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space((indentLevel * 20) + 30);
                        EditorGUILayout.TextArea(entry.Output, outputStyle);
                        EditorGUILayout.EndHorizontal();
                    }

                    if (!string.IsNullOrEmpty(entry.Error))
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space((indentLevel * 20) + 30);
                        EditorGUILayout.LabelField("Error:", EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space((indentLevel * 20) + 30);
                        EditorGUILayout.TextArea(entry.Error, errorStyle);
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            // Draw children if foldout is open
            if (foldoutStates[entry] && entry.Children.Count > 0)
            {
                foreach (var child in entry.Children)
                {
                    DrawLogEntry(child, indentLevel + 1);
                }
            }
        }

        private void OnLogEntryAdded(CommandLogEntry entry)
        {
            // Force a repaint when a new entry is added
            Repaint();
        }

        private void OnLogEntryUpdated(CommandLogEntry entry)
        {
            // Force a repaint when an entry is updated
            Repaint();
        }

        private void OnLogCleared()
        {
            // Clear foldout states and repaint
            foldoutStates.Clear();
            outputFoldoutStates.Clear();
            Repaint();
        }
    }
}
