using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionPanel : MonoBehaviour
{
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private Transform contentContainer;

    public event Action<string> onOptionSelected;

    /// <summary>
    /// Показывает панель с вариантами ответов
    /// </summary>
    public void ShowOptions(List<Option> options)
    {
        if (optionButtonPrefab == null || contentContainer == null)
        {
            Debug.LogError("OptionPanel references not set!");
            return;
        }

        // Очищаем предыдущие кнопки
        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);

        // Создаем кнопки для каждого варианта
        foreach (var option in options)
        {
            var buttonGO = Instantiate(optionButtonPrefab, contentContainer);
            var button = buttonGO.GetComponent<Button>();
            var buttonText = buttonGO.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
                buttonText.text = option.Text;
            else
                Debug.LogError("Text component not found in option button prefab");

            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    onOptionSelected?.Invoke(option.NextNodeGuid);
                    Destroy(button.gameObject);
                    //Hide();
                });
            }
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Скрывает панель вариантов
    /// </summary>
    public void Hide()
    {
        //gameObject.SetActive(false);
    }
}