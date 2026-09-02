public readonly struct StSendMessageEvent
{
    public readonly string Message;
    public readonly EMessageType MessageType;

    public StSendMessageEvent(string message, EMessageType messageType)
    {
        Message = message;
        MessageType = messageType;
    }
}
