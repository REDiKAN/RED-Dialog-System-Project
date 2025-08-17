using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// ������ ���������� UI ��� ��������
/// </summary>
public class SimpleDialogueUI : DialogueUI
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMPro.TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    public override void ShowDialogue(string text, List<NodeLinkData> choices, System.Action<int> onChoiceSelected)
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = text;

        // ������� ���������� �������� �������
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }

        // ������� ������ ��� ��������� �������
        if (choices != null && choices.Count > 0)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
                var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
                var textComp = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

                textComp.text = choice.PortName;

                int choiceIndex = i; // ��������� ����� ��� ���������
                button.onClick.AddListener(() => onChoiceSelected(choiceIndex));
            }
        }
        else
        {
            // ���� ��� ���������, ��������� ������ ��� �����������
            var buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
            var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
            var textComp = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            textComp.text = "Continue";
            button.onClick.AddListener(() => onChoiceSelected(0));
        }
    }

    public override void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}
