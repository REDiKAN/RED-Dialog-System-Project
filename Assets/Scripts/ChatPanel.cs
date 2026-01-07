using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChatPanel : MonoBehaviour
{
    private Dictionary<(CharacterData, MessageTypeDialogue), bool> _prefabValidationCache = new();

    public virtual void AddMessage(Message message, MessageTypeDialogue messageType) { }

    public GameObject GetPrefabForMessage(Message message, MessageTypeDialogue messageType)
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