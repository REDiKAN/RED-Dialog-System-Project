using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.Search;
using UnityEngine;
using UnityEditor;

public class EndNode : BaseNode
{
    // Временное поле для drag-and-drop в редакторе
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

        // ObjectField для drag-and-drop
        nextDialogueField = new ObjectField("Next Dialogue")
        {
            objectType = typeof(DialogueContainer)
            // allowSceneObjects недоступен в UI Toolkit — убираем
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
    /// Получает путь относительно папки Resources (без .asset), или пустую строку, если не в Resources.
    /// </summary>
    public string GetNextDialoguePath()
    {
        if (NextDialogueAsset == null)
            return string.Empty;

        string assetPath = AssetDatabase.GetAssetPath(NextDialogueAsset);
        if (string.IsNullOrEmpty(assetPath))
            return string.Empty;

        // Проверяем, лежит ли файл внутри Assets/Resources/
        if (!assetPath.StartsWith("Assets/Resources/"))
        {
            Debug.LogWarning($"Dialogue asset '{NextDialogueAsset.name}' is not in Resources folder. It will not be loadable at runtime via Resources.Load.", NextDialogueAsset);
            return string.Empty; // Возвращаем пустую строку, чтобы не ломать рантайм
        }

        // Убираем "Assets/Resources/" и ".asset"
        string relativePath = assetPath.Substring("Assets/Resources/".Length);
        if (relativePath.EndsWith(".asset"))
            relativePath = relativePath.Substring(0, relativePath.Length - 6);
        return relativePath;
    }

    /// <summary>
    /// Устанавливает ссылку на диалог по пути (используется при загрузке)
    /// </summary>
    public void SetNextDialogueFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            NextDialogueAsset = null;
            nextDialogueField?.SetValueWithoutNotify(null);
            return;
        }

        // Ищем в Resources
        var asset = Resources.Load<DialogueContainer>(path);
        if (asset != null)
        {
            NextDialogueAsset = asset;
            nextDialogueField?.SetValueWithoutNotify(asset);
            return;
        }

        // Если не найден — ищем через AssetDatabase (для редактора)
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