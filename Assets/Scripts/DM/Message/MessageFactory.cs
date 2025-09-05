using UnityEngine;

public class MessageFactory
{
    private MessageConfig _config;

    public MessageFactory(MessageConfig config)
    {
        _config = config;
    }

    public void Create<T>(T DTO, Transform parent) where T : IMessageDTO
    {
        switch (DTO)
        {
            case MessageDTO message:
                InstantiateMessage(message, parent);
                break;

            case ImageMessageDTO image:
                InstantiateImageMessage(image, parent);
                break;

            case AudioMessageDTO audio:
                InstantiateAudioMessage(audio, parent);
                break;

            default:
                Debug.LogWarning("Unknow DTO: " + DTO.GetType().Name);
                break;
        }
    }

    private void InstantiateMessage(MessageDTO message, Transform parent)
    {
        GameObject messagePrefab = message.Author == Author.Owner ? _config.OwnerMessagePrefab : _config.MessagePrefab;
        GameObject.Instantiate(messagePrefab, parent).TryGetComponent(out MessagePresenter messagePresenter);
        messagePresenter.Initialize(message.AuthorName, message.Content, message.Time);
    }

    private void InstantiateImageMessage(ImageMessageDTO message, Transform parent)
    {
        GameObject.Instantiate(_config.ImageMessagePrefab, parent).TryGetComponent(out ImageMessagePresenter messagePresenter);
        messagePresenter.Initialize(message.Image);
    }

    private void InstantiateAudioMessage(AudioMessageDTO message, Transform parent)
    {
        GameObject.Instantiate(_config.AuidoMessagePrefab, parent).TryGetComponent(out AudioMessagePresenter messagePresenter);
        messagePresenter.Initialize(message.Clip);
    }
}
