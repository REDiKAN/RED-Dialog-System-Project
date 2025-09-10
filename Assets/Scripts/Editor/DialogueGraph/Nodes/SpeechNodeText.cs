using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class SpeechNodeText : SpeechNode
{
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Speech (Text)";

        // Убираем ненужные поля
        if (audioField != null)
        {
            mainContainer.Remove(audioField);
            audioField = null;
            AudioClip = null;
        }

        // Устанавливаем цвет
        styleSheets.Add(Resources.Load<StyleSheet>("SpeechNodeText"));
    }
}