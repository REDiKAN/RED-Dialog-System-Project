// Assets/Scripts/Editor/DialogueGraph/Nodes/SpeechNode.cs
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;

/// <summary>
/// Базовый узел речи NPC — позволяет выбирать спикера из выпадающего списка персонажей из Resources/Characters
/// </summary>
public class SpeechNode : BaseNode
{
    public string DialogueText { get; set; }
    public AudioClip AudioClip { get; set; }
    public CharacterData Speaker;

    protected TextField dialogueTextField;
    protected ObjectField audioField;
    public ObjectField speakerField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Speech Node";
        DialogueText = "New Dialogue";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output port
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Dialogue text
        dialogueTextField = new TextField("Dialogue Text:");
        dialogueTextField.multiline = true;
        dialogueTextField.RegisterValueChangedCallback(evt => DialogueText = evt.newValue);
        dialogueTextField.SetValueWithoutNotify(DialogueText);
        mainContainer.Add(dialogueTextField);

        // Audio clip
        audioField = new ObjectField("Audio Clip") { objectType = typeof(AudioClip) };
        audioField.RegisterValueChangedCallback(evt => AudioClip = evt.newValue as AudioClip);
        mainContainer.Add(audioField);

        // Speaker drag-and-drop field
        speakerField = new ObjectField("Speaker") { objectType = typeof(CharacterData) };
        speakerField.RegisterValueChangedCallback(evt =>
        {
            Speaker = evt.newValue as CharacterData;
        });
        mainContainer.Add(speakerField);

        RefreshExpandedState();
        RefreshPorts();
        styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));
    }

    public void SetSpeaker(CharacterData speaker)
    {
        Speaker = speaker;
        if (speakerField != null)
            speakerField.SetValueWithoutNotify(speaker);
    }

    public virtual void SetDialogueText(string text)
    {
        DialogueText = text;
        if (dialogueTextField != null)
            dialogueTextField.SetValueWithoutNotify(text);
    }

    public override string SerializeNodeData()
    {
        return null;
    }

    public override void DeserializeNodeData(string jsonData)
    {
        // десериализация данных из JSON в узел
    }
}