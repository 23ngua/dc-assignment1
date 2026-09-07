using System;
using System.Collections.Generic;
using ChatResult;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServerTier
{
    /*
     *  Server side channel state
     *  Temporarily maintains a list of messages to be passed to vaild clients
     */
    internal class ChannelState
    {
        public string Name { get; }
        private readonly List<ChatMessage> _messages = new List<ChatMessage>();
        private readonly object _lock = new object();

        public ChannelState(string name)
        {
            Name = name;
        }

        public int AddMessage(string senderId, string text)
        {
            lock (_lock)
            {
                _messages.Add(new ChatMessage { 
                    SenderId = senderId, Text = text, Timestamp = DateTime.UtcNow});
                return _messages.Count; // client to remember which index they joined at, so that they can only view messages onwayds
            }
        }

        public int CurrentMessageCount()
        {
            lock (_lock) { return _messages.Count; }
        }

        // sinceIndex = how many messages the client has already seen
        public List<ChatMessage> GetMessagesSince(int sinceIndex)
        {
            lock (_lock)
            {
                if (sinceIndex >= _messages.Count) return new List<ChatMessage>();
                return _messages.GetRange(sinceIndex, _messages.Count - sinceIndex);
            }
        }
    }
}
