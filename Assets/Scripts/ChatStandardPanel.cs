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
        messageContent.text = message.Text;

        if (characterName != null)
            characterName.text = message.Sender.FirstName;
        if (characterIcon != null)
            characterIcon.sprite = message.Sender.Icon;

        Canvas.ForceUpdateCanvases();
    }
}
