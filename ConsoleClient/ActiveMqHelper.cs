using Apache.NMS;
using Apache.NMS.ActiveMQ;

namespace ConsoleClient;

public static class ActiveMqHelper
{
    private static Uri ActiveMqUri { get; } = new("tcp://localhost:61616?wireFormat.maxInactivityDuration=0");

    private static IConnection? Connection { get; set; }
    private static ISession? Session { get; set; }
    private static IMessageProducer? Producer { get; set; }
    private static IMessageConsumer? Consumer { get; set; }

    public static event Action<ITextMessage>? MessageReceived;

    private static async Task ConnectToActiveMq()
    {
        if (Session != null) return;
        var connectionFactory = new ConnectionFactory(ActiveMqUri);
        Connection = await connectionFactory.CreateConnectionAsync();
        await Connection.StartAsync();
        Session = await Connection.CreateSessionAsync();
    }

    public static async Task ConnectToSendQueue(string queueName)
    {
        if (Session == null) await ConnectToActiveMq();
        var receiveQueue = await Session!.GetQueueAsync(queueName);
        Producer = await Session.CreateProducerAsync(receiveQueue);
        Producer.DeliveryMode = MsgDeliveryMode.NonPersistent;
    }
    public static async Task ConnectToReceiveQueue(string queueName)
    {
        if (Session == null) await ConnectToActiveMq();
        var sendingQueue = await Session!.GetQueueAsync(queueName);
        Consumer = await Session.CreateConsumerAsync(sendingQueue);
        Consumer.Listener += message =>
        {
            if (message is ITextMessage textMessage) MessageReceived?.Invoke(textMessage);
        };
    }

    public static async Task DisconnectFromActiveMq()
    {
        await DisconnectFromQueues();
        if (Session != null) await Session.CloseAsync();
        if (Connection != null) await Connection.CloseAsync();
    }

    public static async Task DisconnectFromQueues()
    {
        if (Producer != null) await Producer.CloseAsync();
        if (Consumer != null) await Consumer.CloseAsync();
    }

    public static async Task SendMessage(string messageContent, string? messageType = null)
    {
        if (Session == null) throw new InvalidOperationException("A session has not been initialized");
        if (Producer == null) throw new InvalidOperationException("You're not connected to a producer");

        var message = await Session.CreateTextMessageAsync(messageContent);
        if (messageType != null) message.Properties.SetString("tag", messageType);
        await Producer.SendAsync(message);
    }
}
