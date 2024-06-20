namespace BlobHandler.Messaging
{
    public interface IEventPublisher
    {
        void Dispose();
        void Publish<T>(string topic, T data);
    }
}