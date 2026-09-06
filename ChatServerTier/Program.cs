using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using ChatServerTier;

/**
 * Program.cs - Starts and hosts the WCF chat service.
 */

namespace ChatServerTier
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Fixed address used by clients to connect to the chat server
            Uri baseAddress = new Uri("net.tcp://localhost:9000/ChatServiceImplementation");

            // Create a WCF host for ChatService implementation
            using (ServiceHost host = new ServiceHost(typeof(ChatServerImplementation), baseAddress))
            {
                // Use TCP communication between server and clients
                NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);

                // Expose shared IChatService contract using this binding
                host.AddServiceEndpoint(
                    typeof(ChatServerImplementation),
                    binding,
                    "");

                // Start listening for incoming client requests
                host.Open();

                Console.WriteLine("Chat Server is running.");
                Console.WriteLine("Address: " + baseAddress);
                Console.WriteLine();
                Console.WriteLine("Press ENTER to stop the server.");

                // Keep server running until ENTER is pressed
                Console.ReadLine();
            }
        }
    }
}