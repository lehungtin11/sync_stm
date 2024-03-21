using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using rsync_stm;
using Serilog;
using Serilog.Events;
using System.Text;

internal class Program
{
    public static config appSettings = new config();
    public static List<MappingValue> mapping = new List<MappingValue>();
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        try
		{
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .Enrich.FromLogContext()
                .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Log.txt"), rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 5000000, flushToDiskInterval: TimeSpan.FromSeconds(3))
                .CreateLogger();

            string txt = System.IO.File.ReadAllText("config.json");
            appSettings = Newtonsoft.Json.JsonConvert.DeserializeObject<config>(txt);

            string txtMapping = System.IO.File.ReadAllText("mapping.json");
            mapping = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MappingValue>>(txtMapping);

            Log.Information("Start!");

            Service service = new Service();

            //var acb = service.getUrlObj("/kyc-service/sessionInfo/save");

            Thread t1 = new Thread(() => pullSTM(service));
            Thread t2 = new Thread(() => sendSTM(service));
            t1.Start();
            t2.Start();
        }
		catch (Exception e)
		{
            Log.Error($"main {e.ToString()}");
        }
        finally
        {
            while (true) { }
        }
    }

    public static void pullSTM(Service sv)
    {
        string key = Guid.NewGuid().ToString();
        try
        {
            // Log.Information("pullRabbitMQ ");
            var HostName = appSettings.RabbitMQ.Hostname;
            var factory = new ConnectionFactory() { Port = appSettings.RabbitMQ.Port };
            if (!string.IsNullOrEmpty(appSettings.RabbitMQ.username) && !string.IsNullOrEmpty(appSettings.RabbitMQ.password))
            {
                factory.UserName = appSettings.RabbitMQ.username;
                factory.Password = appSettings.RabbitMQ.password;
            }
            using (var connection = factory.CreateConnection(HostName))
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: appSettings.RabbitMQ.Queue_CRM,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body.ToArray());
                    Log.Information($"pullCRM key={key}; " + message);
                    try
                    {
                        sv.xlMsg(message);
                    }
                    catch (Exception ec)
                    {
                        Log.Error($"pullSTM: message key={key}; " + ec.ToString());
                    }
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };
                channel.BasicQos(0, 5, false);
                channel.BasicConsume(queue: appSettings.RabbitMQ.Queue_CRM,
                                     autoAck: false,
                                     consumer: consumer);
                Thread.Sleep(-1);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"pullSTM: key={key}; " + ex.ToString());
        }
    }
    public static void sendSTM(Service sv)
    {
        string key = Guid.NewGuid().ToString();
        try
        {
            // Log.Information("pullRabbitMQ ");
            var HostName = appSettings.RabbitMQ.Hostname;
            var factory = new ConnectionFactory() { Port = appSettings.RabbitMQ.Port };
            if (!string.IsNullOrEmpty(appSettings.RabbitMQ.username) && !string.IsNullOrEmpty(appSettings.RabbitMQ.password))
            {
                factory.UserName = appSettings.RabbitMQ.username;
                factory.Password = appSettings.RabbitMQ.password;
            }
            using (var connection = factory.CreateConnection(HostName))
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: appSettings.RabbitMQ.Queue_SEND,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body.ToArray());
                    Log.Information($"sendSTM key={key}; " + message);
                    try
                    {
                        sv.xlSendMsg(message);
                    }
                    catch (Exception ec)
                    {
                        Log.Error($"sendSTM: message key={key}; " + ec.ToString());
                    }
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };
                channel.BasicQos(0, 5, false);
                channel.BasicConsume(queue: appSettings.RabbitMQ.Queue_SEND,
                                     autoAck: false,
                                     consumer: consumer);
                Thread.Sleep(-1);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"sendSTM: key={key}; " + ex.ToString());
        }
    }
}