namespace BlobHandler.Messaging;

public interface IEventPublisher
{
    void Publish<T>(string topic, T data);
}