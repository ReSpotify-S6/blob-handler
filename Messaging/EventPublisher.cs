using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace BlobHandler.Messaging;

public class EventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;

    public EventPublisher(IReadOnlyDictionary<string, string> envStore, ILogger<EventPublisher> logger)
    {
        var hostname = envStore["RABBITMQ_HOSTNAME"];
        var username = envStore["RABBITMQ_USERNAME"];
        var password = envStore["RABBITMQ_PASSWORD"];

        var factory = new ConnectionFactory
        {
            HostName = hostname,
            UserName = username,
            Password = password
        };
        
        while (true)
        {
            try
            {
                _connection = factory.CreateConnection();
                break;
            }
            catch(Exception ex)
            {
                logger.LogError(ex.Message);
                logger.LogInformation("Retrying to connect to RabbitMQ in 5 seconds...");
                Thread.Sleep(5000);
            }
        }
    }


    public void Publish<T>(string topic, T data)
    {
        using var channel = _connection.CreateModel();

        channel.QueueDeclare(
            queue: topic,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var json = JsonSerializer.Serialize(data);
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(string.Empty, topic, body: body);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }


}