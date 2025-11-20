using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
public class DialogueSettingsWindow : EditorWindow
{
    private DialogueSettingsData _settings;
    private VisualElement _root;
    private VisualElement _rightPanel;
    private string[] _categories = { "General", "UI", "File Management", "Audio" };
    private int _selectedCategoryIndex = 0;
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
        }
    }

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
        //    
        var hotkeyToggle = new Toggle("Enable Undo/Redo Hotkeys (Ctrl+Z/Y)") { value = _settings.General.EnableHotkeyUndoRedo };
        hotkeyToggle.RegisterValueChangedCallback(evt => _settings.General.EnableHotkeyUndoRedo = evt.newValue);
        _rightPanel.Add(hotkeyToggle);

        // AUTO-SAVE: Добавляем toggle для новой настройки
        var autoSaveToggle = new Toggle("Auto-save on Unity close") { value = _settings.General.AutoSaveOnUnityClose };
        autoSaveToggle.RegisterValueChangedCallback(evt => _settings.General.AutoSaveOnUnityClose = evt.newValue);
        _rightPanel.Add(autoSaveToggle);
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
    private void SaveSettings()
    {
        EditorUtility.SetDirty(_settings);
        AssetDatabase.SaveAssets();
        //      
        DialogueGraphView.UpdateGraphBackgroundForAllInstances();
    }
}