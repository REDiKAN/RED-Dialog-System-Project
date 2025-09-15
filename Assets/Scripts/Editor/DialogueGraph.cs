using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Главное окно редактора диалоговых графов
/// Содержит тулбар и область для редактирования графа
/// </summary>
public class DialogueGraph : EditorWindow
{
    public DialogueGraphView graphView;
    private string fileName = "New Narrative";

    /// <summary>
    /// Открывает окно редактора диалоговых графов
    /// </summary>
    [MenuItem("Dialog System/Open Graph Editor")]
    public static void OpenDialogueGraphWindow()
    {
        var window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue Graph");
    }

    /// <summary>
    /// Инициализация окна редактора
    /// </summary>
    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
    }

    /// <summary>
    /// Создает область для редактирования графа
    /// </summary>
    private void ConstructGraphView()
    {
        graphView = new DialogueGraphView(this)
        {
            name = "Dialogue Graph"
        };

        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
    }

    /// <summary>
    /// Создает тулбар с кнопками управления
    /// </summary>
    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        // File Name field
        var fileNameTextField = new TextField("File Name:");
        fileNameTextField.SetValueWithoutNotify(fileName);
        fileNameTextField.MarkDirtyRepaint();
        fileNameTextField.RegisterValueChangedCallback(evt => fileName = evt.newValue);
        toolbar.Add(fileNameTextField);

        // Base Character field
        var baseCharacterField = new ObjectField("Base Character")
        {
            objectType = typeof(CharacterData),
            value = AssetDatabaseHelper.LoadAssetFromGuid<CharacterData>(graphView.BaseCharacterGuid)
        };

        baseCharacterField.RegisterValueChangedCallback(evt =>
        {
            var character = evt.newValue as CharacterData;
            graphView.BaseCharacterGuid = AssetDatabaseHelper.GetAssetGuid(character);

            // Обновляем все существующие SpeechNode при изменении базового персонажа
            UpdateAllSpeechNodesSpeaker(character);
        });

        toolbar.Add(baseCharacterField);

        // Save/Load buttons
        toolbar.Add(new Button(() => RequestDataOperation(true)) { text = "Save Data" });
        toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "Load Data" });

        rootVisualElement.Add(toolbar);
    }

    /// <summary>
    /// Обрабатывает запрос на сохранение или загрузку данных
    /// </summary>
    private void RequestDataOperation(bool save)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            EditorUtility.DisplayDialog("Invalid file name!", "Please enter a valid file name.", "OK");
            return;
        }

        var saveUtility = GraphSaveUtility.GetInstance(graphView);
        if (save)
            saveUtility.SaveGraph(fileName);
        else
            saveUtility.LoadGraph(fileName);
    }

    /// <summary>
    /// Очищает ресурсы при закрытии окна
    /// </summary>
    private void OnDisable()
    {
        rootVisualElement.Remove(graphView);
    }

    private void UpdateAllSpeechNodesSpeaker(CharacterData character)
    {
        foreach (var node in graphView.nodes.ToList().OfType<SpeechNode>())
        {
            node.SetSpeaker(character);
        }
    }
}