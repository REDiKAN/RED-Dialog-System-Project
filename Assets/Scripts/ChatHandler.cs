using UnityEngine;

/// <summary>
/// Центральный обработчик чата, объединяющий все компоненты интерфейса
/// </summary>
public class ChatHandler : MonoBehaviour
{
    [Header("Ссылки на компоненты чата")]
    public ChatPanel chatPanel;
    public OptionPanel optionPanel;
    public TimerDisplayController timerDisplayController;

    [Header("Дополнительные настройки")]
    public bool isActiveByDefault = true;
    public float fadeDuration = 0.2f;

    private void Awake()
    {
        // Автоматическая деактивация при запуске, если не задано иное
        if (!isActiveByDefault)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Активирует/деактивирует все компоненты этого чата
    /// </summary>
    public void SetActive(bool isActive)
    {
        gameObject.SetActive(isActive);

        // Дополнительно можно добавить логику плавного появления/скрытия
        if (isActive)
        {
            if (chatPanel != null) chatPanel.gameObject.SetActive(true);
            if (optionPanel != null) optionPanel.gameObject.SetActive(true);
            if (timerDisplayController != null) timerDisplayController.gameObject.SetActive(true);
        }
        else
        {
            if (chatPanel != null) chatPanel.gameObject.SetActive(false);
            if (optionPanel != null) optionPanel.gameObject.SetActive(false);
            if (timerDisplayController != null) timerDisplayController.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Проверяет корректность настроек обработчика
    /// </summary>
    public bool ValidateSetup()
    {
        bool isValid = true;

        if (chatPanel == null)
        {
            Debug.LogError($"ChatHandler '{name}' requires a ChatPanel component!", this);
            isValid = false;
        }

        if (optionPanel == null)
        {
            Debug.LogWarning($"ChatHandler '{name}' has no OptionPanel. Option selection will not work.", this);
        }

        if (timerDisplayController == null)
        {
            Debug.LogWarning($"ChatHandler '{name}' has no TimerDisplayController. Timers will not display.", this);
        }

        return isValid;
    }
}