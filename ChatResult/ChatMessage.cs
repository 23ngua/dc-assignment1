using System;
using System.Runtime.Serialization;

namespace DataLibrary
{
    [DataContract]
    public class ChatMessage
    {

        [DataMember]
        public string SenderId { get; set; }

        [DataMember]
        public string Text { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }
}
