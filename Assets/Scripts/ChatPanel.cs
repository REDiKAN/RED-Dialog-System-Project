using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChatPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private Transform contentContainer;

    /// <summary>
    /// Добавляет сообщение в чат
    /// </summary>
    /// <param name="message">Данные сообщения</param>
    public void AddMessage(Message message)
    {
        // Проверяем обязательные ссылки
        if (messagePrefab == null)
        {
            Debug.LogError("Message prefab не назначен в ChatPanel");
            return;
        }

        if (contentContainer == null)
        {
            Debug.LogError("Content container не назначен в ChatPanel");
            return;
        }

        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect не назначен в ChatPanel");
            return;
        }

        // Создаем экземпляр префаба сообщения
        var messageGO = Instantiate(messagePrefab, contentContainer);

        // Получаем компоненты сообщения
        var textComponent = messageGO.GetComponent<Text>();
        var imageComponent = messageGO.GetComponent<Image>();
        var characterAvatar = messageGO.transform.Find("Avatar")?.GetComponent<Image>();
        var characterName = messageGO.transform.Find("Name")?.GetComponent<Text>();

        // Обработка типа сообщения
        if (message.Type == MessageType.NPC)
        {
            // Левая сторона для NPC
            messageGO.transform.SetAsFirstSibling();

            // Установка аватара и имени (если они существуют)
            if (characterAvatar != null && message.Sender != null && message.Sender.Icon != null)
            {
                characterAvatar.sprite = message.Sender.Icon;
            }
            else if (characterAvatar != null && message.Sender == null)
            {
                Debug.LogWarning("Персонаж не указан для NPC сообщения");
            }

            if (characterName != null && message.Sender != null)
            {
                characterName.text = message.Sender.FirstName;
                characterName.color = message.Sender.NameColor;
            }

            // Отображение текста/изображения
            if (textComponent != null)
            {
                if (!string.IsNullOrEmpty(message.Text))
                    textComponent.text = message.Text;
                else
                    textComponent.text = "";
            }

            if (imageComponent != null)
            {
                if (message.Image != null)
                    imageComponent.sprite = message.Image;
                else
                    imageComponent.sprite = null;
            }
        }
        else if (message.Type == MessageType.Player)
        {
            // Правая сторона для игрока
            messageGO.transform.SetAsLastSibling();

            if (textComponent != null)
            {
                textComponent.text = message.Text;
            }
        }
        else if (message.Type == MessageType.System)
        {
            // Системное сообщение
            if (textComponent != null)
            {
                textComponent.text = message.Text;
                // Можно добавить стилизацию для системных сообщений
            }
        }

        // Автопрокрутка вниз
        if (scrollRect.content != null)
        {
            scrollRect.verticalNormalizedPosition = 0;
        }
    }
}