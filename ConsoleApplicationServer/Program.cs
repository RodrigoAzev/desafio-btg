using ConsoleApplicationServer.Domain;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    private const string RequestQueueName = "request_queue";
    private const string ResponseQueueName = "response_queue";

    static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: RequestQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueDeclare(queue: ResponseQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);
            var properties = ea.BasicProperties;
            var correlationId = properties.CorrelationId;

            try
            {
                TextFunctionConfiguration requestObject = JsonConvert.DeserializeObject<TextFunctionConfiguration>(messageJson);
                TextExtractedProperties responseObject = processText(requestObject);
                var responseJson = JsonConvert.SerializeObject(responseObject);
                var responseBody = Encoding.UTF8.GetBytes(responseJson);
                var responseProperties = channel.CreateBasicProperties();
                responseProperties.CorrelationId = correlationId;
                channel.BasicPublish(exchange: "", routingKey: properties.ReplyTo, basicProperties: responseProperties, body: responseBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" [!] Error processing message: {ex.Message}");
            }
        };

        channel.BasicConsume(queue: RequestQueueName, autoAck: true, consumer: consumer);
    }

    private static TextExtractedProperties processText(TextFunctionConfiguration functionConfiguration){
        TextExtractedProperties processedText = new TextExtractedProperties {
            wordOccurrences = GetWordOccurrences(functionConfiguration.originalText),
            totalWords = CountWords(functionConfiguration.originalText),
            totalBlankSpaces = Regex.Matches(functionConfiguration.originalText, @"\s").Count,
            totalTargetsFound = CountWordOccurrences(functionConfiguration.originalText, functionConfiguration.target)
        };
        return processedText;
    }

    private static int CountWordOccurrences(string originalText, string target)
    {
        if (string.IsNullOrWhiteSpace(originalText) || string.IsNullOrWhiteSpace(target))
        {
            return 0;
        }
        var regex = new Regex(@"\b" + Regex.Escape(target) + @"\b");
        var matches = regex.Matches(originalText);

        return matches.Count;
    }
    static int CountWords(string originalText)
    {
        if (string.IsNullOrWhiteSpace(originalText))
        {
            return 0;
        } else {
            var matches = Regex.Matches(originalText, @"\b\w+\b");
            return matches.Count;
        }
    }
    static List<WordOccurrence> GetWordOccurrences(string originalText)
    {
        if (string.IsNullOrWhiteSpace(originalText))
        {
            return new List<WordOccurrence>();
        }

        var wordCount = new Dictionary<string, int>();
        var matches = Regex.Matches(originalText, @"\b\w+\b");
        foreach (Match match in matches)
        {
            var word = match.Value;
            if (wordCount.ContainsKey(word))
            {
                wordCount[word]++;
            }
            else
            {
                wordCount[word] = 1;
            }
        }
        var wordOccurrences = new List<WordOccurrence>();
        foreach (var entry in wordCount)
        {
            wordOccurrences.Add(new WordOccurrence
            {
                word = entry.Key,
                occurrences = entry.Value
            });
        }
        return wordOccurrences;
    }
}