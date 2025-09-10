using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class OptionNodeAudio : OptionNode
{
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Option (Audio)";

        // ������� ��������� ����
        if (responseTextField != null)
        {
            mainContainer.Remove(responseTextField);
            responseTextField = null;
            ResponseText = string.Empty;
        }

        // ������������� ����
        styleSheets.Add(Resources.Load<StyleSheet>("OptionNodeAudio"));
    }
}