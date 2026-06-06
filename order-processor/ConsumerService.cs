using System.Text.Json;
using Confluent.Kafka;
using OrderProcessor.Contracts;

namespace OrderProcessor;

// Consumes OrderCommands from 'orders'. Lesson 9 makes it RELIABLE:
//   • commit the offset manually, only AFTER handling (at-least-once)
//   • dead-letter permanent failures so a poison message can't block a partition
// (Idempotency lives in OrderStore via ON CONFLICT on event_id.)
public sealed class ConsumerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<ConsumerService> _logger;
    private readonly OrderStore _store;
    // TODO(you) 9.3 — inject the dead-letter sender:
    //   private readonly DeadLetter _deadLetter;

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
            // TODO(you) 9.1 — turn auto-commit OFF. We commit manually below, only
            //   after handling succeeds (or after dead-lettering), for at-least-once.
            EnableAutoCommit = true
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
                            // A permanent (business-invalid) failure.
                            // TODO(you) 9.3 — dead-letter it instead of just logging:
                            //   await _deadLetter.SendAsync(result,
                            //       $"illegal transition {cmd.Type} from {current ?? "<none>"}");
                            _logger.LogWarning("Illegal transition {Type} for order {OrderId} (state {State})",
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
                    // A transient/unexpected failure.
                    // TODO(you) 9.3 — dead-letter it too:
                    //   await _deadLetter.SendAsync(result, ex.Message);
                    _logger.LogError(ex, "Failed to handle message at offset {Offset}", result.Offset.Value);
                }

                // TODO(you) 9.1 — commit the offset HERE, after handling (success OR
                //   dead-letter). A crash before this line just redelivers the message:
                //     _consumer.Commit(result);
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
