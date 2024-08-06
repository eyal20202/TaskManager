using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskManagerApi.Data;
using Microsoft.EntityFrameworkCore; // Ensure this is included

public class TaskReminderWorker : BackgroundService
{
    private readonly ILogger<TaskReminderWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public TaskReminderWorker(ILogger<TaskReminderWorker> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        var factory = new ConnectionFactory() { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(queue: "task_reminders", durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskContext>();
                var tasks = await dbContext.Tasks
                    .Where(t => (t.IsCompleted ?? false) == false && t.DueDate < DateTime.Now)
                     .ToListAsync();

                foreach (var task in tasks)
                {
                    var message = $"Reminder: Task '{task.Title}' is overdue.";
                    var body = Encoding.UTF8.GetBytes(message);
                    _channel.BasicPublish(exchange: "", routingKey: "task_reminders", basicProperties: null, body: body);
                    _logger.LogInformation($"Published reminder for task '{task.Title}' to the queue.");
                }
            }

            await Task.Delay(60000, stoppingToken); // Check every minute
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Close();
        _connection.Close();
        _logger.LogInformation("TaskReminderWorker stopped.");
        return base.StopAsync(cancellationToken);
    }
}
