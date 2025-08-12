using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DialogyeGraph : EditorWindow
{
    private DialogueGrapView grapView;
    private string fileName = "New Narretive";

    [MenuItem("Graph/Dialogue Graph")]
    public static void OpenDialogueGrapWindow()
    {
        var window = GetWindow<DialogyeGraph>();
        window.titleContent = new GUIContent();
    }

    private void OnEnable()
    {
        ConstrucyGraphView();
        GenerateToolbar();
        GenerateMinimap();
        GenerateBlackBord();
    }

    private void GenerateBlackBord()
    {
        var blackboard = new Blackboard(grapView);
        blackboard.Add(new BlackboardSection { title = "Exposed Properties" });
        blackboard.addItemRequested = _blackboard => { grapView.AddproperToBlackBoard(new ExposedProperty()); };

        blackboard.editTextRequested = (blackboard1, element, newValue) =>
        {
            var oldPropertyName = ((BlackboardField)element).text;
            if (grapView.ExposedProperties.Any(x => x.PropertyName == newValue))
            {
                EditorUtility.DisplayDialog("Error", "This property name already exist, please chose another one !", "OK");

                return;
            }

            var propertyIndex = grapView.ExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
            grapView.ExposedProperties[propertyIndex].PropertyName = newValue;
            ((BlackboardField)element).text = newValue;
        };

        blackboard.SetPosition(new Rect(10, 30, 200, 300));
        grapView.Add(blackboard);
        grapView.Blackboard = blackboard;
    }

    private void ConstrucyGraphView()
    {
        grapView = new DialogueGrapView(this)
        { name = "Dialogue Graph" };

        grapView.StretchToParentSize();
        rootVisualElement.Add(grapView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        var fileNameTextField = new TextField("File Name:");
        fileNameTextField.SetValueWithoutNotify(fileName);
        fileNameTextField.MarkDirtyRepaint();
        fileNameTextField.RegisterValueChangedCallback(evt => fileName = evt.newValue);

        toolbar.Add(fileNameTextField);
        toolbar.Add(new Button(() => RequestDataOperation(true)) { text = "Save Data" });
        toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "Load Data"});

        rootVisualElement.Add(toolbar);
    }
    private void GenerateMinimap()
    {
        var miniMap = new MiniMap(/*anchored = true*/);
        var cords = grapView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x - 10, 30));
        miniMap.SetPosition(new Rect(10, 30, 200, 140));
        grapView.Add(miniMap);
    }

    private void RequestDataOperation(bool save)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            EditorUtility.DisplayDialog("invalid file name !", "PLease enter a valid file name", "OK");
            return;
        }

        var saveUnility = GraphSaveUnility.GetInstance(grapView);

        if (save)
            saveUnility.SaveGraph(fileName);
        else
            saveUnility.LoadGraph(fileName);
    }
    private void OnDisable()
    {
        rootVisualElement.Remove(grapView);
    }

}
