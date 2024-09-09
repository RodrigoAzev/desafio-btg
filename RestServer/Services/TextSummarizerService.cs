using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RestServer.Domain;

namespace RestServer.Services;
public class TextSummarizerService : ITextSummarizerService
{
    private static string _responseQueueName = "response_queue";
    private static string _requestQueueName = "request_queue";
    private readonly ConnectionFactory _factory;

    public TextSummarizerService()
    {
        _factory = new ConnectionFactory() { HostName = "localhost" };
    }
    public async Task<TextExtractedProperties> SummarizeText(TextFunctionConfiguration textFunctionConfiguration)
    {
        TextExtractedProperties extractedProperties = await SendMessageAsync(textFunctionConfiguration);
        return extractedProperties;
    }


    private async Task<TextExtractedProperties> SendMessageAsync(TextFunctionConfiguration messageObject)
    {
        var requestId = Guid.NewGuid().ToString();

        using (var connection = _factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: _requestQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(queue: _responseQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            var messageJson = JsonConvert.SerializeObject(messageObject);
            var body = Encoding.UTF8.GetBytes(messageJson);
            var properties = channel.CreateBasicProperties();
            properties.ReplyTo = _responseQueueName;
            properties.CorrelationId = requestId;
            // publica mensagem no rabbitMq para 
            channel.BasicPublish(exchange: "", routingKey: _requestQueueName, basicProperties: properties, body: body);
            var taskCompletionSource = new TaskCompletionSource<TextExtractedProperties>();
            // inicia consumo da fila de resposta 
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == requestId)
                {
                    var responseJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var responseObject = JsonConvert.DeserializeObject<TextExtractedProperties>(responseJson);
                    taskCompletionSource.SetResult(responseObject);
                }
            };
            channel.BasicConsume(queue: _responseQueueName, autoAck: true, consumer: consumer); 
            return await taskCompletionSource.Task;
        }
    }
}



