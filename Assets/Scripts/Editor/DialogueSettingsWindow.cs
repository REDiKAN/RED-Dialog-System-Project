using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class DialogueSettingsWindow : EditorWindow
{
    private DialogueSettingsData _settings;
    private VisualElement _root;
    private VisualElement _rightPanel;
    private string[] _categories = { "General", "UI", "File Management", "Audio", "Favorite Nodes" };
    private int _selectedCategoryIndex = 0;

    // Словарь для хранения всех переключателей
    private Dictionary<string, Toggle> _nodeTypeToggles = new Dictionary<string, Toggle>();

    [MenuItem("Dialog System/Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow<DialogueSettingsWindow>("Dialogue Settings");
        window.minSize = new Vector2(600, 400);
    }

    private void OnEnable()
    {
        LoadOrCreateSettings();
        CreateGUI();
    }

    private void LoadOrCreateSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:DialogueSettingsData");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _settings = AssetDatabase.LoadAssetAtPath<DialogueSettingsData>(path);
        }
        else
        {
            _settings = ScriptableObject.CreateInstance<DialogueSettingsData>();
            string path = "Assets/Resources/DialogueSettings.asset";
            AssetDatabase.CreateAsset(_settings, path);
            AssetDatabase.SaveAssets();
        }
    }

    private void CreateGUI()
    {
        _root = rootVisualElement;
        _root.Clear();

        // Очищаем словарь переключателей при пересоздании интерфейса
        _nodeTypeToggles.Clear();

        // Toolbar with Save button
        var toolbar = new Toolbar();
        var saveButton = new Button(SaveSettings) { text = "Save" };
        toolbar.Add(saveButton);
        _root.Add(toolbar);

        // Two-panel layout
        var splitView = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
        _root.Add(splitView);

        // Left panel: category list
        var leftPanel = new ScrollView();
        for (int i = 0; i < _categories.Length; i++)
        {
            int index = i;
            var button = new Button(() => SelectCategory(index)) { text = _categories[i] };
            if (i == _selectedCategoryIndex) button.AddToClassList("selected");
            leftPanel.Add(button);
        }
        splitView.Add(leftPanel);

        // Right panel: settings editor
        _rightPanel = new VisualElement();
        splitView.Add(_rightPanel);
        RefreshRightPanel();
    }

    private void SelectCategory(int index)
    {
        _selectedCategoryIndex = index;
        CreateGUI(); // Rebuild to update selection highlight
    }

    private void RefreshRightPanel()
    {
        _rightPanel.Clear();
        switch (_selectedCategoryIndex)
        {
            case 0:
                DrawGeneralSettings();
                break;
            case 1:
                DrawUISettings();
                break;
            case 2:
                DrawFileManagementSettings();
                break;
            case 3:
                DrawAudioSettings();
                break;
            case 4:
                DrawFavoriteNodesSettings();
                break;
        }
    }

    private void DrawFavoriteNodesSettings()
    {
        _rightPanel.Add(new Label("Favorite Nodes")
        {
            style = {
            fontSize = 14,
            unityFontStyleAndWeight = FontStyle.Bold
        }
        });

        // ScrollView для списка узлов (без кнопок Select/Deselect All)
        var scrollView = new ScrollView();
        scrollView.style.maxHeight = 400;
        scrollView.style.marginBottom = 10;

        // Сгруппированные категории узлов
        AddNodeCategoryToPanel(scrollView, "Speech Nodes", new[] {
        "SpeechNodeText", "SpeechNodeAudio", "SpeechNodeImage", "SpeechNodeRandText"
    });

        AddNodeCategoryToPanel(scrollView, "Option Nodes", new[] {
        "OptionNodeText", "OptionNodeAudio", "OptionNodeImage"
    });

        AddNodeCategoryToPanel(scrollView, "Condition Nodes", new[] {
        "IntConditionNode", "StringConditionNode", "CharacterIntConditionNode"
    });

        AddNodeCategoryToPanel(scrollView, "Action Nodes", new[] {
        "ModifyIntNode", "CharacterModifyIntNode", "EventNode"
    });

        AddNodeCategoryToPanel(scrollView, "Utility Nodes", new[] {
        "EndNode", "NoteNode", "TimerNode", "PauseNode", "RandomBranchNode",
        "WireNode", "ChatSwitchNode", "ChangeChatIconNode", "ChangeChatNameNode"
    });

        AddNodeCategoryToPanel(scrollView, "Character Nodes", new[] {
        "CharacterButtonPressNode"
    });

        AddNodeCategoryToPanel(scrollView, "Debug Nodes", new[] {
        "DebugLogNode", "DebugWarningNode", "DebugErrorNode"
    });

        _rightPanel.Add(scrollView);
    }

    private void AddNodeCategoryToPanel(VisualElement parent, string categoryName, string[] nodeTypes)
    {
        // Заголовок категории
        var categoryLabel = new Label(categoryName)
        {
            style = {
            fontSize = 12,
            unityFontStyleAndWeight = FontStyle.Bold,
            marginTop = 10,
            marginBottom = 5
        }
        };
        parent.Add(categoryLabel);

        // Чекбоксы для каждого типа узла
        foreach (var nodeType in nodeTypes)
        {
            bool isFavorite = _settings.FavoriteNodeTypes.Contains(nodeType);
            var toggle = new Toggle(GetDisplayNameForNodeType(nodeType)) { value = isFavorite };
            toggle.userData = nodeType;

            toggle.RegisterValueChangedCallback(evt =>
            {
                var nodeTypeName = (string)toggle.userData;
                if (evt.newValue)
                {
                    if (!_settings.FavoriteNodeTypes.Contains(nodeTypeName))
                        _settings.FavoriteNodeTypes.Add(nodeTypeName);
                }
                else
                {
                    _settings.FavoriteNodeTypes.Remove(nodeTypeName);
                }

                EditorUtility.SetDirty(_settings);
            });

            // Сохраняем ссылку на переключатель для будущего обновления
            _nodeTypeToggles[nodeType] = toggle;

            parent.Add(toggle);
        }
    }

    // Вспомогательный метод для получения отображаемого названия узла
    private string GetDisplayNameForNodeType(string nodeType)
    {
        // Сопоставление имен классов с отображаемыми названиями
        Dictionary<string, string> displayNames = new Dictionary<string, string>
    {
        {"SpeechNodeText", "Speech (Text)"},
        {"SpeechNodeAudio", "Speech (Audio)"},
        {"SpeechNodeImage", "Speech (Image)"},
        {"SpeechNodeRandText", "Speech Rand (Text)"},
        {"OptionNodeText", "Option (Text)"},
        {"OptionNodeAudio", "Option (Audio)"},
        {"OptionNodeImage", "Option (Image)"},
        {"IntConditionNode", "Condition (Int)"},
        {"StringConditionNode", "Condition (String)"},
        {"CharacterIntConditionNode", "Character Condition (Int)"},
        {"ModifyIntNode", "Modify Int"},
        {"CharacterModifyIntNode", "Character Modify Int"},
        {"EventNode", "Event"},
        {"EndNode", "End Node"},
        {"NoteNode", "Note Node"},
        {"TimerNode", "Timer"},
        {"PauseNode", "Pause"},
        {"RandomBranchNode", "Random Branch"},
        {"WireNode", "Wire"},
        {"ChatSwitchNode", "Chat Switch"},
        {"ChangeChatIconNode", "Change Chat Icon"},
        {"ChangeChatNameNode", "Change Chat Name"},
        {"CharacterButtonPressNode", "Character Button Press"},
        {"DebugLogNode", "Debug Log"},
        {"DebugWarningNode", "Debug Warning"},
        {"DebugErrorNode", "Debug Error"}
    };

        return displayNames.TryGetValue(nodeType, out string displayName) ? displayName : nodeType;
    }

    private void SetAllFavorites(bool isSelected)
    {
        // Обновляем состояние всех переключателей
        foreach (var toggle in _nodeTypeToggles.Values)
        {
            toggle.SetValueWithoutNotify(isSelected);
        }

        // Обновляем данные
        _settings.FavoriteNodeTypes.Clear();

        if (isSelected)
        {
            foreach (var nodeType in _nodeTypeToggles.Keys)
            {
                _settings.FavoriteNodeTypes.Add(nodeType);
            }
        }

        EditorUtility.SetDirty(_settings);
    }

    // Существующие методы для других вкладок (оставлены без изменений)
    private void DrawFileManagementSettings()
    {
        _rightPanel.Add(new Label("File Management")
        {
            style = {
                fontSize = 14,
                unityFontStyleAndWeight = FontStyle.Bold
            }
        });

        // Контейнер для элементов с отступами
        var container = new VisualElement();
        container.style.marginTop = 10;
        container.style.marginBottom = 10;
        container.style.marginLeft = 5;
        container.style.marginRight = 5;

        // Bool-переключатель
        var enableToggle = new Toggle("Enable Auto-save Location")
        {
            value = _settings.General.enableAutoSaveLocation
        };
        enableToggle.RegisterValueChangedCallback(evt =>
        {
            _settings.General.enableAutoSaveLocation = evt.newValue;
            EditorUtility.SetDirty(_settings);
        });
        container.Add(enableToggle);

        // Контейнер для поля пути и кнопки
        var pathContainer = new VisualElement
        {
            style = {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginTop = 5
            }
        };

        // Поле для отображения пути
        string displayPath = !string.IsNullOrEmpty(_settings.General.autoSaveFolderPath)
            ? _settings.General.autoSaveFolderPath
            : "Not set";
        var pathField = new TextField
        {
            value = displayPath,
            isReadOnly = true
        };
        pathField.style.flexGrow = 1;

        // Кнопка выбора папки с иконкой
        var browseButton = new Button(SelectAutoSaveFolder) { text = "📁" };
        browseButton.style.width = 30;
        browseButton.style.marginLeft = 5;
        pathContainer.Add(pathField);
        pathContainer.Add(browseButton);
        container.Add(pathContainer);

        // Отключаем элементы, если переключатель выключен
        pathContainer.SetEnabled(_settings.General.enableAutoSaveLocation);
        enableToggle.RegisterValueChangedCallback(evt =>
        {
            pathContainer.SetEnabled(evt.newValue);
            EditorUtility.SetDirty(_settings);
        });

        _rightPanel.Add(container);
    }

    private void SelectAutoSaveFolder()
    {
        string initialPath = Application.dataPath;
        // Если уже задан путь - используем его как начальный
        if (!string.IsNullOrEmpty(_settings.General.autoSaveFolderPath))
        {
            string fullPath = _settings.GetFullPath();
            if (!string.IsNullOrEmpty(fullPath) && System.IO.Directory.Exists(fullPath))
                initialPath = fullPath;
        }

        string selectedPath = EditorUtility.OpenFolderPanel(
            "Select Auto-save Folder",
            initialPath,
            ""
        );

        if (string.IsNullOrEmpty(selectedPath))
            return;

        // Проверяем, что путь находится внутри папки Assets
        if (selectedPath.StartsWith(Application.dataPath))
        {
            _settings.SetAutoSaveFolderPath(selectedPath);
            EditorUtility.SetDirty(_settings);
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Invalid Path",
                "Please select a folder inside the Assets directory.",
                "OK"
            );
            _settings.General.enableAutoSaveLocation = false;
            EditorUtility.SetDirty(_settings);
        }
    }

    private void DrawGeneralSettings()
    {
        _rightPanel.Add(new Label("General Settings") { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });

        var delayField = new TextField("Default Message Delay") { value = _settings.General.DefaultMessageDelay };
        delayField.RegisterValueChangedCallback(evt => _settings.General.DefaultMessageDelay = evt.newValue);
        _rightPanel.Add(delayField);

        var autoScrollToggle = new Toggle("Auto Scroll Enabled") { value = _settings.General.AutoScrollEnabled };
        autoScrollToggle.RegisterValueChangedCallback(evt => _settings.General.AutoScrollEnabled = evt.newValue);
        _rightPanel.Add(autoScrollToggle);

        var quickCreateToggle = new Toggle("Enable Quick Node Creation on Drag Drop") { value = _settings.General.EnableQuickNodeCreationOnDragDrop };
        quickCreateToggle.RegisterValueChangedCallback(evt => _settings.General.EnableQuickNodeCreationOnDragDrop = evt.newValue);
        _rightPanel.Add(quickCreateToggle);

        var hotkeyToggle = new Toggle("Enable Undo/Redo Hotkeys (Ctrl+Z/Y)") { value = _settings.General.EnableHotkeyUndoRedo };
        hotkeyToggle.RegisterValueChangedCallback(evt => _settings.General.EnableHotkeyUndoRedo = evt.newValue);
        _rightPanel.Add(hotkeyToggle);

        // AUTO-SAVE: Добавляем toggle для новой настройки
        var autoSaveToggle = new Toggle("Auto-save on Unity close") { value = _settings.General.AutoSaveOnUnityClose };
        autoSaveToggle.RegisterValueChangedCallback(evt => _settings.General.AutoSaveOnUnityClose = evt.newValue);
        _rightPanel.Add(autoSaveToggle);

        var entryNodeMovementToggle = new Toggle("Enable Entry Node Movement")
        {
            value = _settings.General.EnableEntryNodeMovement
        };
        entryNodeMovementToggle.RegisterValueChangedCallback(evt => _settings.General.EnableEntryNodeMovement = evt.newValue);
        _rightPanel.Add(entryNodeMovementToggle);
    }

    private void DrawUISettings()
    {
        _rightPanel.Add(new Label("UI Settings") { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });

        var useCustomBgToggle = new Toggle("Use Custom Background Color") { value = _settings.UI.UseCustomBackgroundColor };
        useCustomBgToggle.RegisterValueChangedCallback(evt => _settings.UI.UseCustomBackgroundColor = evt.newValue);
        _rightPanel.Add(useCustomBgToggle);

        var customBgField = new ColorField("Custom Background Color") { value = _settings.UI.CustomBackgroundColor };
        customBgField.RegisterValueChangedCallback(evt => _settings.UI.CustomBackgroundColor = evt.newValue);
        customBgField.SetEnabled(_settings.UI.UseCustomBackgroundColor);

        useCustomBgToggle.RegisterValueChangedCallback(evt => customBgField.SetEnabled(evt.newValue));
        _rightPanel.Add(customBgField);
    }

    private void DrawAudioSettings()
    {
        _rightPanel.Add(new Label("Audio Settings") { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });

        var masterVolumeField = new Slider("Master Volume", 0f, 1f) { value = _settings.General.DefaultMessageDelay != null ? float.Parse(_settings.General.DefaultMessageDelay) : 0.5f };
        masterVolumeField.RegisterValueChangedCallback(evt => _settings.General.DefaultMessageDelay = evt.newValue.ToString());
        _rightPanel.Add(masterVolumeField);

        var muteOnPauseToggle = new Toggle("Mute On Pause") { value = true };
        _rightPanel.Add(muteOnPauseToggle);

        var audioMixerField = new ObjectField("Audio Mixer") { objectType = typeof(UnityEngine.Audio.AudioMixer) };
        _rightPanel.Add(audioMixerField);
    }

    private void SaveSettings()
    {
        EditorUtility.SetDirty(_settings);
        AssetDatabase.SaveAssets();

        DialogueGraphView.UpdateGraphBackgroundForAllInstances();
    }
}