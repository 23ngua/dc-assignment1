using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Database
{
    internal class ChannelStruct
    {
        private BlockingCollection<string> newMessages;
        public ChannelStruct()
        {
            newMessages = new BlockingCollection<string>();
        }
        
        public void AddNewMessage(string message)
        {
            newMessages.Add(message);
        }
        public string GetNewMessage()
        {
            return newMessages.Take();
        }
    }
}
