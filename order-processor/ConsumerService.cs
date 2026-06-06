using System.Text.Json;
using Confluent.Kafka;
using OrderProcessor.Contracts;

namespace OrderProcessor;

// Consumes OrderCommands from 'orders', reliably (lesson 9):
//   • manual commit AFTER handling (at-least-once)
//   • dead-letter permanent failures so a poison message can't block a partition
// Idempotency lives in OrderStore via ON CONFLICT on event_id.
public sealed class ConsumerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<ConsumerService> _logger;
    private readonly OrderStore _store;
    private readonly DeadLetter _deadLetter;

    public ConsumerService(IConfiguration config, ILogger<ConsumerService> logger, OrderStore store, DeadLetter deadLetter)
    {
        _logger = logger;
        _store = store;
        _deadLetter = deadLetter;
        var bootstrap = config["KAFKA_BOOTSTRAP"] ?? "kafka:9092";
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "order-processor",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false   // we commit manually, after the write
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
                try
                {
                    var cmd = JsonSerializer.Deserialize<OrderCommand>(result.Message.Value)
                              ?? throw new InvalidOperationException("unparseable message");

                    if (cmd.Type == "PLACE")
                    {
                        await _store.SavePlacedOrderAsync(cmd);
                        _logger.LogInformation("Placed order {OrderId}", cmd.OrderId);
                    }
                    else
                    {
                        var current = await _store.LoadStateAsync(cmd.OrderId);
                        var next = current is null ? null : OrderStateMachine.Next(current, cmd.Type);
                        if (next is null)
                        {
                            await _deadLetter.SendAsync(result, $"illegal transition {cmd.Type} from {current ?? "<none>"}");
                            _logger.LogWarning("Dead-lettered illegal {Type} for order {OrderId} (state {State})",
                                cmd.Type, cmd.OrderId, current ?? "<unknown>");
                        }
                        else
                        {
                            await _store.ApplyTransitionAsync(cmd, next);
                            _logger.LogInformation("Applied {Type}: order {OrderId} -> {State}", cmd.Type, cmd.OrderId, next);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    await _deadLetter.SendAsync(result, ex.Message);
                    _logger.LogError(ex, "Dead-lettered message at offset {Offset}", result.Offset.Value);
                }

                // Commit only after handling (success or dead-letter): a crash before
                // here just redelivers, and idempotency makes redelivery safe.
                _consumer.Commit(result);
            }
        }
        catch (OperationCanceledException) { }
        finally { _consumer.Close(); }
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}
