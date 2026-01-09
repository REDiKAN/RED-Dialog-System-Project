using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;

public class ChatSwitchNode : BaseNode
{
    public int TargetChatIndex = 0;
    public IntegerField chatIndexField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Chat Switch";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output port
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        // Chat index field
        chatIndexField = new IntegerField("Target Chat Index") { value = TargetChatIndex };
        chatIndexField.RegisterValueChangedCallback(evt => TargetChatIndex = evt.newValue);
        mainContainer.Add(chatIndexField);

        RefreshExpandedState();
        RefreshPorts();
    }

    [System.Serializable]
    private class ChatSwitchNodeSerializedData
    {
        public int TargetChatIndex;
    }

    public override string SerializeNodeData()
    {
        var data = new ChatSwitchNodeSerializedData
        {
            TargetChatIndex = TargetChatIndex
        };
        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<ChatSwitchNodeSerializedData>(jsonData);
        TargetChatIndex = data.TargetChatIndex;
        // Безопасное обновление UI
        if (chatIndexField != null)
        {
            chatIndexField.SetValueWithoutNotify(TargetChatIndex);
        }
        else
        {
            // Если UI еще не создан, сохраняем значение для последующего обновления
            // Это может быть полезно при загрузке узла
        }
    }
}