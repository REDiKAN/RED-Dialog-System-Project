using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChatPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private Transform contentContainer;

    /// <summary>
    /// ��������� ��������� � ���
    /// </summary>
    /// <param name="message">������ ���������</param>
    public void AddMessage(Message message)
    {
        // ��������� ������������ ������
        if (messagePrefab == null)
        {
            Debug.LogError("Message prefab �� �������� � ChatPanel");
            return;
        }

        if (contentContainer == null)
        {
            Debug.LogError("Content container �� �������� � ChatPanel");
            return;
        }

        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect �� �������� � ChatPanel");
            return;
        }

        // ������� ��������� ������� ���������
        var messageGO = Instantiate(messagePrefab, contentContainer);

        // �������� ���������� ���������
        var textComponent = messageGO.GetComponent<Text>();
        var imageComponent = messageGO.GetComponent<Image>();
        var characterAvatar = messageGO.transform.Find("Avatar")?.GetComponent<Image>();
        var characterName = messageGO.transform.Find("Name")?.GetComponent<Text>();

        // ��������� ���� ���������
        if (message.Type == MessageType.NPC)
        {
            // ����� ������� ��� NPC
            messageGO.transform.SetAsFirstSibling();

            // ��������� ������� � ����� (���� ��� ����������)
            if (characterAvatar != null && message.Sender != null && message.Sender.Icon != null)
            {
                characterAvatar.sprite = message.Sender.Icon;
            }
            else if (characterAvatar != null && message.Sender == null)
            {
                Debug.LogWarning("�������� �� ������ ��� NPC ���������");
            }

            if (characterName != null && message.Sender != null)
            {
                characterName.text = message.Sender.FirstName;
                characterName.color = message.Sender.NameColor;
            }

            // ����������� ������/�����������
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
            // ������ ������� ��� ������
            messageGO.transform.SetAsLastSibling();

            if (textComponent != null)
            {
                textComponent.text = message.Text;
            }
        }
        else if (message.Type == MessageType.System)
        {
            // ��������� ���������
            if (textComponent != null)
            {
                textComponent.text = message.Text;
                // ����� �������� ���������� ��� ��������� ���������
            }
        }

        // ������������� ����
        if (scrollRect.content != null)
        {
            scrollRect.verticalNormalizedPosition = 0;
        }
    }
}