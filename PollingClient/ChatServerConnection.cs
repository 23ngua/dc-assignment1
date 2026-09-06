using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using ChatServerTier;
using ChatResults;

/**
 * ChatServerConnection.cs - Creates the WCF connection used by the polling client
 *                           to communicate with the chat server.
 */

namespace PollingClient
{
    public class ChatServerConnection
    {
        // Fix WCF address exposed by ChatServer project
        private const string ServerAddress = "net.tcp://localhost:9000/ChatServerImplimentation";

        // Create and manage WCF client channels
        private readonly ChannelFactory<ChatServerInterface> channelFactory;

        // Give client access to server's shared service contract
        public ChatServerInterface Service { get; private set; }

        // Set up WCF connection when this class is created
        public ChatServerConnection()
        {
            // Use same TCP binding and security mode as the server
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);

            // Identify server endpoint that the client will contact
            EndpointAddress endpoint = new EndpointAddress(ServerAddress);

            // Create factory that understands shared IChatService contract
            channelFactory = new ChannelFactory<ChatServerInterface>(binding, endpoint);

            // Create client-side proxy used to call server opertions
            Service = channelFactory.CreateChannel();
        }
    }
}