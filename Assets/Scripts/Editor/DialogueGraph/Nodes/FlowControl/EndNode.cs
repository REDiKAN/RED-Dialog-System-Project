using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEditor.Search;

public class EndNode : BaseNode
{
    public DialogueContainer NextDialogueAsset { get; set; }
    public bool ShouldEndDialogue = true;

    private ObjectField nextDialogueField;
    private Toggle endDialogueToggle;
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "End Node";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Toggle для управления завершением диалога
        endDialogueToggle = new Toggle("End Dialogue")
        {
            value = ShouldEndDialogue,
            tooltip = "When enabled, the dialogue will end and a message will be printed to console"
        };
        endDialogueToggle.RegisterValueChangedCallback(evt =>
        {
            ShouldEndDialogue = evt.newValue;
        });
        mainContainer.Add(endDialogueToggle);

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

    [System.Serializable]
    private class EndNodeSerializedData
    {
        public string NextDialoguePath;
        public bool ShouldEndDialogue;
    }

    public override string SerializeNodeData()
    {
        var data = new EndNodeSerializedData
        {
            NextDialoguePath = GetNextDialoguePath(),
            ShouldEndDialogue = ShouldEndDialogue
        };
        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<EndNodeSerializedData>(jsonData);
        SetNextDialogueFromPath(data.NextDialoguePath);
        ShouldEndDialogue = data.ShouldEndDialogue;
        // Обновление UI
        if (endDialogueToggle != null)
        {
            endDialogueToggle.SetValueWithoutNotify(ShouldEndDialogue);
        }
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