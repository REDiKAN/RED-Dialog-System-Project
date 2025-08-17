using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

public class PersonGraph : Graph
{
    private PersonGraphView graphView;
    private string fileName = "New Person";

    [MenuItem("Dialog System/Open Person Editor")]
    public static void OpenDialoguePersonWindow()
    {
        var window = GetWindow<PersonGraph>();
        window.titleContent = new GUIContent("Perosn Graph");
    }

    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
    }

    private void ConstructGraphView()
    {
        graphView = new PersonGraphView(this) { name = "Peroson Graph" };
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
}
