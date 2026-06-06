using System.Text;
using Confluent.Kafka;

namespace OrderProcessor;

// Publishes failed messages to the dead-letter topic 'orders.DLT' (ADR-0007),
// keeping the original key + value bytes and adding diagnostic headers. After a
// message is dead-lettered the consumer commits its offset, so one poison message
// never blocks its partition.
public sealed class DeadLetter
{
    public const string Topic = "orders.DLT";
    private readonly IProducer<string, string> _producer;

    public DeadLetter(IConfiguration config)
    {
        var bootstrap = config["KAFKA_BOOTSTRAP"] ?? "kafka:9092";
        _producer = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = bootstrap }).Build();
    }

    public async Task SendAsync(ConsumeResult<string, string> source, string reason)
    {
        var headers = new Headers
        {
            { "x-error", Encoding.UTF8.GetBytes(reason) },
            { "x-original-topic", Encoding.UTF8.GetBytes(source.Topic) },
            { "x-original-partition", Encoding.UTF8.GetBytes(source.Partition.Value.ToString()) },
            { "x-original-offset", Encoding.UTF8.GetBytes(source.Offset.Value.ToString()) },
            { "x-failed-at", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("o")) },
        };
        await _producer.ProduceAsync(Topic, new Message<string, string>
        {
            Key = source.Message.Key,
            Value = source.Message.Value,
            Headers = headers,
        });
    }
}
