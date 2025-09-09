using System.Collections.Generic;

public class DMContainer
{
    private List<IMessageDTO> _messages = new(); 

    public void Initialize(EventBus eventBus)
    {
        eventBus.Subscribe<SendMessageEvent>(RegisterMessage);
    }

    public void RegisterMessage(SendMessageEvent ctx)
    {
        _messages.Add(ctx.Message);
    }
}
