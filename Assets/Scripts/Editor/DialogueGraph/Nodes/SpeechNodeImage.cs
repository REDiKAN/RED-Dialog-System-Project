using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;

public class SpeechNodeImage : SpeechNode
{
    public Sprite ImageSprite { get; set; }
    private ObjectField imageField;

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Speech (Image)";

        // Убираем ненужные поля
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

        // Добавляем поле для изображения
        imageField = new ObjectField("Image Sprite");
        imageField.objectType = typeof(Sprite);
        imageField.RegisterValueChangedCallback(evt =>
        {
            ImageSprite = evt.newValue as Sprite;
        });
        mainContainer.Add(imageField);

        // Устанавливаем цвет
        styleSheets.Add(Resources.Load<StyleSheet>("SpeechNodeImage"));
    }
}
