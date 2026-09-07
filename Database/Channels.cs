using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;

namespace Database
{
    public class Channels
    {
        private readonly Dictionary<string, ChannelStruct> _channel;
        private readonly object _lock = new object();

        public static Channels Instance { get; } = new Channels();

        private Channels()
        {
            _channel = new Dictionary<string, ChannelStruct>();
            SetNewChannel("General Chat Room!");
        }

        public int GetNumChannels()
        {
            lock (_lock) 
            { 
                return _channel.Count; 
            }
        }

        public List<string> GetChannelList()
        {
            lock (_lock) 
            { 
                return new List<string>(_channel.Keys); 
            }
        }

        public void SetNewChannel(string name)
        {
            lock (_lock) 
            { 
                _channel.Add(name, new ChannelStruct()); 
            }
        }

        public bool ContainsChannel(string name)
        {
            lock (_lock) 
            { 
                return _channel.ContainsKey(name); 
            }
        }

        public void AddMessage(string channelName, string senderId, string text)
        {
            ChannelStruct channel;
            lock (_lock)
            {
                if (!_channel.ContainsKey(channelName))
                {
                    return;
                }
                channel = _channel[channelName];
            }
            channel.AddMessage(senderId, text);
        }

        public int GetMessageCount(string channelName)
        {
            ChannelStruct channel;

            lock (_lock)
            {
                if (!_channel.ContainsKey(channelName))
                {
                    return 0;
                }
                channel = _channel[channelName];
            }
            return channel.CurrentMessageCount();
        }

        public List<MessageStruct> GetMessagesSince(string channelName, int sinceIndex)
        {
            ChannelStruct channel;

            lock (_lock)
            {
                if (!_channel.ContainsKey(channelName))
                {
                    return new List<MessageStruct>();
                }
                channel = _channel[channelName];
            }
            return channel.GetMessagesSince(sinceIndex);
        }
    }
}