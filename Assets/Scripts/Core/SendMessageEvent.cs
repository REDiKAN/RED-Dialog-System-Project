public class SendMessageEvent : IEvent
{
    public IMessageDTO Message { get; private set; }

    public SendMessageEvent(IMessageDTO message) {  Message = message; }
}
