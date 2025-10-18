using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChatPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentContainer;

    // Кэш валидности префабов: (character, messageType) → isValid
    private Dictionary<(CharacterData, MessageTypeDialogue), bool> _prefabValidationCache = new();

    public void AddMessage(Message message, MessageTypeDialogue messageType)
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

        // Определяем нужный префаб по типу сообщения и отправителю
        GameObject prefabToInstantiate = GetPrefabForMessage(message, messageType);
        if (prefabToInstantiate == null)
            return; // ошибка уже залогирована в GetPrefabForMessage

        // Инстанциируем и настраиваем
        GameObject messageGO = Instantiate(prefabToInstantiate, contentContainer);
        messageGO.transform.SetAsLastSibling();

        if (messageGO.TryGetComponent(out IMessageObject messageObject))
        {
            messageObject.InitializationContent(message);
            messageObject.SetCharacterAvatar(message.Sender);
            messageObject.SetCharacterName(message.Sender);
        }

        // Прокрутка вниз
        if (scrollRect.content != null)
            scrollRect.verticalNormalizedPosition = 1;
    }

    private GameObject GetPrefabForMessage(Message message, MessageTypeDialogue messageType)
    {
        if (message.Sender == null)
        {
            Debug.LogError($"Message sender is null for type {messageType}");
            return null;
        }

        var character = message.Sender;
        string characterName = $"{character.FirstName} {character.LastName}".Trim();

        // Определяем, какой префаб брать
        Object prefab = null;
        string typeName = "";
        switch (messageType)
        {
            case MessageTypeDialogue.Speech:
            case MessageTypeDialogue.SpeechText:
                prefab = character.SpeechTextMessagePrefab;
                typeName = "SpeechText";
                break;
            case MessageTypeDialogue.SpeechImage:
                prefab = character.SpeechImageMessagePrefab;
                typeName = "SpeechImage";
                break;
            case MessageTypeDialogue.SpeechAudio:
                prefab = character.SpeechAudioMessagePrefab;
                typeName = "SpeechAudio";
                break;
            default:
                // Опции и системные сообщения — не обрабатываем здесь
                Debug.LogError($"Unsupported message type for prefab resolution: {messageType}");
                return null;
        }

        // Проверка: назначен ли префаб?
        if (prefab == null)
        {
            Debug.LogError($"Missing {typeName} message prefab for character '{characterName}'.");
            return null;
        }

        // Кэшированная проверка на IMessageObject
        var cacheKey = (character, messageType);
        if (!_prefabValidationCache.TryGetValue(cacheKey, out bool isValid))
        {
            isValid = prefab is GameObject go && go.TryGetComponent(out IMessageObject _);
            _prefabValidationCache[cacheKey] = isValid;
        }

        if (!isValid)
        {
            Debug.LogError($"Assigned prefab for {typeName} does not implement IMessageObject.");
            return null;
        }

        return (prefab as GameObject);
    }
}