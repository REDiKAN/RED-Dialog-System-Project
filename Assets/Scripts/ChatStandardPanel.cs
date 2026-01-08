using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class ChatStandardPanel : ChatPanel
{
    [Header("Chat Components")]
    [SerializeField] private TMP_Text messageContent;
    [SerializeField] private TMP_Text characterName;
    [SerializeField] private Image characterIcon;

    public override void AddMessage(Message message, MessageTypeDialogue messageType)
    {
        if (messageContent != null)
            messageContent.text = message.Text;
        else
            Debug.LogError("messageContent не назначен в инспекторе ChatStandardPanel!", this);

        if (characterName != null && message.Sender != null && !string.IsNullOrEmpty(message.Sender.FirstName))
            characterName.text = message.Sender.FirstName;

        if (characterIcon != null && message.Sender != null && message.Sender.Icon != null)
            characterIcon.sprite = message.Sender.Icon;

        Canvas.ForceUpdateCanvases();
    }
}