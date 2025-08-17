using System.Collections.Generic;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
    [SerializeField] private DialogueContainer dialogueContainer;

    [Space(5)]

    [SerializeField] private DialogueUI dialogueUI;

    private DialogueNodeData currentNode;
    private Dictionary<string, List<NodeLinkData>> nodeLinks;
    private Dictionary<string, DialogueNodeData> dialogueNodes;

    private void Start()
    {
        StartDialogue(dialogueContainer);
    }

    /// <summary>
    /// ������ �������
    /// </summary>
    public void StartDialogue(DialogueContainer dialogueContainer)
    {
        // ������� ��������� ���� (������ � ������ ������)
        if (dialogueContainer.NodeLinks.Count > 0)
        {
            var startNodeGuid = dialogueContainer.NodeLinks[0].BaseNodeGuid;
            if (dialogueNodes.TryGetValue(startNodeGuid, out currentNode))
                ShowCurrentNode();
        }
    }

    private void ShowCurrentNode()
    {
        if (currentNode == null) return;

        // �������� �������� �������
        List<NodeLinkData> choices = null;
        if (nodeLinks.TryGetValue(currentNode.Guid, out var links))
        {
            choices = links;
        }

        // ���������� ������ ����� UI
        //dialogueUI.ShowDialogue(currentNode.DialogueText, choices, SelectChoice);
    }
}
