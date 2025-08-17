using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Пример реализации UI для диалогов
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

        // Очищаем предыдущие варианты ответов
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }

        // Создаем кнопки для вариантов ответов
        if (choices != null && choices.Count > 0)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
                var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
                var textComp = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

                textComp.text = choice.PortName;

                int choiceIndex = i; // Локальная копия для замыкания
                button.onClick.AddListener(() => onChoiceSelected(choiceIndex));
            }
        }
        else
        {
            // Если нет вариантов, добавляем кнопку для продолжения
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
