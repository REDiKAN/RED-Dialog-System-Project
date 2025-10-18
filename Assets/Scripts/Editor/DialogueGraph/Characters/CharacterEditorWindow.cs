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

    [MenuItem("Dialog System/Character Editor")]
    public static void ShowWindow()
    {
        GetWindow<CharacterEditorWindow>("Character Editor");
    }

    private void OnEnable()
    {
        RefreshCharacterList();
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
            variableToRemove = -1;
            GUIUtility.ExitGUI(); // Выходим из текущего GUI цикла
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Create New", EditorStyles.toolbarButton))
        {
            CreateNewCharacter();
        }

        if (GUILayout.Button("Save", EditorStyles.toolbarButton) && selectedCharacter != null)
        {
            SaveCharacter();
        }

        if (GUILayout.Button("Delete", EditorStyles.toolbarButton) && selectedCharacter != null)
        {
            DeleteCharacter();
        }

        GUILayout.FlexibleSpace();

        searchString = EditorGUILayout.TextField(searchString, EditorStyles.toolbarSearchField, GUILayout.Width(200));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCharacterList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        EditorGUILayout.LabelField("Characters", EditorStyles.boldLabel);

        // Добавляем прокрутку для списка персонажей
        characterListScrollPosition = EditorGUILayout.BeginScrollView(characterListScrollPosition, GUILayout.ExpandHeight(true));

        var filteredCharacters = string.IsNullOrEmpty(searchString)
            ? characters
            : characters.Where(c => c.name.ToLower().Contains(searchString.ToLower())).ToList();

        foreach (var character in filteredCharacters)
        {
            var isSelected = selectedCharacter == character;
            if (GUILayout.Toggle(isSelected, character.name, EditorStyles.toolbarButton) && !isSelected)
            {
                selectedCharacter = character;
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
        selectedCharacter.FirstName = EditorGUILayout.TextField("First Name", selectedCharacter.FirstName);
        selectedCharacter.LastName = EditorGUILayout.TextField("Last Name", selectedCharacter.LastName);
        selectedCharacter.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", selectedCharacter.Icon, typeof(Sprite), false);
        selectedCharacter.Description = EditorGUILayout.TextArea(selectedCharacter.Description, GUILayout.Height(60));
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
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add Variable"))
        {
            selectedCharacter.AddVariable();
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(selectedCharacter);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawValidationMessage(Object prefab, System.Type expectedType)
    {
        if (prefab == null)
        {
            EditorGUILayout.HelpBox("⚠️ Not assigned", MessageType.Warning);
        }
        else if (prefab is GameObject go && go.GetComponent(expectedType) != null)
        {
            EditorGUILayout.HelpBox("✅ Valid: implements IMessageObject", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("❌ Invalid: does not implement IMessageObject", MessageType.Error);
        }
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
        }
    }

    private void SaveCharacter()
    {
        EditorUtility.SetDirty(selectedCharacter);
        AssetDatabase.SaveAssets();
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