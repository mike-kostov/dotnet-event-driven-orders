using System.Text.Json;
using Confluent.Kafka;
using OrderProcessor.Contracts;

namespace OrderProcessor;

// A hosted background service that consumes OrderCommands from the 'orders'
// topic. This lesson it just consumes and logs. Persistence (lesson 5), the
// state machine (lesson 6), and manual commit (lesson 9) come later.
public sealed class ConsumerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<ConsumerService> _logger;

    public ConsumerService(IConfiguration config, ILogger<ConsumerService> logger)
    {
        _logger = logger;
        var bootstrap = config["KAFKA_BOOTSTRAP"] ?? "kafka:9092";
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "order-processor",            // offsets are tracked per consumer group
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true                 // simplest for now; lesson 9 → manual commit after the DB write
        }).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("orders");
        _logger.LogInformation("Subscribed to 'orders' as group 'order-processor'");
        await Task.Yield(); // let the web host finish starting before we block on Consume

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = _consumer.Consume(stoppingToken);

                // TODO(you) 4.1 — handle the consumed message:
                //   a) deserialize the JSON value into an OrderCommand:
                //        var cmd = JsonSerializer.Deserialize<OrderCommand>(result.Message.Value);
                //   b) log what you received and where it came from:
                //        _logger.LogInformation(
                //            "Consumed {Type} for order {OrderId} (partition {Partition}, offset {Offset})",
                //            cmd?.Type, cmd?.OrderId, result.Partition.Value, result.Offset.Value);
                //
                // Offsets auto-commit for now. Lesson 9 turns this into a MANUAL
                // commit AFTER persisting to Postgres (at-least-once + idempotency).
            }
        }
        catch (OperationCanceledException)
        {
            // normal on shutdown
        }
        finally
        {
            _consumer.Close(); // leave the group cleanly
        }
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}
