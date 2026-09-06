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
            //Start the server
            Console.WriteLine("Welcome to the Chat Server");
            var tcp = new NetTcpBinding();

            //Bind the interface
            //Create the host
            var host = new ServiceHost(typeof(ChatServerImplementation));
            host.AddServiceEndpoint(typeof(ChatServerInterface), tcp, "net.tcp://localhost:9000/ChatServiceImplementation");
            host.Open();
            //Hold the server open until someone does something
            Console.WriteLine("System Online");
            Console.ReadLine();
            //Close the host
            host.Close();
        }
    }
}