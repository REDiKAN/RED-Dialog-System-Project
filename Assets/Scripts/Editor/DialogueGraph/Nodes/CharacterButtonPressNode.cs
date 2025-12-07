// File: Assets/Scripts/Editor/DialogueGraph/Nodes/CharacterButtonPressNode.cs
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using UnityEditor.UIElements;

public class CharacterButtonPressNode : BaseNode
{
    public CharacterData CharacterAsset;
    public bool RequireButtonPress = false;

    public ObjectField characterField;
    public Toggle buttonPressToggle;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Character Button Press";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output port
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        // Character field
        characterField = new ObjectField("Character")
        {
            objectType = typeof(CharacterData)
        };
        characterField.RegisterValueChangedCallback(evt =>
        {
            CharacterAsset = evt.newValue as CharacterData;
        });
        mainContainer.Add(characterField);

        // Toggle for RequireButtonPress
        buttonPressToggle = new Toggle("Require Button Press")
        {
            value = RequireButtonPress
        };
        buttonPressToggle.RegisterValueChangedCallback(evt =>
        {
            RequireButtonPress = evt.newValue;
        });
        mainContainer.Add(buttonPressToggle);

        RefreshExpandedState();
        RefreshPorts();
        capabilities |= Capabilities.Deletable;
    }

    [System.Serializable]
    private class CharacterButtonPressNodeSerializedData
    {
        public string CharacterAssetGuid;
        public bool RequireButtonPress;
    }

    public override string SerializeNodeData()
    {
        string characterGuid = string.Empty;
        if (CharacterAsset != null)
        {
            characterGuid = AssetDatabaseHelper.GetAssetGuid(CharacterAsset);
        }

        var data = new CharacterButtonPressNodeSerializedData
        {
            CharacterAssetGuid = characterGuid,
            RequireButtonPress = RequireButtonPress
        };

        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<CharacterButtonPressNodeSerializedData>(jsonData);

        // Load character from GUID
        if (!string.IsNullOrEmpty(data.CharacterAssetGuid))
        {
            CharacterAsset = AssetDatabaseHelper.LoadAssetFromGuid<CharacterData>(data.CharacterAssetGuid);
        }

        RequireButtonPress = data.RequireButtonPress;

        // Update UI
        if (characterField != null)
        {
            characterField.SetValueWithoutNotify(CharacterAsset);
        }

        if (buttonPressToggle != null)
        {
            buttonPressToggle.SetValueWithoutNotify(RequireButtonPress);
        }
    }
}