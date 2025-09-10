using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class OptionNodeText : OptionNode
{
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Option (Text)";

        // ������� �������� ����
        if (audioField != null)
        {
            mainContainer.Remove(audioField);
            audioField = null;
            AudioClip = null;
        }

        // ������������� ����
        styleSheets.Add(Resources.Load<StyleSheet>("OptionNodeText"));
    }
}