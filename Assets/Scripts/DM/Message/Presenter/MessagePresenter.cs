using TMPro;
using UnityEngine;

public class MessagePresenter : MonoBehaviour
{
    [SerializeField] private TMP_Text _authorName;
    [SerializeField] private TMP_Text _messageContent;
    [SerializeField] private TMP_Text _messageTime;

    public void Initialize(string authorName, string messageContent, Time messageTime)
    {
        _authorName.text = authorName;
        _messageContent.text = messageContent;
        _messageTime.text = messageTime.ToString();
    }
}
