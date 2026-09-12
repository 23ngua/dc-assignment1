using ServerTier;
using System.ServiceModel;

namespace DuplexClient
{
    public class DuplexServerConnection
    {
        private const string ServerAddress = "net.tcp://localhost:9000/DuplexChatServiceImplementation";

        private readonly DuplexChannelFactory<DuplexServerInterface> duplexChannelFactory;

        public DuplexServerInterface Service { get; private set; }

        public DuplexServerConnection(ClientUpdateHandler callbackHandler)
        {
            var tcp = new NetTcpBinding();

            tcp.MaxReceivedMessageSize = 4 * 1024 * 1024;
            tcp.MaxBufferSize = 4 * 1024 * 1024;
            tcp.ReaderQuotas.MaxArrayLength = 4 * 1024 * 1024;

            InstanceContext callbackContext = new InstanceContext(callbackHandler);

            EndpointAddress endpoint = new EndpointAddress(ServerAddress);

            duplexChannelFactory = new DuplexChannelFactory<DuplexServerInterface>(
                callbackContext,
                tcp,
                endpoint);

            Service = duplexChannelFactory.CreateChannel();
        }
    }
}
