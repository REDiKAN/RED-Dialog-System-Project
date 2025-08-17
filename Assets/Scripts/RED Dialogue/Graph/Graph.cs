using UnityEditor.Experimental.GraphView;
using UnityEditor;

public class Graph : EditorWindow
{
    private GraphView graphView;
    private const string GUID = "";
    private string graphName;

    #region File Operations
    public void SaveGraph()
    {
        if (string.IsNullOrEmpty(graphName))
        {
            EditorUtility.DisplayDialog("Invalid filename!", "Please enter valid filename", "OK");
            return;
        }
        //GraphSaveUtility.GetInstance(graphView).SaveGraph(graphName);
    }

    public void LoadGraph()
    {
        if (string.IsNullOrEmpty(graphName))
        {
            EditorUtility.DisplayDialog("Invalid filename!", "Please enter valid filename", "OK");
            return;
        }
        //GraphSaveUtility.GetInstance(graphView).LoadGraph(graphName);
    }
    #endregion
}
