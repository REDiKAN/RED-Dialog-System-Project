using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionPanel : MonoBehaviour
{
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private Transform contentContainer;

    public event Action<string> onOptionSelected;

    /// <summary>
    /// ���������� ������ � ���������� �������
    /// </summary>
    public void ShowOptions(List<Option> options)
    {
        if (optionButtonPrefab == null || contentContainer == null)
        {
            Debug.LogError("OptionPanel references not set!");
            return;
        }

        // ������� ���������� ������
        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);

        // ������� ������ ��� ������� ��������
        foreach (var option in options)
        {
            var buttonGO = Instantiate(optionButtonPrefab, contentContainer);
            var button = buttonGO.GetComponent<Button>();
            var buttonText = buttonGO.GetComponentInChildren<Text>();

            if (buttonText != null)
                buttonText.text = option.Text;
            else
                Debug.LogError("Text component not found in option button prefab");

            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    onOptionSelected?.Invoke(option.NextNodeGuid);
                    Hide();
                });
            }
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// �������� ������ ���������
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}