using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class SpeechNodeAudio : SpeechNode
{
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Speech (Audio)";

        // ������� ��������� ����
        if (dialogueTextField != null)
        {
            mainContainer.Remove(dialogueTextField);
            dialogueTextField = null;
            DialogueText = string.Empty;
        }

        // ������������� ����
        styleSheets.Add(Resources.Load<StyleSheet>("SpeechNodeAudio"));
    }
}