using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно редактора диалоговых графов
/// </summary>
public class DialogueGraph : EditorWindow
{
    private DialogueGraphView graphView;
    private string fileName = "New Narrative";

    #region Window Management
    [MenuItem("Dialog System/Open Graph Editor")]
    public static void OpenDialogueGraphWindow()
    {
        var window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue Graph");
    }
    #endregion

    #region Graph Initialization
    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
        GenerateMinimap();
        GenerateBlackBoard();
    }

    private void ConstructGraphView()
    {
        graphView = new DialogueGraphView(this) { name = "Dialogue Graph" };
        graphView.StretchToParentSize();
        rootVisualElement.Add(graphView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        var fileNameField = new TextField("File Name:") { value = fileName };
        fileNameField.RegisterValueChangedCallback(evt => fileName = evt.newValue);
        toolbar.Add(fileNameField);

        toolbar.Add(new Button(() => SaveGraph()) { text = "Save" });
        toolbar.Add(new Button(() => LoadGraph()) { text = "Load" });

        rootVisualElement.Add(toolbar);
    }

    private void GenerateMinimap()
    {
        var minimap = new MiniMap { anchored = true };
        minimap.SetPosition(new Rect(10, 30, 200, 140));
        graphView.Add(minimap);
    }

    private void GenerateBlackBoard()
    {
        var blackboard = new Blackboard(graphView);
        blackboard.Add(new BlackboardSection { title = "Exposed Properties" });

        blackboard.addItemRequested = _ =>
            graphView.AddPropertyToBlackBoard(new ExposedProperty());

        blackboard.editTextRequested = (_, element, newValue) =>
        {
            if (graphView.ExposedProperties.Any(x => x.PropertyName == newValue))
            {
                EditorUtility.DisplayDialog("Error", "Property name already exists!", "OK");
                return;
            }

            var oldName = ((BlackboardField)element).text;
            var propertyIndex = graphView.ExposedProperties.FindIndex(x => x.PropertyName == oldName);
            graphView.ExposedProperties[propertyIndex].PropertyName = newValue;
            ((BlackboardField)element).text = newValue;
        };

        blackboard.SetPosition(new Rect(10, 30, 200, 300));
        graphView.Add(blackboard);
        graphView.Blackboard = blackboard;
    }
    #endregion

    #region File Operations
    private void SaveGraph()
    {
        if (string.IsNullOrEmpty(fileName))
        {
            EditorUtility.DisplayDialog("Invalid filename!", "Please enter valid filename", "OK");
            return;
        }
        GraphSaveUtility.GetInstance(graphView).SaveGraph(fileName);
    }

    private void LoadGraph()
    {
        if (string.IsNullOrEmpty(fileName))
        {
            EditorUtility.DisplayDialog("Invalid filename!", "Please enter valid filename", "OK");
            return;
        }
        GraphSaveUtility.GetInstance(graphView).LoadGraph(fileName);
    }
    #endregion

    private void OnDisable() => rootVisualElement.Remove(graphView);
}
