// Assets/Scripts/Editor/DialogueGraph/Nodes/ChangeChatNameNode.cs
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using System;

public class ChangeChatNameNode : BaseNode
{
    public string NewChatName = "New Chat Name";
    public TextField nameField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Change Chat Name";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output port
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Name field
        nameField = new TextField("Chat Name") { value = NewChatName };
        nameField.RegisterValueChangedCallback(evt =>
        {
            NewChatName = evt.newValue;
        });
        mainContainer.Add(nameField);

        RefreshExpandedState();
        RefreshPorts();
    }

    [System.Serializable]
    private class ChangeChatNameNodeSerializedData
    {
        public string NewChatName;
    }

    public override string SerializeNodeData()
    {
        var data = new ChangeChatNameNodeSerializedData
        {
            NewChatName = string.IsNullOrEmpty(NewChatName) ? "New Chat Name" : NewChatName
        };

        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<ChangeChatNameNodeSerializedData>(jsonData);
        NewChatName = string.IsNullOrEmpty(data.NewChatName) ? "New Chat Name" : data.NewChatName;

        // Обновляем UI
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (nameField != null)
        {
            nameField.SetValueWithoutNotify(NewChatName);
        }
    }
}