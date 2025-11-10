// Assets/Scripts/Editor/DialogueSettingsWindow.cs
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueSettingsWindow : EditorWindow
{
    private DialogueSettingsData _settings;
    private VisualElement _root;
    private VisualElement _rightPanel;
    private string[] _categories = { "General", "UI", "Audio" };
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

        // Новая настройка горячих клавиш
        var hotkeyToggle = new Toggle("Enable Undo/Redo Hotkeys (Ctrl+Z/Y)") { value = _settings.General.EnableHotkeyUndoRedo };
        hotkeyToggle.RegisterValueChangedCallback(evt => _settings.General.EnableHotkeyUndoRedo = evt.newValue);
        _rightPanel.Add(hotkeyToggle);
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
        // Обновляем фон во всех открытых графах
        DialogueGraphView.UpdateGraphBackgroundForAllInstances();
    }
}