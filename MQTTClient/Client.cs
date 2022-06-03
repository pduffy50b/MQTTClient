
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System.Text;
namespace MQTTClient
{
    public class Client : IDisposable
    {
        private static IMqttClient? _client;
        private static IMqttClientOptions? _options;
        private readonly string _topic;

        public delegate void MessageReceivedHandler(string topic, string payload);
        public event MessageReceivedHandler? OnMessageReceived;

        public delegate void ConnectedHandler();
        public event ConnectedHandler? OnConnected;

        public delegate void DisconnectedHandler();
        public event DisconnectedHandler? OnDisconnected;

        public Client(MqttConfig mqttConfig)
        {
            try
            {
                if (mqttConfig == null)
                    throw new ArgumentNullException(nameof(mqttConfig));

                if (string.IsNullOrEmpty(mqttConfig.Server))
                    throw new ArgumentNullException("Server");

                if (mqttConfig.Port == default || mqttConfig.Port == 0)
                    throw new ArgumentNullException("Port");

                if (string.IsNullOrEmpty(mqttConfig.Topic))
                    throw new ArgumentNullException("Topic");

                if (string.IsNullOrEmpty(mqttConfig.Username))
                    throw new ArgumentNullException("Username");

                if (string.IsNullOrEmpty(mqttConfig.Password))
                    throw new ArgumentNullException("Password");

                mqttConfig.ClientId = string.IsNullOrEmpty(mqttConfig.ClientId) ? Guid.NewGuid().ToString() : mqttConfig.ClientId;
                _topic = mqttConfig.Topic;

                // Create a new MQTT client.
                var factory = new MqttFactory();
                _client = factory.CreateMqttClient();

                //configure options
                if (mqttConfig.UseSSL)
                {
                    _options = new MqttClientOptionsBuilder()
                                .WithClientId(mqttConfig.ClientId)
                                .WithTcpServer(mqttConfig.Server, mqttConfig.Port)
                                .WithTls()
                                .WithCredentials(mqttConfig.Username, mqttConfig.Password)
                                .WithCleanSession()
                                .Build();
                }
                else
                {
                    _options = new MqttClientOptionsBuilder()
                                .WithClientId(mqttConfig.ClientId)
                                .WithTcpServer(mqttConfig.Server, mqttConfig.Port)
                                .WithCredentials(mqttConfig.Username, mqttConfig.Password)
                                .WithCleanSession()
                                .Build();
                }

                //handlers
                _client.UseConnectedHandler(e =>
                {
                    //Subscribe to topic when connected
                    _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(_topic).Build()).Wait();
                    OnConnected?.Invoke();
                });

                _client.UseDisconnectedHandler(e =>
                {
                    OnDisconnected?.Invoke();
                });

                _ = _client.UseApplicationMessageReceivedHandler(e =>
                {
                    string topic = e.ApplicationMessage.Topic;
                    if (string.IsNullOrWhiteSpace(topic) == false)
                    {

                        string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                        OnMessageReceived?.Invoke(topic, payload);
                    }
                });

                //connect
                _client.ConnectAsync(_options).Wait();

            }
            catch (Exception)
            {
                throw;
            }
        }

        //Test Method to Simulate 10 messages
        public void SimulatePublish()
        {

            var counter = 0;
            while (counter < 10)
            {
                counter++;
                var testMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(_topic)
                    .WithPayload($"Payload: Simulate {counter}")
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();

                if (_client != null)
                {
                    if (_client.IsConnected)
                        _client.PublishAsync(testMessage);

                    Thread.Sleep(2000);
                }
            }
        }

        public void Publish(string payload)
        {
            try
            {
                if (string.IsNullOrEmpty(payload))
                    throw new ArgumentNullException(nameof(payload));

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(_topic)
                    .WithPayload(payload)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();

                if (_client != null)
                {
                    if (_client.IsConnected)
                        _client.PublishAsync(message);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Publish(string payload, string topic)
        {
            try
            {
                if (string.IsNullOrEmpty(payload))
                    throw new ArgumentNullException(nameof(payload));

                if (string.IsNullOrEmpty(topic))
                    throw new ArgumentNullException(nameof(topic));

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();

                if (_client != null)
                {
                    if (_client.IsConnected)
                        _client.PublishAsync(message);
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_client != null)
                {
                    _client.DisconnectAsync().Wait();
                    _client.Dispose();
                }
            }
        }

    }

    public class MqttConfig
    {
        public string? Topic { get; set; }
        public string? Payload { get; set; }
        public string? Server { get; set; }
        public int? Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool UseSSL { get; set; }
        public string? ClientId { get; set; }
    }
}