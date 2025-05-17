using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using KahaGameCore.Package.EffectProcessor;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene.Editor
{
    public class EffectEditorWindow : EditorWindow
    {
        private const string EditorPrefKey = "EffectEditor_FilePath";

        private List<EffectBlock> effectBlocks = new List<EffectBlock>();
        private Dictionary<string, string[]> commandNameToArgDescs = new Dictionary<string, string[]>();
        private string descriptionFilePath = "Assets/Scripts/Cutscene/Editor/EffectCommandDescriptions.json";
        private Vector2 scrollPosition;
        private GUIStyle headerStyle;
        private GUIStyle blockStyle;
        private GUIStyle commandStyle;
        private GUIStyle dirtyLabelStyle;

        // File path handling
        private string filePath = "";
        private string lastFilePath = "";
        private bool isFilePathValid = false;
        private bool isFileContentValid = true;
        private string errorMessage = "";
        private bool showArgDescriptions = true;

        // Delayed editing fields
        private string currentEditingBlockName = "";
        private int currentEditingBlockIndex = -1;

        private string[] cachedCommandTypeNames;

        // Search functionality
        private string searchText = "";
        private List<int> filteredBlockIndices = new List<int>();

        // Dirty state tracking
        private bool isDirty = false;

        [MenuItem("Tools/Effect Editor")]
        public static void ShowWindow()
        {
            GetWindow<EffectEditorWindow>("Effect Editor");
        }

        private void OnEnable()
        {
            // Initialize styles when the window is opened
            headerStyle = new GUIStyle();
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            headerStyle.margin = new RectOffset(5, 5, 10, 10);

            filePath = EditorPrefs.GetString(EditorPrefKey, "");
            lastFilePath = filePath;
            isFilePathValid = false;
            isFileContentValid = true;
            errorMessage = "";
            showArgDescriptions = true;
            currentEditingBlockName = "";
            currentEditingBlockIndex = -1;
            isDirty = false;

            cachedCommandTypeNames = null;
            cachedCommandTypeNames = GetEffectCommandTypeNames();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            ValidateFilePath();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorPrefs.SetString(EditorPrefKey, filePath);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SaveEditorState();
            }
        }

        private void SaveEditorState()
        {
            EditorPrefs.SetString(EditorPrefKey, filePath);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Effect Command Editor", headerStyle);
            EditorGUILayout.Space();

            // File path selection
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            filePath = EditorGUILayout.TextField("File Path", filePath);

            if (lastFilePath != filePath)
            {
                // Check if there are unsaved changes before changing file
                if (isDirty && EditorUtility.DisplayDialog("Unsaved Changes",
                    "You have unsaved changes. Do you want to save them before changing the file?",
                    "Save", "Discard"))
                {
                    SaveToFile();
                }

                lastFilePath = filePath;
                ValidateFilePath();
                SaveEditorState();
                isDirty = false;
            }

            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                if (isDirty && EditorUtility.DisplayDialog("Unsaved Changes",
                    "You have unsaved changes. Do you want to save them before browsing for a new file?",
                    "Save", "Discard"))
                {
                    SaveToFile();
                }

                string newPath = EditorUtility.OpenFilePanel("Select Effect File", Application.dataPath, "txt");

                if (!string.IsNullOrEmpty(newPath))
                {
                    filePath = newPath;
                    ValidateFilePath();
                    SaveEditorState();
                    isDirty = false;
                }
                else
                {
                    EditorGUILayout.EndHorizontal();
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            // Display dirty state indicator
            if (isDirty)
            {
                dirtyLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                dirtyLabelStyle.normal.textColor = Color.red;

                EditorGUILayout.LabelField("* Unsaved Changes *", dirtyLabelStyle);
            }

            // Display error message if file content is invalid
            if (!isFileContentValid)
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                return;
            }

            EditorGUILayout.Space();

            // Only show the editor content if file path is valid
            if (isFilePathValid)
            {
                // Search bar
                EditorGUILayout.BeginHorizontal();
                GUI.SetNextControlName("SearchField");
                string newSearchText = EditorGUILayout.TextField("Search Blocks", searchText);

                if (newSearchText != searchText)
                {
                    searchText = newSearchText;
                    UpdateFilteredBlocks();
                }

                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                {
                    searchText = "";
                    UpdateFilteredBlocks();
                    GUI.FocusControl("SearchField");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                showArgDescriptions = EditorGUILayout.Toggle("Show Arg Descriptions", showArgDescriptions);

                if (GUILayout.Button("Edit Descriptions", GUILayout.Width(120)))
                {
                    EditCommandDescriptions();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                // Draw each effect block based on search filter
                if (string.IsNullOrEmpty(searchText))
                {
                    // Show all blocks
                    for (int i = 0; i < effectBlocks.Count; i++)
                    {
                        DrawEffectBlock(i);
                    }
                }
                else
                {
                    // Show only filtered blocks
                    foreach (int blockIndex in filteredBlockIndices)
                    {
                        DrawEffectBlock(blockIndex);
                    }

                    // Show message if no blocks found
                    if (filteredBlockIndices.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No blocks matching the search criteria.", MessageType.Info);
                    }
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                // Bottom toolbar
                EditorGUILayout.BeginHorizontal();

                // Add new block button
                if (GUILayout.Button("Add Block", GUILayout.Width(100)))
                {
                    AddNewBlock();
                    isDirty = true;
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh Command Cache", GUILayout.Width(300)))
                {
                    cachedCommandTypeNames = null;
                    GetEffectCommandTypeNames();
                    ValidateFilePath();
                    EditorUtility.DisplayDialog("Cache Refreshed", "Command cache has been refreshed.", "OK");
                }

                GUILayout.FlexibleSpace();

                // Save button
                EditorGUI.BeginDisabledGroup(!isDirty);
                if (GUILayout.Button("Save", GUILayout.Width(100)))
                {
                    if (currentEditingBlockIndex != -1)
                    {
                        FinishEditingBlockName();
                    }

                    GUI.FocusControl(null);
                    EditorApplication.update += WaitForUpdatesToSave;
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
            }
        }

        private void WaitForUpdatesToSave()
        {
            EditorApplication.update -= WaitForUpdatesToSave;
            SaveToFile();
        }

        private void DrawEffectBlock(int blockIndex)
        {
            EffectBlock block = effectBlocks[blockIndex];

            blockStyle = new GUIStyle(EditorStyles.helpBox);
            blockStyle.margin = new RectOffset(5, 5, 2, 2);
            blockStyle.padding = new RectOffset(5, 5, 5, 5);

            EditorGUILayout.BeginVertical(blockStyle);
            EditorGUILayout.BeginHorizontal();

            if (currentEditingBlockIndex == blockIndex)
            {
                EditorGUI.BeginChangeCheck();
                currentEditingBlockName = EditorGUILayout.TextField("Block Name", currentEditingBlockName);
                if (EditorGUI.EndChangeCheck())
                {
                    isDirty = true;
                }

                if (!IsBlockNameValid(currentEditingBlockName, blockIndex))
                {
                    EditorGUILayout.HelpBox("Block name must be unique and not empty", MessageType.Error);
                }

                if (GUILayout.Button("OK"))
                {
                    FinishEditingBlockName();
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Block Name", block.name);

                if (GUILayout.Button("Edit Name"))
                {
                    currentEditingBlockIndex = blockIndex;
                    currentEditingBlockName = block.name;
                    GUIUtility.keyboardControl = 0;
                    GUIUtility.ExitGUI();
                }

                if (block.isExpanded)
                {
                    if (GUILayout.Button("Fold"))
                    {
                        block.isExpanded = false;
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    if (GUILayout.Button("Expand"))
                    {
                        block.isExpanded = true;
                        GUIUtility.ExitGUI();
                    }
                }
            }

            // Delete block button
            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
                effectBlocks.RemoveAt(blockIndex);
                if (currentEditingBlockIndex == blockIndex)
                {
                    currentEditingBlockIndex = -1;
                }
                UpdateFilteredBlocks();
                isDirty = true;
                GUIUtility.ExitGUI();
                return;
            }

            EditorGUILayout.EndHorizontal();

            if (block.isExpanded)
            {
                EditorGUILayout.Space();

                // Draw commands
                for (int i = 0; i < block.commands.Count; i++)
                {
                    DrawCommandLine(block, i);
                }

                // Add command button
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    block.commands.Add(new EffectCommand());
                    isDirty = true;
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawCommandLine(EffectBlock block, int commandIndex)
        {
            EffectCommand command = block.commands[commandIndex];

            commandStyle = new GUIStyle(EditorStyles.helpBox);
            commandStyle.margin = new RectOffset(5, 5, 2, 2);
            commandStyle.padding = new RectOffset(5, 5, 5, 5);

            EditorGUILayout.BeginVertical(commandStyle);
            EditorGUILayout.BeginHorizontal();

            // Command type dropdown
            int selectedIndex = 0;
            string[] commandTypeNames = GetEffectCommandTypeNames();
            for (int i = 0; i < commandTypeNames.Length; i++)
            {
                if (commandTypeNames[i] == command.name)
                {
                    selectedIndex = i;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = EditorGUILayout.Popup("Command", selectedIndex, commandTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
                if (newSelectedIndex != selectedIndex)
                {
                    command.name = commandTypeNames[newSelectedIndex];
                    for (int i = 0; i < 5; i++)
                    {
                        command.argDescriptions[i] = commandNameToArgDescs.ContainsKey(commandTypeNames[newSelectedIndex]) ? commandNameToArgDescs[commandTypeNames[newSelectedIndex]][i] : "";
                    }
                }
            }

            // Delete command button
            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
                block.commands.RemoveAt(commandIndex);
                isDirty = true;
                GUIUtility.ExitGUI();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                return;
            }

            EditorGUILayout.EndHorizontal();

            // Draw argument fields (5 fields) - using delayed text fields to improve performance
            EditorGUILayout.BeginVertical();
            for (int i = 0; i < 5; i++)
            {
                EditorGUILayout.BeginHorizontal();

                string argLabel = $"Arg {i + 1}";
                if (showArgDescriptions && !string.IsNullOrEmpty(command.argDescriptions[i]))
                {
                    argLabel += $" ({command.argDescriptions[i]})";
                }

                EditorGUI.BeginChangeCheck();
                command.arguments[i] = EditorGUILayout.DelayedTextField(argLabel, command.arguments[i]);

                if (EditorGUI.EndChangeCheck())
                {
                    isDirty = true;
                }

                if (showArgDescriptions && GUILayout.Button("?", GUILayout.Width(25)))
                {
                    EditArgDescription(command, i, command.name);
                }

                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(command.argDescriptions[i]) && string.IsNullOrEmpty(command.arguments[i]))
                {
                    EditorGUILayout.HelpBox("Argument cannot be empty if description is provided.", MessageType.Warning);
                }

                if (string.IsNullOrEmpty(command.argDescriptions[i]) && !string.IsNullOrEmpty(command.arguments[i]))
                {
                    EditorGUILayout.HelpBox("Description is empty but argument is provided.", MessageType.Warning);
                }
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Up"))
            {
                int newIndex = commandIndex - 1;
                if (newIndex >= 0)
                {
                    EffectCommand temp = block.commands[newIndex];
                    block.commands[newIndex] = block.commands[commandIndex];
                    block.commands[commandIndex] = temp;
                    isDirty = true;
                }
            }

            if (GUILayout.Button("Insert"))
            {
                EffectCommand newCommand = new EffectCommand();
                block.commands.Insert(commandIndex + 1, newCommand);
                isDirty = true;
            }

            if (GUILayout.Button("Down"))
            {
                int newIndex = commandIndex + 1;
                if (newIndex < block.commands.Count)
                {
                    EffectCommand temp = block.commands[newIndex];
                    block.commands[newIndex] = block.commands[commandIndex];
                    block.commands[commandIndex] = temp;
                    isDirty = true;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void EditArgDescription(EffectCommand command, int argIndex, string commandName)
        {
            string description = command.argDescriptions[argIndex];
            string result = EditorInputDialog.Show("Edit Arg Description", $"Description for {commandName} Arg {argIndex + 1}:", description);

            if (result != null)
            {
                command.argDescriptions[argIndex] = result;

                if (!commandNameToArgDescs.ContainsKey(commandName))
                {
                    commandNameToArgDescs[commandName] = new string[5] { "", "", "", "", "" };
                }

                commandNameToArgDescs[commandName][argIndex] = result;

                SaveCommandDescriptions();
            }

            GUIUtility.ExitGUI();
        }

        private void EditCommandDescriptions()
        {
            var window = GetWindow<CommandDescriptionsEditorWindow>("Command Descriptions Editor");
            window.Initialize(commandNameToArgDescs, cachedCommandTypeNames, OnEditDescriptionUpdated);
        }

        private void OnEditDescriptionUpdated()
        {
            SaveCommandDescriptions();

            foreach (var block in effectBlocks)
            {
                foreach (var command in block.commands)
                {
                    string commandName = command.name;
                    if (commandNameToArgDescs.ContainsKey(commandName))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (i < commandNameToArgDescs[commandName].Length)
                            {
                                command.argDescriptions[i] = commandNameToArgDescs[commandName][i];
                            }
                        }
                    }
                }
            }
        }

        private void LoadCommandDescriptions()
        {
            commandNameToArgDescs.Clear();

            if (File.Exists(descriptionFilePath))
            {
                try
                {
                    string json = File.ReadAllText(descriptionFilePath);
                    CommandDescriptionsData data = JsonUtility.FromJson<CommandDescriptionsData>(json);

                    if (data != null && data.descriptions != null)
                    {
                        foreach (var item in data.descriptions)
                        {
                            commandNameToArgDescs[item.commandName] = item.argDescriptions;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading command descriptions: {e.Message}");
                }
            }
        }

        private void SaveCommandDescriptions()
        {
            try
            {
                var data = new CommandDescriptionsData();
                data.descriptions = new List<CommandDescriptionItem>();

                foreach (var pair in commandNameToArgDescs)
                {
                    data.descriptions.Add(new CommandDescriptionItem
                    {
                        commandName = pair.Key,
                        argDescriptions = pair.Value
                    });
                }

                string json = JsonUtility.ToJson(data, true);

                if (!Directory.Exists(Path.GetDirectoryName(descriptionFilePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(descriptionFilePath));
                }

                File.WriteAllText(descriptionFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving command descriptions: {e.Message}");
            }
        }

        private void AddNewBlock()
        {
            string baseName = "Block";
            string uniqueName = baseName;
            int counter = 1;

            // Find a unique name
            while (effectBlocks.Any(b => b.name == uniqueName))
            {
                uniqueName = $"{baseName}{counter}";
                counter++;
            }

            effectBlocks.Add(new EffectBlock(uniqueName));
            UpdateFilteredBlocks();
        }

        private bool IsBlockNameValid(string name, int currentBlockIndex)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Check if any other block has the same name
            for (int i = 0; i < effectBlocks.Count; i++)
            {
                if (i != currentBlockIndex && effectBlocks[i].name == name)
                    return false;
            }

            return true;
        }

        private void FinishEditingBlockName()
        {
            if (currentEditingBlockIndex >= 0 && currentEditingBlockIndex < effectBlocks.Count)
            {
                if (IsBlockNameValid(currentEditingBlockName, currentEditingBlockIndex))
                {
                    effectBlocks[currentEditingBlockIndex].name = currentEditingBlockName;
                    UpdateFilteredBlocks();
                    isDirty = true;
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Block Name", "Block name must be unique and not empty.", "OK");
                }
            }

            currentEditingBlockIndex = -1;
            currentEditingBlockName = "";
        }

        private string[] GetEffectCommandTypeNames()
        {
            if (cachedCommandTypeNames != null && cachedCommandTypeNames.Length > 0)
            {
                return cachedCommandTypeNames;
            }

            // Get all types that inherit from EffectCommandBase but exclude those in KahaGameCore.Tests namespace
            var commandTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        return new Type[0]; // Return empty array if assembly can't be loaded
                    }
                })
                .Where(type =>
                    type != null &&
                    type.IsClass &&
                    !type.IsAbstract &&
                    typeof(EffectCommandBase).IsAssignableFrom(type) &&
                    type.Namespace.StartsWith("Assets.Scripts.Gameplay.Cutscene"))
                .Select(type => type.Name)
                .ToArray();

            // If no types found, provide a default option
            if (commandTypes.Length == 0)
            {
                return new[] { "No Commands Found" };
            }

            Array.Sort(commandTypes); // Sort the command types alphabetically
            commandTypes = new List<string>(commandTypes) { "None" }.ToArray(); // Add "None" option at the end
            cachedCommandTypeNames = commandTypes;

            return commandTypes;
        }

        private void ValidateFilePath()
        {
            isFilePathValid = !string.IsNullOrEmpty(filePath) && File.Exists(filePath);

            if (isFilePathValid)
            {
                LoadCommandDescriptions();
                LoadFromFile();
                UpdateFilteredBlocks();
            }
            else
            {
                commandNameToArgDescs.Clear();
                isFileContentValid = false;
                errorMessage = "Invalid file path or file does not exist.";
                effectBlocks.Clear();
                filteredBlockIndices.Clear();
            }
        }

        private void LoadFromFile()
        {
            try
            {
                string content = File.ReadAllText(filePath);
                effectBlocks.Clear();
                isFileContentValid = true;

                if (string.IsNullOrWhiteSpace(content))
                {
                    // Empty file is valid
                    return;
                }

                // Parse the file content
                EffectBlock currentBlock = null;
                int lineNum = 0;

                using (StringReader reader = new StringReader(content))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNum++;
                        line = line.Trim();

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        if (line == "{")
                        {
                            if (currentBlock == null)
                            {
                                SetErrorState($"Found opening bracket without block name at line {lineNum}");
                                return;
                            }

                            continue;
                        }
                        else if (line == "}")
                        {
                            currentBlock = null;
                            continue;
                        }
                        else if (currentBlock == null)
                        {
                            // This should be a block name
                            currentBlock = new EffectBlock(line);

                            // Check if block name is already used
                            if (effectBlocks.Any(b => b.name == line))
                            {
                                SetErrorState($"Duplicate block name '{line}' at line {lineNum}");
                                return;
                            }

                            effectBlocks.Add(currentBlock);
                        }
                        else
                        {
                            // This should be a command
                            Match match = Regex.Match(line, @"^\s*(\w+)\s*\((.*)\);$");
                            if (match.Success)
                            {
                                string commandName = match.Groups[1].Value;
                                string argsString = match.Groups[2].Value;

                                // Get command index
                                string[] commandTypes = GetEffectCommandTypeNames();
                                int commandIndex = Array.IndexOf(commandTypes, commandName);

                                if (commandIndex < 0)
                                {
                                    if (commandTypes.Length > 0 && commandTypes[0] != "No Commands Found")
                                    {
                                        SetErrorState($"Unknown command type '{commandName}' at line {lineNum}");
                                        return;
                                    }
                                    commandIndex = 0; // Default to first command if command not found
                                }

                                // Parse arguments
                                string[] args = argsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                         .Select(a => a.Trim())
                                                         .ToArray();

                                EffectCommand command = new EffectCommand();
                                command.name = commandName;

                                // Add arguments
                                for (int i = 0; i < 5; i++)
                                {
                                    command.arguments[i] = args.Length > i ? args[i] : "";
                                    command.argDescriptions[i] = commandNameToArgDescs.ContainsKey(commandName) ? commandNameToArgDescs[commandName][i] : "";
                                }

                                currentBlock.commands.Add(command);
                            }
                            else
                            {
                                SetErrorState($"Invalid command format at line {lineNum}: '{line}'");
                                return;
                            }
                        }
                    }
                }

                // Check if we ended with an unclosed block
                if (currentBlock != null)
                {
                    SetErrorState("File ended with an unclosed block");
                    return;
                }
            }
            catch (Exception e)
            {
                SetErrorState($"Error loading file: {e.Message}");
            }
        }

        private void SetErrorState(string message)
        {
            isFileContentValid = false;
            errorMessage = message;
            effectBlocks.Clear();
            filteredBlockIndices.Clear();
        }

        private void SaveToFile()
        {
            if (!isFilePathValid)
                return;

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    foreach (var block in effectBlocks)
                    {
                        writer.WriteLine(block.name);
                        writer.WriteLine("{");

                        foreach (var command in block.commands)
                        {
                            // Get non-empty arguments
                            List<string> usedArgs = new List<string>();
                            foreach (var arg in command.arguments)
                            {
                                if (!string.IsNullOrEmpty(arg))
                                {
                                    usedArgs.Add(arg);
                                }
                                else
                                {
                                    // Stop at first empty argument
                                    break;
                                }
                            }

                            string argString = string.Join(", ", usedArgs);
                            writer.WriteLine($"\t{command.name}({argString});");
                        }

                        writer.WriteLine("}");
                        writer.WriteLine();
                    }
                }

                // Clear dirty flag
                isDirty = false;

                // Refresh/reimport the asset in the project to make sure changes are applied immediately
                if (filePath.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length);
                    AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
                }

                EditorUtility.DisplayDialog("Save Successful", "Effect commands saved successfully!", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Save Error", $"Error saving file: {e.Message}", "OK");
            }
        }

        // Update the filtered block indices based on search text
        private void UpdateFilteredBlocks()
        {
            filteredBlockIndices.Clear();

            if (string.IsNullOrEmpty(searchText))
            {
                // If search text is empty, all blocks are in the filter
                for (int i = 0; i < effectBlocks.Count; i++)
                {
                    filteredBlockIndices.Add(i);
                }
            }
            else
            {
                // Case-insensitive search
                string lowerSearchText = searchText.ToLower();

                // Find blocks that contain the search text in their name
                for (int i = 0; i < effectBlocks.Count; i++)
                {
                    if (effectBlocks[i].name.ToLower().Contains(lowerSearchText))
                    {
                        filteredBlockIndices.Add(i);
                    }
                }
            }
        }

        // 命令描述数据结构
        [Serializable]
        public class CommandDescriptionsData
        {
            public List<CommandDescriptionItem> descriptions;
        }

        [Serializable]
        public class CommandDescriptionItem
        {
            public string commandName;
            public string[] argDescriptions = new string[5];
        }

        // 输入对话框工具类
        public class EditorInputDialog : EditorWindow
        {
            public static string Show(string title, string message, string defaultText)
            {
                EditorInputDialog window = CreateInstance<EditorInputDialog>();
                window.titleContent = new GUIContent(title);
                window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 150);
                window._message = message;
                window._text = defaultText;
                window.ShowModalUtility();
                return window._result;
            }

            private string _message;
            private string _text;
            private string _result;
            private bool _cancelled = true;

            private void OnGUI()
            {
                EditorGUILayout.LabelField(_message);
                EditorGUILayout.Space();
                GUI.SetNextControlName("TextField");
                _text = EditorGUILayout.TextField(_text);
                EditorGUILayout.Space();

                // 聚焦到文本字段
                EditorGUI.FocusTextInControl("TextField");

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Cancel"))
                {
                    _cancelled = true;
                    Close();
                }

                if (GUILayout.Button("OK"))
                {
                    _cancelled = false;
                    _result = _text;
                    Close();
                }
                EditorGUILayout.EndHorizontal();

                // 处理回车键
                if (Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.KeyDown)
                {
                    _cancelled = false;
                    _result = _text;
                    Close();
                }

                // 处理Escape键
                if (Event.current.keyCode == KeyCode.Escape && Event.current.type == EventType.KeyDown)
                {
                    _cancelled = true;
                    Close();
                }
            }

            private void OnDestroy()
            {
                if (_cancelled)
                {
                    _result = null;
                }
            }
        }

        // 命令描述编辑器窗口
        public class CommandDescriptionsEditorWindow : EditorWindow
        {
            private Dictionary<string, string[]> commandDescriptionsReference;
            private string[] commandTypeNames;
            private Action onDescriptionsUpdated;
            private Vector2 scrollPosition;
            private string searchText = "";
            private bool isDirty = false;

            public void Initialize(Dictionary<string, string[]> descriptions, string[] commands, Action callback)
            {
                commandDescriptionsReference = descriptions;
                commandTypeNames = commands;
                onDescriptionsUpdated = callback;
                isDirty = false;
            }

            private void OnGUI()
            {
                if (commandTypeNames == null || commandDescriptionsReference == null)
                    return;

                EditorGUILayout.LabelField("Command Argument Descriptions", EditorStyles.boldLabel);

                if (isDirty)
                {
                    EditorGUILayout.LabelField("* Unsaved Changes *", EditorStyles.boldLabel);
                }

                EditorGUILayout.Space();

                // 搜索框
                searchText = EditorGUILayout.TextField("Search", searchText);
                EditorGUILayout.Space();

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                foreach (string commandName in commandTypeNames)
                {
                    // 如果有搜索文本且当前命令不匹配，则跳过
                    if (!string.IsNullOrEmpty(searchText) &&
                        !commandName.ToLower().Contains(searchText.ToLower()))
                    {
                        continue;
                    }

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(commandName, EditorStyles.boldLabel);

                    // 确保该命令在字典中存在
                    if (!commandDescriptionsReference.ContainsKey(commandName))
                    {
                        commandDescriptionsReference[commandName] = new string[5] { "", "", "", "", "" };
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        EditorGUI.BeginChangeCheck();
                        string newDescription = EditorGUILayout.TextField(
                            $"Arg {i + 1}", commandDescriptionsReference[commandName][i]);

                        if (EditorGUI.EndChangeCheck())
                        {
                            commandDescriptionsReference[commandName][i] = newDescription;
                            isDirty = true;
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }

                EditorGUILayout.EndScrollView();

                // 底部工具栏
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Save & Close"))
                {
                    if (onDescriptionsUpdated != null)
                    {
                        onDescriptionsUpdated.Invoke();
                    }
                    isDirty = false;
                    Close();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            private void OnDestroy()
            {
                // If dirty and about to close, ask if user wants to save
                if (isDirty)
                {
                    if (EditorUtility.DisplayDialog("Unsaved Changes",
                        "You have unsaved changes to command descriptions. Save them?",
                        "Save", "Discard"))
                    {
                        if (onDescriptionsUpdated != null)
                        {
                            onDescriptionsUpdated.Invoke();
                        }
                    }
                }
            }
        }
    }

    // Model classes
    public class EffectBlock
    {
        public string name;
        public List<EffectCommand> commands = new List<EffectCommand>();
        public bool isExpanded = false;

        public EffectBlock(string name)
        {
            this.name = name;
        }
    }

    public class EffectCommand
    {
        public string name = "None";
        public string[] arguments = new string[5] { "", "", "", "", "" };
        public string[] argDescriptions = new string[5] { "", "", "", "", "" };
    }
}