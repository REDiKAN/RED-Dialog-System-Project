using UnityEngine.UI;
using UnityEngine;

public class ChatMessengerPanel : ChatPanel
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentContainer;

    public override void AddMessage(Message message, MessageTypeDialogue messageType)
    {
        if (contentContainer == null)
        {
            Debug.LogError("Content container not assigned in ChatPanel");
            return;
        }
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect not assigned in ChatPanel");
            return;
        }

        // ќпредел€ем нужный префаб по типу сообщени€ и отправителю
        GameObject prefabToInstantiate = GetPrefabForMessage(message, messageType);
        if (prefabToInstantiate == null)
            return; // ошибка уже залогирована в GetPrefabForMessage

        // »нстанциируем и настраиваем
        GameObject messageGO = Instantiate(prefabToInstantiate, contentContainer);
        messageGO.transform.SetAsLastSibling();

        if (messageGO.TryGetComponent(out IMessageObject messageObject))
        {
            messageObject.InitializationContent(message);
            messageObject.SetCharacterAvatar(message.Sender);
            messageObject.SetCharacterName(message.Sender);
        }

        // ѕрокрутка вниз с принудительным обновлением UI
        ScrollToBottom();

        ForceScrollToBottom();
    }

    private void ScrollToBottom()
    {
        // ѕринудительно обновл€ем холст, чтобы убедитьс€, что размеры контента актуальны
        Canvas.ForceUpdateCanvases();

        // ”станавливаем позицию прокрутки в самый низ
        if (scrollRect.content != null)
            scrollRect.verticalNormalizedPosition = 0f; // Unity использует 0 дл€ "самого низа"
    }

    public void ForceScrollToBottom()
    {
        // ѕринудительно обновл€ем холст, чтобы убедитьс€, что размеры контента актуальны
        Canvas.ForceUpdateCanvases();
        // ”станавливаем позицию прокрутки в самый низ
        if (scrollRect.content != null)
            scrollRect.verticalNormalizedPosition = 0f; // Unity использует 0 дл€ "самого низа"
    }
}
