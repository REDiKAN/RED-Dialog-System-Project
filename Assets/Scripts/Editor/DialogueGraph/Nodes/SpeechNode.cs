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
    private DropdownField speakerDropdown;

    private List<string> _characterNames = new List<string>();
    private List<CharacterData> _characterList = new List<CharacterData>();

    /// <summary>
    /// Загружает всех персонажей из Resources/Characters
    /// </summary>
    private void LoadCharacters()
    {
        _characterList.Clear();
        _characterNames.Clear();

        var characters = Resources.LoadAll<CharacterData>("Characters");
        foreach (var character in characters)
        {
            if (character != null)
            {
                _characterList.Add(character);
                _characterNames.Add(character.name);
            }
        }
    }

    public override void Initialize(Vector2 position)
    {
        LoadCharacters(); // ← загружаем персонажей ДО создания UI

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

        // Speaker dropdown
        speakerDropdown = new DropdownField(_characterNames, 0) // выбирает первый элемент
        {
            label = "Speaker"
        };
        speakerDropdown.RegisterValueChangedCallback(evt =>
        {
            int index = _characterNames.IndexOf(evt.newValue);
            Speaker = index >= 0 ? _characterList[index] : null;
        });
        mainContainer.Add(speakerDropdown);

        RefreshExpandedState();
        RefreshPorts();
        styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));
    }

    public void SetSpeaker(CharacterData speaker)
    {
        Speaker = speaker;
        if (speakerDropdown != null)
        {
            speakerDropdown.value = Speaker != null ? Speaker.name : "";
        }
    }

    public virtual void SetDialogueText(string text)
    {
        DialogueText = text;
        if (dialogueTextField != null)
            dialogueTextField.SetValueWithoutNotify(text);
    }
}