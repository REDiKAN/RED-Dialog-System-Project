using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��������� ��� UI ��������
/// </summary>
public abstract class DialogueUI : MonoBehaviour
{
    public abstract void ShowDialogue(string text, List<NodeLinkData> choices, System.Action<int> onChoiceSelected);
    public abstract void HideDialogue();
}
