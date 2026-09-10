using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using ServerTier;

/**
 * ChatServerConnection.cs - Creates the WCF connection used by the polling client
 *                           to communicate with the chat server.
 */

namespace PollingClient
{
    public class ServerConnection
    {
        private const string ServerAddress = "net.tcp://localhost:9000/ChatServiceImplementation";

        private readonly ChannelFactory<ServerInterface> channelFactory;

        public ServerInterface Service { get; private set; }

        public ServerConnection()
        {
            var tcp = new NetTcpBinding();
            tcp.MaxReceivedMessageSize = 4 * 1024 * 1024; 
            tcp.MaxBufferSize = 4 * 1024 * 1024;
            tcp.ReaderQuotas.MaxArrayLength = 4 * 1024 * 1024;

            EndpointAddress endpoint = new EndpointAddress(ServerAddress);

            channelFactory = new ChannelFactory<ServerInterface>(tcp, endpoint);

            Service = channelFactory.CreateChannel();
        }
    }
}