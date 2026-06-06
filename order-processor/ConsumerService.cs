using System.Text.Json;
using Confluent.Kafka;
using OrderProcessor.Contracts;

namespace OrderProcessor;

// Consumes OrderCommands from 'orders'. Lesson 4: consume + log. Lesson 5: also
// persist PLACE commands. The state machine (lesson 6) and manual commit (lesson 9)
// come later.
public sealed class ConsumerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<ConsumerService> _logger;
    private readonly OrderStore _store;

    public ConsumerService(IConfiguration config, ILogger<ConsumerService> logger, OrderStore store)
    {
        _logger = logger;
        _store = store;
        var bootstrap = config["KAFKA_BOOTSTRAP"] ?? "kafka:9092";
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "order-processor",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true                 // lesson 9 → manual commit after the DB write
        }).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("orders");
        _logger.LogInformation("Subscribed to 'orders' as group 'order-processor'");
        await Task.Yield();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = _consumer.Consume(stoppingToken);
                var cmd = JsonSerializer.Deserialize<OrderCommand>(result.Message.Value);

                if (cmd is { Type: "PLACE" })
                    await _store.SavePlacedOrderAsync(cmd);

                _logger.LogInformation(
                    "Consumed {Type} for order {OrderId} (partition {Partition}, offset {Offset})",
                    cmd?.Type, cmd?.OrderId, result.Partition.Value, result.Offset.Value);
            }
        }
        catch (OperationCanceledException)
        {
            // normal on shutdown
        }
        finally
        {
            _consumer.Close();
        }
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}
