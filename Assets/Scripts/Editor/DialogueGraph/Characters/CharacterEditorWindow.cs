using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CharacterEditorWindow : EditorWindow
{
    private List<CharacterData> characters = new List<CharacterData>();
    private CharacterData selectedCharacter;
    private Vector2 scrollPosition;
    private string searchString = "";
    // Добавляем переменные для прокрутки
    private Vector2 characterListScrollPosition;
    private Vector2 variablesScrollPosition;
    private int variableToRemove = -1;

    // === ДОБАВЛЕННЫЕ ПОЛЯ ДЛЯ АВТОСОХРАНЕНИЯ ===
    private float _lastChangeTime;
    private const float DEBOUNCE_DELAY = 0.5f;
    private CharacterData _lastSavedCharacterState; // Для отслеживания предыдущего состояния

    [MenuItem("Dialog System/Character Editor")]
    public static void ShowWindow()
    {
        GetWindow<CharacterEditorWindow>("Character Editor");
    }

    private void OnEnable()
    {
        RefreshCharacterList();
        // Инициализация времени последнего изменения
        _lastChangeTime = Time.realtimeSinceStartup;
    }

    // === МЕТОД UPDATE ДЛЯ ПРОВЕРКИ АВТОСОХРАНЕНИЯ ===
    private void Update()
    {
        // Только если окно в фокусе
        if (EditorWindow.focusedWindow == this)
        {
            AutoSaveCharacter();
        }
    }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.BeginHorizontal();
        DrawCharacterList();
        DrawCharacterDetails();
        EditorGUILayout.EndHorizontal();
        // Обрабатываем удаление переменных после отрисовки GUI
        if (variableToRemove != -1)
        {
            selectedCharacter.RemoveVariable(variableToRemove);
            _lastChangeTime = Time.realtimeSinceStartup; // Обновляем время изменения
            variableToRemove = -1;
            GUIUtility.ExitGUI(); // Выходим из текущего GUI цикла
        }
    }

    // === МЕТОД АВТОСОХРАНЕНИЯ ===
    private void AutoSaveCharacter()
    {
        if (selectedCharacter == null || _lastSavedCharacterState == null)
            return;

        // Проверяем, прошло ли достаточно времени с последнего изменения
        if (Time.realtimeSinceStartup - _lastChangeTime < DEBOUNCE_DELAY)
            return;

        // Проверяем, есть ли реальные изменения
        if (HasCharacterChanged(selectedCharacter))
        {
            EditorUtility.SetDirty(selectedCharacter);
            AssetDatabase.SaveAssets();

            // Создаем копию текущего состояния для последующего сравнения
            _lastSavedCharacterState = Instantiate(selectedCharacter);
            Debug.Log($"Character {selectedCharacter.name} auto-saved");
        }
    }

    // === ПРОВЕРКА ИЗМЕНЕНИЙ ===
    private bool HasCharacterChanged(CharacterData currentCharacter)
    {
        if (_lastSavedCharacterState == null)
            return true;

        // Сравниваем основные поля
        if (_lastSavedCharacterState.FirstName != currentCharacter.FirstName ||
            _lastSavedCharacterState.LastName != currentCharacter.LastName ||
            _lastSavedCharacterState.Icon != currentCharacter.Icon ||
            _lastSavedCharacterState.Description != currentCharacter.Description ||
            _lastSavedCharacterState.NameColor != currentCharacter.NameColor ||
            _lastSavedCharacterState.SpeechTextMessagePrefab != currentCharacter.SpeechTextMessagePrefab ||
            _lastSavedCharacterState.SpeechImageMessagePrefab != currentCharacter.SpeechImageMessagePrefab ||
            _lastSavedCharacterState.SpeechAudioMessagePrefab != currentCharacter.SpeechAudioMessagePrefab)
        {
            return true;
        }

        // Сравниваем переменные
        if (_lastSavedCharacterState.Variables.Count != currentCharacter.Variables.Count)
            return true;

        for (int i = 0; i < _lastSavedCharacterState.Variables.Count; i++)
        {
            if (_lastSavedCharacterState.Variables[i].VariableName != currentCharacter.Variables[i].VariableName ||
                _lastSavedCharacterState.Variables[i].Value != currentCharacter.Variables[i].Value)
            {
                return true;
            }
        }

        return false;
    }

    // === ОБНОВЛЕННЫЙ МЕТОД ПАНЕЛИ ИНСТРУМЕНТОВ ===
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Create New", EditorStyles.toolbarButton))
        {
            // Сбрасываем фокус перед созданием нового персонажа
            EditorGUI.FocusTextInControl(null);
            EditorGUIUtility.editingTextField = false;
            CreateNewCharacter();
        }
        if (GUILayout.Button("Save", EditorStyles.toolbarButton) && selectedCharacter != null)
        {
            SaveCharacter(); // Ручное сохранение по-прежнему работает
        }
        if (GUILayout.Button("Delete", EditorStyles.toolbarButton) && selectedCharacter != null)
        {
            // Сбрасываем фокус перед удалением персонажа
            EditorGUI.FocusTextInControl(null);
            EditorGUIUtility.editingTextField = false;
            DeleteCharacter();
        }
        GUILayout.FlexibleSpace();

        string newSearchString = EditorGUILayout.TextField(searchString, EditorStyles.toolbarSearchField, GUILayout.Width(200));
        // Если строка поиска изменилась, сбрасываем фокус и обновляем выделение
        if (newSearchString != searchString)
        {
            EditorGUI.FocusTextInControl(null);
            EditorGUIUtility.editingTextField = false;
            searchString = newSearchString;

            // Если строка поиска очищена или изменена, сбрасываем выделение
            if (string.IsNullOrEmpty(searchString) || selectedCharacter != null)
            {
                var filteredCharacters = string.IsNullOrEmpty(searchString)
                    ? characters
                    : characters.Where(c => c.name.ToLower().Contains(searchString.ToLower())).ToList();

                // Если текущий выбранный персонаж не входит в отфильтрованный список, сбрасываем выделение
                if (selectedCharacter != null && !filteredCharacters.Contains(selectedCharacter))
                {
                    selectedCharacter = null;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCharacterList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        EditorGUILayout.LabelField("Characters", EditorStyles.boldLabel);

        // Сбрасываем выделение, если список персонажей изменяется
        if (characters.Count == 0 || (searchString != "" && !characters.Any(c => c.name.ToLower().Contains(searchString.ToLower()))))
        {
            // Сбрасываем выделение, если нет персонажей или нет совпадений с фильтром
            if (selectedCharacter != null)
            {
                selectedCharacter = null;
                // Явно сбрасываем фокус со всех текстовых полей
                EditorGUIUtility.editingTextField = false;
            }
        }

        // Добавляем прокрутку для списка персонажей
        characterListScrollPosition = EditorGUILayout.BeginScrollView(characterListScrollPosition, GUILayout.ExpandHeight(true));
        var filteredCharacters = string.IsNullOrEmpty(searchString)
            ? characters
            : characters.Where(c => c.name.ToLower().Contains(searchString.ToLower())).ToList();

        foreach (var character in filteredCharacters)
        {
            var isSelected = selectedCharacter == character;
            // При клике на персонажа сбрасываем фокус с текстовых полей перед сменой выделения
            if (GUILayout.Toggle(isSelected, character.name, EditorStyles.toolbarButton) && !isSelected)
            {
                // Явно сбрасываем фокус со всех текстовых полей
                EditorGUI.FocusTextInControl(null);
                EditorGUIUtility.editingTextField = false;

                selectedCharacter = character;
                // Создаем копию текущего состояния для последующего сравнения
                if (selectedCharacter != null)
                {
                    _lastSavedCharacterState = Instantiate(selectedCharacter);
                }
                _lastChangeTime = Time.realtimeSinceStartup; // Сбрасываем таймер при смене персонажа
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawCharacterDetails()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        if (selectedCharacter == null)
        {
            EditorGUILayout.HelpBox("Select a character or create a new one", UnityEditor.MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUI.BeginChangeCheck();

        // Basic Info
        EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
        // Явно указываем ID контролов для возможности сброса фокуса
        string firstNameControlId = "FirstName_" + selectedCharacter.GetInstanceID();
        string lastNameControlId = "LastName_" + selectedCharacter.GetInstanceID();
        string descriptionControlId = "Description_" + selectedCharacter.GetInstanceID();

        EditorGUI.BeginChangeCheck();
        selectedCharacter.FirstName = EditorGUILayout.TextField("First Name", selectedCharacter.FirstName);
        if (EditorGUI.EndChangeCheck())
        {
            _lastChangeTime = Time.realtimeSinceStartup;
        }

        EditorGUI.BeginChangeCheck();
        selectedCharacter.LastName = EditorGUILayout.TextField("Last Name", selectedCharacter.LastName);
        if (EditorGUI.EndChangeCheck())
        {
            _lastChangeTime = Time.realtimeSinceStartup;
        }

        selectedCharacter.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", selectedCharacter.Icon, typeof(Sprite), false);

        EditorGUI.BeginChangeCheck();
        selectedCharacter.Description = EditorGUILayout.TextArea(selectedCharacter.Description, GUILayout.Height(60));
        if (EditorGUI.EndChangeCheck())
        {
            _lastChangeTime = Time.realtimeSinceStartup;
        }

        selectedCharacter.NameColor = EditorGUILayout.ColorField("Name Color", selectedCharacter.NameColor);
        EditorGUILayout.Space();

        // Message Prefabs
        EditorGUILayout.LabelField("Message Prefabs", EditorStyles.boldLabel);
        DrawPrefabFieldWithValidation(
            "Speech Text Message",
            ref selectedCharacter.SpeechTextMessagePrefab,
            typeof(SpeechTextMessage)
        );
        DrawPrefabFieldWithValidation(
            "Speech Image Message",
            ref selectedCharacter.SpeechImageMessagePrefab,
            typeof(SpeechImageMessage)
        );
        DrawPrefabFieldWithValidation(
            "Speech Audio Message",
            ref selectedCharacter.SpeechAudioMessagePrefab,
            typeof(SpeechAudioMessage)
        );
        EditorGUILayout.Space();

        // Variables
        EditorGUILayout.LabelField("Variables", EditorStyles.boldLabel);
        variablesScrollPosition = EditorGUILayout.BeginScrollView(variablesScrollPosition, GUILayout.Height(200));
        for (int i = 0; i < selectedCharacter.Variables.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            selectedCharacter.Variables[i].VariableName = EditorGUILayout.TextField(
                GUIContent.none,
                selectedCharacter.Variables[i].VariableName);
            selectedCharacter.Variables[i].Value = EditorGUILayout.IntField(
                GUIContent.none,
                selectedCharacter.Variables[i].Value);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                variableToRemove = i;
                _lastChangeTime = Time.realtimeSinceStartup; // Обновляем время при удалении переменной
            }
            EditorGUILayout.EndHorizontal();
            // Обновляем время после изменения любой переменной
            if (GUI.changed)
                _lastChangeTime = Time.realtimeSinceStartup;
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add Variable"))
        {
            selectedCharacter.AddVariable();
            _lastChangeTime = Time.realtimeSinceStartup; // Обновляем время при добавлении переменной
        }

        // Обработка изменений в любом поле
        if (EditorGUI.EndChangeCheck())
        {
            _lastChangeTime = Time.realtimeSinceStartup;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPrefabFieldWithValidation(string label, ref GameObject prefab, System.Type expectedComponentType)
    {
        prefab = (GameObject)EditorGUILayout.ObjectField(label, prefab, typeof(GameObject), false);
        if (prefab == null)
        {
            EditorGUILayout.HelpBox("⚠️ Not assigned", MessageType.Warning);
        }
        else
        {
            // Проверяем, есть ли нужный компонент, реализующий IMessageObject
            var component = prefab.GetComponent(expectedComponentType);
            if (component != null && component is IMessageObject)
            {
                EditorGUILayout.HelpBox("✅ Valid: implements IMessageObject", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("❌ Invalid: missing required component or it doesn't implement IMessageObject", MessageType.Error);
            }
        }
        // Обновляем время после изменения префаба
        if (GUI.changed)
            _lastChangeTime = Time.realtimeSinceStartup;
    }

    private void CreateNewCharacter()
    {
        var newCharacter = CreateInstance<CharacterData>();
        newCharacter.name = "NewCharacter";
        var path = EditorUtility.SaveFilePanelInProject(
            "Create Character",
            "NewCharacter",
            "asset",
            "Please enter a file name to save the character");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(newCharacter, path);
            AssetDatabase.SaveAssets();
            RefreshCharacterList();
            selectedCharacter = newCharacter;
            // Инициализируем состояние для автосохранения
            _lastSavedCharacterState = Instantiate(selectedCharacter);
            _lastChangeTime = Time.realtimeSinceStartup;
        }
    }

    private void SaveCharacter()
    {
        if (selectedCharacter == null) return;

        EditorUtility.SetDirty(selectedCharacter);
        AssetDatabase.SaveAssets();

        // Обновляем состояние последнего сохраненного персонажа
        _lastSavedCharacterState = Instantiate(selectedCharacter);
        Debug.Log($"Character {selectedCharacter.name} saved successfully");
    }

    private void DeleteCharacter()
    {
        if (EditorUtility.DisplayDialog(
            "Delete Character",
            $"Are you sure you want to delete {selectedCharacter.name}?",
            "Delete",
            "Cancel"))
        {
            var path = AssetDatabase.GetAssetPath(selectedCharacter);
            AssetDatabase.DeleteAsset(path);
            RefreshCharacterList();
            selectedCharacter = null;
            _lastSavedCharacterState = null; // Очищаем состояние
        }
    }

    private void RefreshCharacterList()
    {
        characters.Clear();
        var guids = AssetDatabase.FindAssets("t:CharacterData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (character != null)
                characters.Add(character);
        }
    }
}