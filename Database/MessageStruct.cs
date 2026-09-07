using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public class MessageStruct
    {
        public string SenderId { get; }
        public string Text { get; }
        public DateTime Timestamp { get; }

        public MessageStruct(string senderId, string text, DateTime timestamp)
        {
            SenderId = senderId;
            Text = text;
            Timestamp = timestamp;
        }
    }
}
