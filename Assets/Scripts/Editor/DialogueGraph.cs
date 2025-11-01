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

        rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
    }

    private void OnKeyDownEvent(KeyDownEvent evt)
    {
        // Проверяем комбинацию Ctrl + S (только Windows)
        if (!evt.ctrlKey || evt.keyCode != KeyCode.S)
            return;

        // Проверяем, что окно редактора активно
        if (EditorWindow.focusedWindow != this)
            return;

        evt.StopPropagation(); // Предотвращаем системное сохранение сцены

        var assetField = rootVisualElement.Q<ObjectField>("Dialogue File");
        var container = assetField?.value as DialogueContainer;

        // Случай: нет привязанного файла
        if (container == null)
        {
            // Проверяем, есть ли хоть какой-то контент помимо EntryNode
            bool hasContent = graphView?.nodes != null &&
                              graphView.nodes.OfType<BaseNode>().Any(n => !n.EntryPoint);

            if (!hasContent)
            {
                EditorUtility.DisplayDialog("No File", "No dialogue file is currently loaded. Please create or load one first.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Dialogue As",
                "NewDialogue",
                "asset",
                "Choose location and name for the dialogue file"
            );

            if (string.IsNullOrEmpty(path))
                return;

            // Создаём новый контейнер
            var newContainer = ScriptableObject.CreateInstance<DialogueContainer>();
            var saveUtility = GraphSaveUtility.GetInstance(graphView);
            saveUtility.SaveGraphToExistingContainer(newContainer);

            // Сохраняем в AssetDatabase
            AssetDatabase.CreateAsset(newContainer, path);
            AssetDatabase.SaveAssets();

            // Обновляем ObjectField
            assetField.SetValueWithoutNotify(newContainer);

            EditorUtility.DisplayDialog("Saved", "Dialogue saved successfully!", "OK");
            return;
        }

        // Случай: файл уже задан — просто сохраняем
        SaveCurrentDialogue();
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
    /// Генерация тулбара с новыми элементами управления
    /// </summary>
    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        // Поле выбора существующего файла диалога
        var dialogueAssetField = new ObjectField("Dialogue File")
        {
            objectType = typeof(DialogueContainer),
            value = null
        };
        dialogueAssetField.name = "Dialogue File"; // важно для поиска через Q
        dialogueAssetField.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue is DialogueContainer container)
            {
                LoadDialogueFromFile(container);
            }
        });
        toolbar.Add(dialogueAssetField);

        // Кнопка создания нового диалога
        toolbar.Add(new Button(CreateNewDialogue) { text = "Create New..." });

        // Кнопка загрузки через проводник
        toolbar.Add(new Button(LoadDialogueFromFileBrowser) { text = "Load File..." });

        // Кнопка сохранения
        toolbar.Add(new Button(SaveCurrentDialogue) { text = "Save" });

        // Base Character field (в конце)
        var baseCharacterField = new ObjectField("Base Character")
        {
            objectType = typeof(CharacterData),
            value = AssetDatabaseHelper.LoadAssetFromGuid<CharacterData>(graphView.BaseCharacterGuid)
        };
        baseCharacterField.RegisterValueChangedCallback(evt =>
        {
            var character = evt.newValue as CharacterData;
            graphView.BaseCharacterGuid = AssetDatabaseHelper.GetAssetGuid(character);
            UpdateAllSpeechNodesSpeaker(character);
            graphView.MarkUnsavedChangeWithoutFile(); // ← добавлено
        });
        toolbar.Add(baseCharacterField);

        rootVisualElement.Add(toolbar);
    }

    /// <summary>
    /// Создаёт новый диалог и сохраняет его по указанному пользователем пути
    /// </summary>
    private void CreateNewDialogue()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Dialogue",
            "NewDialogue",
            "asset",
            "Choose location and name for the new dialogue file"
        );
        if (string.IsNullOrEmpty(path))
            return;

        // Создаём контейнер
        var newContainer = ScriptableObject.CreateInstance<DialogueContainer>();

        // Сохраняем текущее состояние графа в контейнер
        var saveUtility = GraphSaveUtility.GetInstance(graphView);
        // Временно сохраняем в контейнер без записи в AssetDatabase
        saveUtility.SaveGraphToExistingContainer(newContainer);

        // Сохраняем файл
        AssetDatabase.CreateAsset(newContainer, path);
        AssetDatabase.SaveAssets();

        // Обновляем ObjectField
        var assetField = rootVisualElement.Q<ObjectField>("Dialogue File");
        if (assetField != null)
            assetField.SetValueWithoutNotify(newContainer);

        // Загружаем (это сбросит флаги)
        LoadDialogueFromFile(newContainer);

        // Сбрасываем состояние "без файла"
        graphView._hasUnsavedChangesWithoutFile = false;
        graphView._unsavedChangesWarningShown = false;

        Debug.Log($"Created new dialogue: {path}");
    }

    /// <summary>
    /// Загружает диалог из выбранного в ObjectField контейнера
    /// </summary>
    private void LoadDialogueFromFile(DialogueContainer container)
    {
        if (container == null) return;

        var saveUtility = GraphSaveUtility.GetInstance(graphView);
        saveUtility.LoadGraphFromContainer(container);

        // Обновляем GUID базового персонажа в графе
        graphView.BaseCharacterGuid = container.BaseCharacterGuid;

        // Обновляем отображение Base Character в тулбаре
        var baseCharField = rootVisualElement.Q<ObjectField>("Base Character");
        if (baseCharField != null)
        {
            baseCharField.SetValueWithoutNotify(
                AssetDatabaseHelper.LoadAssetFromGuid<CharacterData>(container.BaseCharacterGuid)
            );
        }
    }

    /// <summary>
    /// Открывает проводник для выбора существующего .asset файла диалога
    /// </summary>
    private void LoadDialogueFromFileBrowser()
    {
        string path = EditorUtility.OpenFilePanel(
            "Load Dialogue File",
            Application.dataPath,
            "asset"
        );

        if (string.IsNullOrEmpty(path)) return;

        // Преобразуем абсолютный путь в относительный от Assets
        if (!path.StartsWith(Application.dataPath))
        {
            EditorUtility.DisplayDialog("Invalid Path", "Please select a file inside the Assets folder.", "OK");
            return;
        }

        string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
        var container = AssetDatabase.LoadAssetAtPath<DialogueContainer>(relativePath);

        if (container == null)
        {
            EditorUtility.DisplayDialog("Invalid File", "Selected file is not a valid DialogueContainer.", "OK");
            return;
        }

        // Обновляем ObjectField
        var assetField = rootVisualElement.Q<ObjectField>("Dialogue File");
        if (assetField != null)
            assetField.SetValueWithoutNotify(container);

        LoadDialogueFromFile(container);

        graphView._hasUnsavedChangesWithoutFile = false;
        graphView._unsavedChangesWarningShown = false;
    }

    /// <summary>
    /// Сохраняет текущий граф в уже загруженный/созданный файл
    /// </summary>
    private void SaveCurrentDialogue()
    {
        var container = GetCurrentLoadedContainer();
        if (container == null)
        {
            EditorUtility.DisplayDialog("No File", "No dialogue file is currently loaded...", "OK");
            return;
        }
        var saveUtility = GraphSaveUtility.GetInstance(graphView);
        saveUtility.SaveGraphToExistingContainer(container);

        // === НОВОЕ: обновляем фон после сохранения настроек ===
        DialogueGraphView.UpdateGraphBackgroundForAllInstances();

        EditorUtility.DisplayDialog("Saved", "Dialogue saved successfully!", "OK");
    }

    /// <summary>
    /// Вспомогательный метод: получает текущий загруженный контейнер по пути
    /// </summary> 
    private DialogueContainer GetCurrentLoadedContainer()
    {
        var assetField = rootVisualElement.Q<ObjectField>("Dialogue File");
        return assetField?.value as DialogueContainer;
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
        if (character == null) return;

        foreach (var node in graphView.nodes.ToList())
        {
            switch (node)
            {
                case SpeechNode speechNode when speechNode.Speaker == null:
                    speechNode.SetSpeaker(character);
                    break;
                case SpeechNodeImage speechImageNode when speechImageNode.Speaker == null:
                    speechImageNode.SetSpeaker(character);
                    break;
                case SpeechNodeRandText speechRandNode when speechRandNode.Speaker == null:
                    speechRandNode.SetSpeaker(character);
                    break;
            }
        }
    }


}