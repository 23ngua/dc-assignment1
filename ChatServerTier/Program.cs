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
            Console.WriteLine("Welcome to the Chat Server");
            var tcp = new NetTcpBinding();
            tcp.MaxReceivedMessageSize = 4 * 1024 * 1024; // 4MB
            tcp.MaxBufferSize = 4 * 1024 * 1024;
            tcp.ReaderQuotas.MaxArrayLength = 4 * 1024 * 1024; 

            var host = new ServiceHost(typeof(ChatServerImplementation));
            host.AddServiceEndpoint(typeof(ChatServerInterface), tcp, "net.tcp://localhost:9000/ChatServiceImplementation");
            host.AddServiceEndpoint(typeof(DuplexChatServerInterface), tcp, "net.tcp://localhost:9000/DuplexChatServiceImplementation");
            host.Open();

            Console.WriteLine("System Online");
            Console.ReadLine();

            host.Close();
        }
    }
}