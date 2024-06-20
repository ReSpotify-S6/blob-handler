using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace BlobHandler.Messaging;

public class EventPublisher : IDisposable, IEventPublisher
{
    private readonly IConnection _connection;

    private readonly EnvironmentVariableManager _envManager;

    public EventPublisher(EnvironmentVariableManager envManager)
    {
        var hostname = envManager["RABBITMQ_HOSTNAME"];
        var username = envManager["RABBITMQ_USERNAME"];
        var password = envManager["RABBITMQ_PASSWORD"];

        var factory = new ConnectionFactory
        {
            HostName = hostname,
            UserName = username,
            Password = password
        };

        _connection = factory.CreateConnection();
        _envManager = envManager;
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
        GC.SuppressFinalize(this);
        _connection.Close();
    }
}