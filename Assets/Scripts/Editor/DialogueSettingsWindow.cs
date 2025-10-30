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
            case 2:
                DrawAudioSettings();
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

        var historyField = new IntegerField("Max Message History") { value = _settings.General.MaxMessageHistory };
        historyField.RegisterValueChangedCallback(evt => _settings.General.MaxMessageHistory = evt.newValue);
        _rightPanel.Add(historyField);
    }

    private void DrawUISettings()
    {
        _rightPanel.Add(new Label("UI Settings") { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });

        var bgField = new ColorField("Background Color") { value = _settings.UI.BackgroundColor };
        bgField.RegisterValueChangedCallback(evt => _settings.UI.BackgroundColor = evt.newValue);
        _rightPanel.Add(bgField);

        var timestampsToggle = new Toggle("Show Timestamps") { value = _settings.UI.ShowTimestamps };
        timestampsToggle.RegisterValueChangedCallback(evt => _settings.UI.ShowTimestamps = evt.newValue);
        _rightPanel.Add(timestampsToggle);

        var fontField = new TextField("Font Name") { value = _settings.UI.FontName };
        fontField.RegisterValueChangedCallback(evt => _settings.UI.FontName = evt.newValue);
        _rightPanel.Add(fontField);
    }

    private void DrawAudioSettings()
    {
        _rightPanel.Add(new Label("Audio Settings") { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });

        var volumeSlider = new Slider("Master Volume", 0f, 1f) { value = _settings.Audio.MasterVolume };
        volumeSlider.RegisterValueChangedCallback(evt => _settings.Audio.MasterVolume = evt.newValue);
        _rightPanel.Add(volumeSlider);

        var muteToggle = new Toggle("Mute On Pause") { value = _settings.Audio.MuteOnPause };
        muteToggle.RegisterValueChangedCallback(evt => _settings.Audio.MuteOnPause = evt.newValue);
        _rightPanel.Add(muteToggle);

        var mixerField = new TextField("Audio Mixer Path") { value = _settings.Audio.AudioMixerPath };
        mixerField.RegisterValueChangedCallback(evt => _settings.Audio.AudioMixerPath = evt.newValue);
        _rightPanel.Add(mixerField);
    }

    private void SaveSettings()
    {
        EditorUtility.SetDirty(_settings);
        AssetDatabase.SaveAssets();
    }
}