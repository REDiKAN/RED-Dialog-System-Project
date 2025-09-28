using UnityEngine;
using UnityEngine.UI;

public class ChatPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentContainer;

    [Header("Prefabs")]
    [SerializeField] private SpeechTextMessage speechTextMessagePrefab;
    [SerializeField] private OptionTextMessage optionTextMessagePrefab;

    /// <summary>
    /// Добавляет сообщение в чат
    /// </summary>
    /// <param name="message">Данные сообщения</param>
    public void AddMessage(Message message, MessageType messageType)
    {
        // Проверяем обязательные ссылки
        if (speechTextMessagePrefab == null)
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

        // Обработка типа сообщения
        if (message.Type == SenderType.NPC)
        {
            GameObject messageGO = Instantiate(speechTextMessagePrefab.gameObject, contentContainer);

            // Левая сторона для NPC
            messageGO.transform.SetAsFirstSibling();

            if (messageGO.TryGetComponent(out SpeechTextMessage speechText))
                speechText.InitializationContent(message);
        }
        else if (message.Type == SenderType.Player)
        {
            GameObject messageGO = Instantiate(optionTextMessagePrefab.gameObject, contentContainer);

            // Правая сторона для игрока
            messageGO.transform.SetAsLastSibling();

            if (messageGO.TryGetComponent(out OptionTextMessage optionText))
                optionText.InitializationContent(message);
        }
        else if (message.Type == SenderType.System)
        {
            // Системное сообщение
            //if (textComponent != null)
            //{
            //    textComponent.text = message.Text;
            //    Debug.Log(textComponent.text);
            //    // Можно добавить стилизацию для системных сообщений
            //}
        }

        // Автопрокрутка вниз
        if (scrollRect.content != null)
        {
            scrollRect.verticalNormalizedPosition = 0;
        }
    }
}