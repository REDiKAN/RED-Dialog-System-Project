using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ������� ���� ��������� ���������� ������
/// �������� ������ � ������� ��� �������������� �����
/// </summary>
public class DialogueGraph : EditorWindow
{
    public DialogueGraphView graphView;
    private string fileName = "New Narrative";

    /// <summary>
    /// ��������� ���� ��������� ���������� ������
    /// </summary>
    [MenuItem("Dialog System/Open Graph Editor")]
    public static void OpenDialogueGraphWindow()
    {
        var window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue Graph");
    }

    /// <summary>
    /// ������������� ���� ���������
    /// </summary>
    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
    }

    /// <summary>
    /// ������� ������� ��� �������������� �����
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
    /// ������� ������ � �������� ����������
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

            // ��������� ��� ������������ SpeechNode ��� ��������� �������� ���������
            UpdateAllSpeechNodesSpeaker(character);
        });

        toolbar.Add(baseCharacterField);

        // Save/Load buttons
        toolbar.Add(new Button(() => RequestDataOperation(true)) { text = "Save Data" });
        toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "Load Data" });

        rootVisualElement.Add(toolbar);
    }

    /// <summary>
    /// ������������ ������ �� ���������� ��� �������� ������
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
    /// ������� ������� ��� �������� ����
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