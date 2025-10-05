using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class SpeechTextMessage : MonoBehaviour, IMessageObject
{
    [Header("Character Data")]
    [SerializeField] private Image characterAvatar;
    [SerializeField] private TMP_Text characterName;

    [Header("Message Data")]
    [SerializeField] private TMP_Text textMessage;

    private RectTransform _rectTransform;
    private ContentSizeFitter _contentSizeFitter;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _contentSizeFitter = GetComponent<ContentSizeFitter>();

        if (_contentSizeFitter == null)
            _contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();

        // Важно: фит только по высоте, ширина — фиксирована через RectTransform
        _contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        _contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Настройка TMP
        if (textMessage != null)
        {
            textMessage.enableWordWrapping = true;
            textMessage.textWrappingMode = TextWrappingModes.Normal;
            textMessage.overflowMode = TextOverflowModes.Overflow; // не обрезать
        }
    }

    public void InitializationContent(Message contentMessage)
    {
        if (textMessage == null)
        {
            Debug.LogError("textMessage is not assigned!");
            return;
        }

        textMessage.text = !string.IsNullOrEmpty(contentMessage.Text) ? contentMessage.Text : "";

        // Устанавливаем максимальную ширину = 80% экрана
        float maxWidth = Screen.width * 0.8f;

        // Ограничиваем ширину через RectTransform
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);

        // Принудительно обновляем текст, чтобы перенос сработал
        textMessage.ForceMeshUpdate();

        SetCharacterAvatar(contentMessage.Sender);
        SetCharacterName(contentMessage.Sender);
    }

    public void SetCharacterAvatar(CharacterData character)
    {
        if (characterAvatar != null && character != null && character.Icon != null)
            characterAvatar.sprite = character.Icon;
    }

    public void SetCharacterName(CharacterData character)
    {
        if (characterName != null && character != null)
        {
            characterName.text = character.FirstName;
            characterName.color = character.NameColor;
        }
    }
}