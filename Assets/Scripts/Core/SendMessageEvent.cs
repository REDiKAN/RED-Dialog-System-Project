public class SendMessageEvent : IEvent
{
    public IMessageDTO Message { get; private set; }
    public Transform Parent { get; private set; }

    public SendMessageEvent(IMessageDTO message, Transform parent) 
    {  
        Message = message; 
        Parent = parent;
    }
}
