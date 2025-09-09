using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

public class SpeechNodeEditor : BaseNodeEditor
{
    private SpeechNode speechNode;
    private TextField dialogueField;
    private ObjectField audioField;

    public SpeechNodeEditor(SpeechNode node, DialogueGraphView graphView) : base(node, graphView)
    {
        this.speechNode = node;
    }

    protected override void CreateGUI()
    {
        base.CreateGUI();

        dialogueField = new TextField("Dialogue Text") { value = speechNode.DialogueText };
        dialogueField.multiline = true;
        visualElement.Add(dialogueField);

        audioField = new ObjectField("Audio Clip") { objectType = typeof(AudioClip), value = speechNode.AudioClip };
        visualElement.Add(audioField);

        AddPortsSection();
    }

    private void AddPortsSection()
    {
        var portsSection = new Foldout() { text = "Ports" };
        foreach (var port in speechNode.inputContainer.Children())
            if (port is Port inputPort)
                AddPortInfo(portsSection, inputPort, "Input");

        foreach (var port in speechNode.outputContainer.Children())
            if (port is Port outputPort)
                AddPortInfo(portsSection, outputPort, "Output");

        visualElement.Add(portsSection);
    }

    private void AddPortInfo(Foldout foldout, Port port, string direction)
    {
        var connections = port.connections;
        var connectionInfo = new Label($"{direction} Port: {connections.Count()} connections");
        foldout.Add(connectionInfo);
    }

    public override void ApplyChanges()
    {
        speechNode.DialogueText = dialogueField.value;
        speechNode.AudioClip = audioField.value as AudioClip;
        speechNode.title = speechNode.DialogueText.Length > 15 ?
            speechNode.DialogueText.Substring(0, 15) + "..." : speechNode.DialogueText;
    }

    public override object CopyProperties() => new
    {
        DialogueText = dialogueField.value,
        AudioClip = audioField.value
    };

    public override void PasteProperties(object properties)
    {
        if (properties is dynamic props)
        {
            dialogueField.value = props.DialogueText;
            audioField.value = props.AudioClip;
        }
    }
}
