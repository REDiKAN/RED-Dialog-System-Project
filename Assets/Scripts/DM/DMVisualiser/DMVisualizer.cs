public class DMVisualizer
{
    private MessageFactory _messageFactory;
    public void Initialize(EventBus eventBus, MessageFactory messageFactory)
    {
        eventBus.Subscribe<SendMessageEvent>(RecieveMessage);
        _messageFactory = messageFactory;
    }
    public void RecieveMessage(SendMessageEvent ctx)
    {
        _messageFactory.Create(ctx.Message);
    }
}
