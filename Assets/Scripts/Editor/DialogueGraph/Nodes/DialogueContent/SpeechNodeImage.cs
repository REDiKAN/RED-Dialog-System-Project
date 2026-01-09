// Assets/Scripts/Editor/DialogueGraph/Nodes/SpeechNodeImage.cs
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

public class SpeechNodeImage : SpeechNode
{
    public Sprite ImageSprite { get; set; }
    public ObjectField imageField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Speech (Image)";

        if (dialogueTextField != null)
        {
            mainContainer.Remove(dialogueTextField);
            dialogueTextField = null;
            DialogueText = string.Empty;
        }
        if (audioField != null)
        {
            mainContainer.Remove(audioField);
            audioField = null;
            AudioClip = null;
        }

        // Инициализируем поле класса
        imageField = new ObjectField("Image Sprite");
        imageField.objectType = typeof(Sprite);
        imageField.RegisterValueChangedCallback(evt =>
        {
            ImageSprite = evt.newValue as Sprite;
        });
        mainContainer.Add(imageField);

        styleSheets.Add(Resources.Load<StyleSheet>("SpeechNodeImage"));
    }
}