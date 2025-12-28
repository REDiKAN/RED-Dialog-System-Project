// Assets/Scripts/Editor/DialogueGraph/Nodes/ChangeChatIconNode.cs
using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

public class ChangeChatIconNode : BaseNode
{
    public Sprite ChatIconSprite;
    public ObjectField spriteField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Change Chat Icon";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output port
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Sprite field
        spriteField = new ObjectField("Chat Icon") { objectType = typeof(Sprite) };
        spriteField.RegisterValueChangedCallback(evt =>
        {
            ChatIconSprite = evt.newValue as Sprite;
        });
        mainContainer.Add(spriteField);

        RefreshExpandedState();
        RefreshPorts();
    }

    [System.Serializable]
    private class ChangeChatIconNodeSerializedData
    {
        public string SpritePath;
    }

    public override string SerializeNodeData()
    {
        string spritePath = string.Empty;
        if (ChatIconSprite != null)
        {
            string path = AssetDatabase.GetAssetPath(ChatIconSprite);
            // Проверяем, находится ли спрайт в папке Resources
            if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/Resources/"))
            {
                spritePath = path.Substring("Assets/Resources/".Length);
                int dotIndex = spritePath.LastIndexOf('.');
                if (dotIndex > 0)
                {
                    spritePath = spritePath.Substring(0, dotIndex);
                }
            }
            else
            {
                Debug.LogWarning($"Sprite {ChatIconSprite.name} is not in a Resources folder. It must be placed in Assets/Resources to load at runtime.");
            }
        }

        var data = new ChangeChatIconNodeSerializedData
        {
            SpritePath = spritePath
        };

        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<ChangeChatIconNodeSerializedData>(jsonData);

        if (!string.IsNullOrEmpty(data.SpritePath))
        {
            // Сначала пробуем загрузить как спрайт
            string fullPath = $"Assets/Resources/{data.SpritePath}.sprite";
            ChatIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

            // Если не получилось, пробуем загрузить как обычный ассет
            if (ChatIconSprite == null)
            {
                fullPath = $"Assets/Resources/{data.SpritePath}";
                ChatIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
            }
        }

        // Обновляем UI
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (spriteField != null)
        {
            spriteField.SetValueWithoutNotify(ChatIconSprite);
        }
    }
}