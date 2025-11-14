using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEditor.Search;

public class EndNode : BaseNode
{
    // Ссылка на следующий диалог для drag-and-drop
    public DialogueContainer NextDialogueAsset { get; set; }
    private ObjectField nextDialogueField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "End Node";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // ObjectField для drag-and-drop следующего диалога
        nextDialogueField = new ObjectField("Next Dialogue")
        {
            objectType = typeof(DialogueContainer)
        };
        nextDialogueField.RegisterValueChangedCallback(evt =>
        {
            NextDialogueAsset = evt.newValue as DialogueContainer;
        });
        mainContainer.Add(nextDialogueField);

        RefreshExpandedState();
        RefreshPorts();
        styleSheets.Add(Resources.Load<StyleSheet>("EndNode"));
    }

    /// <summary>
    /// Получает путь к следующему диалогу в Resources (для .asset), или возвращает пустую строку, если файл не в Resources.
    /// </summary>
    public string GetNextDialoguePath()
    {
        if (NextDialogueAsset == null)
            return string.Empty;

        string assetPath = AssetDatabase.GetAssetPath(NextDialogueAsset);
        if (string.IsNullOrEmpty(assetPath))
            return string.Empty;

        // Проверяем, что файл находится в папке Assets/Resources/
        if (!assetPath.StartsWith("Assets/Resources/"))
        {
            Debug.LogWarning($"Dialogue asset '{NextDialogueAsset.name}' is not in Resources folder. It will not be loadable at runtime via Resources.Load.", NextDialogueAsset);
            return string.Empty;
        }

        // Убираем "Assets/Resources/" и расширение ".asset"
        string relativePath = assetPath.Substring("Assets/Resources/".Length);
        if (relativePath.EndsWith(".asset"))
            relativePath = relativePath.Substring(0, relativePath.Length - 6);

        return relativePath;
    }

    /// <summary>
    /// Устанавливает следующий диалог по пути (восстанавливает ссылку при загрузке)
    /// </summary>
    public void SetNextDialogueFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            NextDialogueAsset = null;
            nextDialogueField?.SetValueWithoutNotify(null);
            return;
        }

        // Пытаемся загрузить из Resources
        var asset = Resources.Load<DialogueContainer>(path);
        if (asset != null)
        {
            NextDialogueAsset = asset;
            nextDialogueField?.SetValueWithoutNotify(asset);
            return;
        }

        // Пытаемся загрузить через AssetDatabase (для редактора)
        string fullPath = $"Assets/Resources/{path}.asset";
        var assetAtPath = AssetDatabase.LoadAssetAtPath<DialogueContainer>(fullPath);
        if (assetAtPath != null)
        {
            NextDialogueAsset = assetAtPath;
            nextDialogueField?.SetValueWithoutNotify(assetAtPath);
        }
        else
        {
            NextDialogueAsset = null;
            nextDialogueField?.SetValueWithoutNotify(null);
            Debug.LogWarning($"Could not restore DialogueContainer for path: {path}");
        }
    }
}