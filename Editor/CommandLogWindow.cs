using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text;

namespace Commandify
{
    public class CommandLogWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Dictionary<CommandLogEntry, bool> foldoutStates = new Dictionary<CommandLogEntry, bool>();
        private Dictionary<CommandLogEntry, bool> outputFoldoutStates = new Dictionary<CommandLogEntry, bool>();
        [NonSerialized]
        private GUIStyle commandStyle;
        [NonSerialized]
        private GUIStyle macroStyle;
        [NonSerialized]
        private GUIStyle outputStyle;
        [NonSerialized]
        private GUIStyle errorStyle;
        [NonSerialized]
        private GUIStyle selectedEntryStyle;
        [NonSerialized]
        private GUIStyle navigationButtonStyle;
        [NonSerialized]
        private GUIContent clearIcon;
        [NonSerialized]
        private GUIContent outputIcon;
        [NonSerialized]
        private GUIContent errorIcon;
        [NonSerialized]
        private GUIContent prevIcon;
        [NonSerialized]
        private GUIContent nextIcon;
        [NonSerialized]
        private Texture2D selectionTexture;

        // For entry selection and navigation
        private CommandLogEntry selectedEntry;
        private int currentIndex = -1;
        private List<CommandLogEntry> flattenedEntries = new List<CommandLogEntry>();

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

            // Use Unity's built-in selection style as a base
            selectedEntryStyle = new GUIStyle()
            {
                padding = new RectOffset(4, 4, 4, 4),
                margin = new RectOffset(0, 0, 2, 2),
                border = new RectOffset(6, 6, 6, 6),
                alignment = TextAnchor.MiddleLeft
            };
            
            // Store the texture to prevent garbage collection
            selectionTexture = MakeTexture(20, 20, new Color(0.2f, 0.4f, 0.8f, 0.3f));
            selectedEntryStyle.normal.background = selectionTexture;
            selectedEntryStyle.focused.background = selectionTexture;
            selectedEntryStyle.hover.background = selectionTexture;
            selectedEntryStyle.active.background = selectionTexture;

            navigationButtonStyle = new GUIStyle(EditorStyles.toolbarButton);

            // Load icons
            clearIcon = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
            outputIcon = EditorGUIUtility.IconContent("console.infoicon.sml");
            errorIcon = EditorGUIUtility.IconContent("console.erroricon.sml");
            prevIcon = EditorGUIUtility.IconContent("d_back");
            nextIcon = EditorGUIUtility.IconContent("d_forward");
        }

        private void OnGUI()
        {
            // Initialize styles if needed
            if (commandStyle == null)
            {
                InitializeStyles();
            }

            DrawToolbar();

            // Update flattened entries list for navigation
            UpdateFlattenedEntries();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Get top-level entries
            var topLevelEntries = CommandLogger.Instance.GetTopLevelEntries().ToList();

            // Display entries in reverse chronological order (newest first)
            for (int i = topLevelEntries.Count - 1; i >= 0; i--)
            {
                DrawLogEntry(topLevelEntries[i], 0);
            }

            EditorGUILayout.EndScrollView();

            // Handle keyboard navigation
            HandleKeyboardInput();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Navigation buttons
            EditorGUI.BeginDisabledGroup(currentIndex <= 0 || flattenedEntries.Count == 0);
            if (GUILayout.Button(prevIcon, navigationButtonStyle))
            {
                NavigateToPrevious();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(currentIndex >= flattenedEntries.Count - 1 || flattenedEntries.Count == 0);
            if (GUILayout.Button(nextIcon, navigationButtonStyle))
            {
                NavigateToNext();
            }
            EditorGUI.EndDisabledGroup();

            // Display current position
            if (flattenedEntries.Count > 0 && currentIndex >= 0 && currentIndex < flattenedEntries.Count)
            {
                GUILayout.Label($"{flattenedEntries.Count - currentIndex}/{flattenedEntries.Count}", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("0/0", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(clearIcon, EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Clear Command Log", 
                    "Are you sure you want to clear the command log?", "Yes", "No"))
                {
                    CommandLogger.Instance.Clear();
                    selectedEntry = null;
                    currentIndex = -1;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // Helper method to create a colored texture with size
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave; // Prevent garbage collection
            return texture;
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

            // Check if this entry is selected
            bool isSelected = entry == selectedEntry;

            // Create a rect for the entire entry to handle selection
            Rect entryRect = EditorGUILayout.BeginVertical();

            // Add top padding to each entry for better visual separation
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            // Draw selection background if selected
            if (isSelected)
            {
                // Make the selection rect cover the entire width
                entryRect.width = EditorGUIUtility.currentViewWidth;
                GUI.Box(entryRect, GUIContent.none, selectedEntryStyle);
            }

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

            // Handle selection click
            if (Event.current.type == EventType.MouseDown && entryRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0) // Left click
                {
                    selectedEntry = entry;
                    currentIndex = flattenedEntries.IndexOf(entry);
                    Repaint();
                }
                else if (Event.current.button == 1) // Right click
                {
                    selectedEntry = entry;
                    currentIndex = flattenedEntries.IndexOf(entry);
                    ShowContextMenu(entry);
                    Repaint();
                    Event.current.Use();
                }
            }

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

            // Add bottom padding to each entry for better visual separation
            EditorGUILayout.Space(5);
            
            EditorGUILayout.EndVertical();

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
            selectedEntry = null;
            currentIndex = -1;
            flattenedEntries.Clear();
            Repaint();
        }

        private void UpdateFlattenedEntries()
        {
            flattenedEntries.Clear();
            var topLevelEntries = CommandLogger.Instance.GetTopLevelEntries().ToList();

            // Process in reverse order (newest first) to match display order
            for (int i = topLevelEntries.Count - 1; i >= 0; i--)
            {
                FlattenEntries(topLevelEntries[i]);
            }

            // Update current index if we have a selected entry
            if (selectedEntry != null)
            {
                currentIndex = flattenedEntries.IndexOf(selectedEntry);
            }
        }

        private void FlattenEntries(CommandLogEntry entry)
        {
            flattenedEntries.Add(entry);

            // Only include children if the entry is expanded
            if (foldoutStates.TryGetValue(entry, out bool isExpanded) && isExpanded && entry.Children.Count > 0)
            {
                foreach (var child in entry.Children)
                {
                    FlattenEntries(child);
                }
            }
        }

        private void NavigateToPrevious()
        {
            if (flattenedEntries.Count == 0 || currentIndex <= 0) return;

            currentIndex--;
            selectedEntry = flattenedEntries[currentIndex];
            ScrollToSelectedEntry();
            Repaint();
        }

        private void NavigateToNext()
        {
            if (flattenedEntries.Count == 0 || currentIndex >= flattenedEntries.Count - 1) return;

            currentIndex++;
            selectedEntry = flattenedEntries[currentIndex];
            ScrollToSelectedEntry();
            Repaint();
        }

        private void ScrollToSelectedEntry()
        {
            // This is a simple implementation - in a real scenario, you might want to calculate
            // the exact position to scroll to based on the entry's position in the UI
            if (currentIndex < flattenedEntries.Count / 3)
            {
                scrollPosition.y = 0;
            }
            else if (currentIndex > (2 * flattenedEntries.Count) / 3)
            {
                scrollPosition.y = float.MaxValue;
            }
        }

        private void HandleKeyboardInput()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    NavigateToPrevious();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    NavigateToNext();
                    Event.current.Use();
                }
            }
        }

        private void ShowContextMenu(CommandLogEntry entry)
        {
            GenericMenu menu = new GenericMenu();

            // Copy command
            menu.AddItem(new GUIContent("Copy Command"), false, () => 
            {
                EditorGUIUtility.systemCopyBuffer = entry.Command;
            });

            // Copy output if available
            if (!string.IsNullOrEmpty(entry.Output))
            {
                menu.AddItem(new GUIContent("Copy Output"), false, () => 
                {
                    EditorGUIUtility.systemCopyBuffer = entry.Output;
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy Output"));
            }

            // Copy error if available
            if (!string.IsNullOrEmpty(entry.Error))
            {
                menu.AddItem(new GUIContent("Copy Error"), false, () => 
                {
                    EditorGUIUtility.systemCopyBuffer = entry.Error;
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy Error"));
            }

            // Copy all information
            menu.AddItem(new GUIContent("Copy All Information"), false, () => 
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Command: {entry.Command}");
                sb.AppendLine($"Timestamp: {entry.Timestamp}");

                if (!string.IsNullOrEmpty(entry.Output))
                {
                    sb.AppendLine("Output:");
                    sb.AppendLine(entry.Output);
                }

                if (!string.IsNullOrEmpty(entry.Error))
                {
                    sb.AppendLine("Error:");
                    sb.AppendLine(entry.Error);
                }

                EditorGUIUtility.systemCopyBuffer = sb.ToString();
            });

            // Add separator
            menu.AddSeparator("");

            // Re-run command option (if you want to implement this feature)
            menu.AddItem(new GUIContent("Re-run Command"), false, () => 
            {
                // You would need to implement this functionality
                Debug.Log($"Re-running command: {entry.Command}");
                // CommandProcessor.Instance.ExecuteCommand(entry.Command);
            });

            menu.ShowAsContext();
        }
    }
}
