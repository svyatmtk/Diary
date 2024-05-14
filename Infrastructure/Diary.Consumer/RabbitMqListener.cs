﻿using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Diary.Domain.Settings;
using RabbitMQ.Client.Events;

namespace Diary.Consumer;

public class RabbitMqListener : BackgroundService
{
    private readonly IModel _channel;
    private readonly IOptions<RabbitMqSettings> _options;

    public RabbitMqListener(IOptions<RabbitMqSettings> options)
    {
        _options = options;
        var factory = new ConnectionFactory() { HostName = "localhost" };
        var connection = factory.CreateConnection();
        _channel = connection.CreateModel();
        _channel.QueueDeclare(_options.Value.QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: null);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (obj, basicDeliver) =>
        {
            var content = Encoding.UTF8.GetString(basicDeliver.Body.ToArray());
            Debug.WriteLine($"Получено сообщение: {content}");
            
            _channel.BasicAck(basicDeliver.DeliveryTag, false);
        };
        _channel.BasicConsume(_options.Value.QueueName, false, consumer);
        
        return Task.CompletedTask;
    }
}