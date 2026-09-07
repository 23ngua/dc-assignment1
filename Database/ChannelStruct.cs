using System;
using System.Collections.Generic;

namespace Database
{
    public class ChannelStruct
    {
        private readonly List<MessageStruct> _messages = new List<MessageStruct>();
        private readonly object _messagesLock = new object();

        public ChannelStruct()
        {
        }

        public int AddMessage(string senderId, string text)
        {
            lock (_messagesLock)
            {
                _messages.Add(new MessageStruct(senderId, text, DateTime.UtcNow));
                return _messages.Count;
            }
        }

        public int CurrentMessageCount()
        {
            lock (_messagesLock)
            {
                return _messages.Count;
            }
        }

        public List<MessageStruct> GetMessagesSince(int sinceIndex)
        {
            lock (_messagesLock)
            {
                int totalMessages = _messages.Count;

                if (sinceIndex >= totalMessages)
                {
                    return new List<MessageStruct>();
                }

                List<MessageStruct> newMessages = new List<MessageStruct>();

                for (int i = sinceIndex; i < totalMessages; i++)
                {
                    newMessages.Add(_messages[i]);
                }

                return newMessages;
            }
        }
    }
}