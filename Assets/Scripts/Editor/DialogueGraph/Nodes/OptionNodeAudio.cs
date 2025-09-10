using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class OptionNodeAudio : OptionNode
{
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Option (Audio)";

        // Убираем текстовое поле
        if (responseTextField != null)
        {
            mainContainer.Remove(responseTextField);
            responseTextField = null;
            ResponseText = string.Empty;
        }

        // Устанавливаем цвет
        styleSheets.Add(Resources.Load<StyleSheet>("OptionNodeAudio"));
    }
}