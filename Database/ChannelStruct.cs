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
        private string newMessages;
        public ChannelStruct()
        {
            newMessages = "";
        }
        
        public void AddNewMessage(string message)
        {
            newMessages = message;
        }
        public string GetNewMessage()
        {
            return newMessages;
        }
    }
}
