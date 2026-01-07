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
        // Определяем нужный префаб по типу сообщения и отправителю
        GameObject prefabToInstantiate = GetPrefabForMessage(message, messageType);
        if (prefabToInstantiate == null)
            return;

        messageContent.text = message.Text;
        characterName.text = message.Sender.FirstName;
        characterIcon.sprite = message.Sender.Icon;

        Canvas.ForceUpdateCanvases();
    }
}
